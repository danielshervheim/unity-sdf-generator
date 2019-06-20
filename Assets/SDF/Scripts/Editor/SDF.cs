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
        SDF window = ScriptableObject.CreateInstance(typeof(SDF)) as SDF;
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

        // Get the RenderTexture representation of the SDF.
        RenderTexture voxelsRT = ComputeSDF();

        // Convert the RenderTexture to a Texture3D to be saved as an asset.
        Texture3D voxels = new Texture3D(m_resolution, m_resolution, m_resolution, TextureFormat.RGBAFloat, false);
        Graphics.CopyTexture(voxelsRT, voxels);

        // Save the voxels to texture3d asset at path.
        AssetDatabase.CreateAsset(voxels, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Close the window.
        Close();

        // Select the SDF in the project view.
        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path);
    }



    private RenderTexture ComputeSDF()
    {
        RenderTexture voxels = new RenderTexture(m_resolution, m_resolution, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        voxels.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        voxels.volumeDepth = m_resolution;
        voxels.enableRandomWrite = true;
        voxels.Create();

        // Get the compute shader instance.
        ComputeShader compute = Resources.Load("SDFCompute") as ComputeShader;
        int computeHandle = compute.FindKernel("CSMain");

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
        ComputeBuffer triangleBuffer = new ComputeBuffer(triangleArray.Length, 36);  // 3*3*sizeof(float)
        triangleBuffer.SetData(triangleArray);

        // Upload the RenderTexture and info to the gpu.
        compute.SetTexture(computeHandle, "voxels", voxels);
        compute.SetInt("resolution", m_resolution);

        // Upload the triangle buffer to the gpu.
        compute.SetBuffer(computeHandle, "triangleBuffer", triangleBuffer);
        compute.SetInt("triangleBufferSize", triangleArray.Length);

        // Set the other necessary parameters.
        float maxMeshSize = Mathf.Max(Mathf.Max(m_mesh.bounds.size.x, m_mesh.bounds.size.y), m_mesh.bounds.size.z);
        float totalUnitsInTexture = maxMeshSize + 2.0f * m_padding;
        compute.SetFloat("totalUnitsInTexture", totalUnitsInTexture);
        compute.SetInt("useIntersectionCounter", (m_signComputationMethod == SignComputationMethod.IntersectionCounter) ? 1 : 0);

        // Compute the sdf.
        int groups = m_resolution / 10 + 1;
        compute.Dispatch(computeHandle, groups, groups, groups);

        // Release the triangle buffer.
        triangleBuffer.Release();

        return voxels;
    }
}



/*
[CreateAssetMenu(fileName = "New SDF", menuName = "Signed Distance Field")]
public class SignedDistanceField : ScriptableObject {

	public enum SignComputationMethod {IntersectionCounter, DotProduct};

    public Mesh mesh;
	public int subMeshIndex = 0;
	[Range(0f, 1f)] public float padding;
	[Range(0, 128)] public int textureSize = 32;
	public SignComputationMethod signComputationMethod = SignComputationMethod.IntersectionCounter;

	private Texture3D voxels;


	// triangle buffer data
	private struct Triangle {
		public Vector3 a;
		public Vector3 b;
		public Vector3 c;
	}
	private const int TRIANGLE_SIZE = 36;  // 3*3*sizeof(float)

    public void Bake() {
    	DestroyVoxelTexture();

		// stop if the mesh is not assigned
		if (mesh == null) {
			return;
		}
	
		// create the voxel texture and get an array of pixels
		CreateVoxelTexture();
		Color[] pixelArray = voxels.GetPixels(0);
		ComputeBuffer pixelBuffer = new ComputeBuffer(pixelArray.Length, sizeof(float)*4);//System.Runtime.InteropServices.Marshal.SizeOf(pixelArray[0]));
		pixelBuffer.SetData(pixelArray);

		// create the triangle array and buffer from the mesh
		Vector3[] meshVertices = mesh.vertices;
		int[] meshTriangles = mesh.GetTriangles(subMeshIndex);
		Triangle[] triangleArray = new Triangle[meshTriangles.Length/3];
		for (int t = 0; t < triangleArray.Length; t++) {
			triangleArray[t].a = meshVertices[meshTriangles[3*t+0]] - mesh.bounds.center;
			triangleArray[t].b = meshVertices[meshTriangles[3*t+1]] - mesh.bounds.center;
			triangleArray[t].c = meshVertices[meshTriangles[3*t+2]] - mesh.bounds.center;
		}
		ComputeBuffer triangleBuffer = new ComputeBuffer(triangleArray.Length, TRIANGLE_SIZE);
		triangleBuffer.SetData(triangleArray);

		// instantiate the compute shader
		ComputeShader compute = (ComputeShader)Instantiate(Resources.Load("SignedDistanceFieldCompute"));
		int kernel = compute.FindKernel("CSMain");

		// upload the pixel buffer to the gpu
		compute.SetBuffer(kernel, "pixelBuffer", pixelBuffer);
		compute.SetInt("pixelBufferSize", pixelArray.Length);

		// upload the triangle buffer to the gpu
		compute.SetBuffer(kernel, "triangleBuffer", triangleBuffer);
		compute.SetInt("triangleBufferSize", triangleArray.Length);

		// set the other necessary parameters
		float maxMeshSize = Mathf.Max(Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y), mesh.bounds.size.z);
		float totalUnitsInTexture = maxMeshSize + 2.0f*padding;
		// float unitsPerVoxel = totalUnitsInTexture / (float)textureSize;

		compute.SetInt("textureSize", textureSize);  // pixelBufferSize/3
		compute.SetFloat("totalUnitsInTexture", totalUnitsInTexture);
		compute.SetInt("useIntersectionCounter", (signComputationMethod==SignComputationMethod.IntersectionCounter)?1:0);

		// compute the sdf
		compute.Dispatch(kernel, pixelArray.Length/256 + 1, 1, 1);

		// destroy the compute shader and release the triangle buffer
		DestroyImmediate(compute);
		triangleBuffer.Release();

		// retrieve the pixel buffer and reapply it to the texture3d
		pixelBuffer.GetData(pixelArray);
		pixelBuffer.Release();
		voxels.SetPixels(pixelArray, 0);
		voxels.Apply();

		// save the texture3d as asset
		AssetDatabase.AddObjectToAsset(voxels, AssetDatabase.GetAssetPath(this));
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}

	void CreateVoxelTexture() {
		if (voxels != null) {
			DestroyVoxelTexture();
		}

		voxels = new Texture3D(textureSize, textureSize, textureSize, TextureFormat.RGBAHalf, false);
		voxels.name = mesh.name + "_SDF_Texture3D";
		voxels.anisoLevel = 1;
		voxels.filterMode = FilterMode.Bilinear;
		voxels.wrapMode = TextureWrapMode.Clamp;
	}

	void DestroyVoxelTexture() {
		if (voxels != null) {
			if (AssetDatabase.Contains(voxels)) {
				AssetDatabase.RemoveObjectFromAsset(voxels);
				voxels = null;
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
		}
	}

	public Texture3D GetTexture() {
		return voxels;
	}
}
*/