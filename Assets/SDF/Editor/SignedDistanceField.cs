using UnityEngine;
using UnityEditor;
using System.Collections;

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