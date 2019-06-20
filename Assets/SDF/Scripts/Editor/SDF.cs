//
// Copyright © Daniel Shervheim, 2019
// www.danielshervheim.com
//

using UnityEngine;
using UnityEditor;

public class SDF : EditorWindow
{
    Mesh m_mesh;
    int m_resolution = 32;

    bool m_showAdvanced = false;

    int m_submeshIndex = 0;
    float m_padding = 0f;
    enum SignComputationMethod {IntersectionCounter, DotProduct};
    SignComputationMethod m_signComputationMethod = SignComputationMethod.IntersectionCounter;

    // For triangle buffer.
    private struct Triangle
    {
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;
    }



    [MenuItem("Signed Distance Field/Generator")]
    static void Window()
    {
        SDF window = CreateInstance(typeof(SDF)) as SDF;
        window.ShowUtility();
    }



    private void OnGUI()
    {
        // Verify that compute shaders are supported.
        if (!SystemInfo.supportsComputeShaders)
        {
            EditorGUILayout.HelpBox("This tool requires a GPU that supports compute shaders.", MessageType.Error);

            if (GUILayout.Button("Close"))
            {
                Close();
            }

            return;
        }

        // Draw the GUI options.
        m_mesh = EditorGUILayout.ObjectField("Mesh", m_mesh, typeof(Mesh), false) as Mesh;
        m_resolution = (int)Mathf.Max(EditorGUILayout.IntField("Resolution", m_resolution), 1f);

        // Display warning if the resolution is high enough to potentially cause slow downs.
        if (m_resolution > 256)
        {
            EditorGUILayout.HelpBox("Computing the SDF at this resolution is not recommended.", MessageType.Error);
        }
        else if (m_resolution > 128)
        {
            EditorGUILayout.HelpBox("Computing the SDF at this resolution may be slow. Consider using a lower resolution.", MessageType.Warning);
        }

        m_showAdvanced = EditorGUILayout.Foldout(m_showAdvanced, "Advanced");
        if (m_showAdvanced)
        {
            m_submeshIndex = (int)Mathf.Max(EditorGUILayout.IntField("Submesh Index", m_submeshIndex), 0f);
            m_padding = EditorGUILayout.Slider("Padding", m_padding, 0f, 1f);
            m_signComputationMethod = (SignComputationMethod)EditorGUILayout.EnumPopup("Method", m_signComputationMethod);
        }

        // Create the SDF if the mesh has been assigned.
        if (m_mesh == null)
        {
            GUI.enabled = false;
        }

        if (GUILayout.Button("Create"))
        {
            CreateSDF();
        }

        GUI.enabled = true;
        
        // Cancel.
        if (GUILayout.Button("Close"))
        {
            Close();
        }
    }



    private void OnInspectorUpdate()
    {
        Repaint();
    }



    private void CreateSDF()
    {
        // Prompt the user to save the file.
        string path = EditorUtility.SaveFilePanelInProject("Save As", m_mesh.name + "_SDF", "asset", "");

        // ... If they hit cancel.
        if (path == null || path.Equals(""))
        {
            return;
        }

        // Get the Texture3D representation of the SDF.
        Texture3D voxels = ComputeSDF();

        // Save the Texture3D asset at path.
        AssetDatabase.CreateAsset(voxels, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Close the window.
        Close();

        // Select the SDF in the project view.
        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path);
    }



    private Texture3D ComputeSDF()
    {
        // Create the voxel texture and get an array of pixels from it.
        Texture3D voxels = new Texture3D(m_resolution, m_resolution, m_resolution, TextureFormat.RGBAHalf, false);
        voxels.anisoLevel = 1;
        voxels.filterMode = FilterMode.Bilinear;
        voxels.wrapMode = TextureWrapMode.Clamp;
        Color[] pixelArray = voxels.GetPixels(0);
        ComputeBuffer pixelBuffer = new ComputeBuffer(pixelArray.Length, sizeof(float) * 4);
        pixelBuffer.SetData(pixelArray);

        // Create the triangle array and buffer from the mesh.
        Vector3[] meshVertices = m_mesh.vertices;
        int[] meshTriangles = m_mesh.GetTriangles(m_submeshIndex);
        Triangle[] triangleArray = new Triangle[meshTriangles.Length / 3];
        for (int t = 0; t < triangleArray.Length; t++)
        {
            triangleArray[t].a = meshVertices[meshTriangles[3 * t + 0]] - m_mesh.bounds.center;
            triangleArray[t].b = meshVertices[meshTriangles[3 * t + 1]] - m_mesh.bounds.center;
            triangleArray[t].c = meshVertices[meshTriangles[3 * t + 2]] - m_mesh.bounds.center;
        }
        ComputeBuffer triangleBuffer = new ComputeBuffer(triangleArray.Length, sizeof(float) * 3 * 3);
        triangleBuffer.SetData(triangleArray);

        // Instantiate the compute shader from resources.
        ComputeShader compute = (ComputeShader)Instantiate(Resources.Load("SDFCompute"));
        int kernel = compute.FindKernel("CSMain");

        // Upload the pixel buffer to the GPU.
        compute.SetBuffer(kernel, "pixelBuffer", pixelBuffer);
        compute.SetInt("pixelBufferSize", pixelArray.Length);

        // Upload the triangle buffer to the GPU.
        compute.SetBuffer(kernel, "triangleBuffer", triangleBuffer);
        compute.SetInt("triangleBufferSize", triangleArray.Length);

        // Calculate and upload the other necessary parameters.
        float maxMeshSize = Mathf.Max(Mathf.Max(m_mesh.bounds.size.x, m_mesh.bounds.size.y), m_mesh.bounds.size.z);
        float totalUnitsInTexture = maxMeshSize + 2.0f * m_padding;
        compute.SetInt("textureSize", m_resolution);
        compute.SetFloat("totalUnitsInTexture", totalUnitsInTexture);
        compute.SetInt("useIntersectionCounter", (m_signComputationMethod == SignComputationMethod.IntersectionCounter) ? 1 : 0);

        // Compute the SDF.
        compute.Dispatch(kernel, pixelArray.Length / 256 + 1, 1, 1);

        // Destroy the compute shader and release the triangle buffer.
        DestroyImmediate(compute);
        triangleBuffer.Release();

        // Retrieve the pixel buffer and reapply it to the voxels texture.
        pixelBuffer.GetData(pixelArray);
        pixelBuffer.Release();
        voxels.SetPixels(pixelArray, 0);
        voxels.Apply();

        // Return the voxels texture.
        return voxels;
    }
}
