//
// Copyright © Daniel Shervheim, 2019
// www.danielshervheim.com
//

using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SignedDistanceFieldGenerator : MonoBehaviour {
	
	[Header("Mesh")]
	public Mesh mesh;
	public int subMeshIndex = 0;
	public float padding = 0.05f;
	private float boundingBoxDimension;

	// texture type enum
	public enum TextureBitDepth {Float, Half};

	[Header("Texture")]
	[Range(1, 128)]
	public int textureDimension;
	public TextureBitDepth textureDepth;
	[ReadOnly] public float textureSize;  // in megabytes
	[ReadOnly] public float unitsPerVoxel;
	private RenderTexture voxels;

	[Header("Compute")]
    public ComputeShader compute;

    [Header("TMP mat")]
    public Material material;

	private struct Triangle {
    	public Vector3 a;
    	public Vector3 b;
    	public Vector3 c;
    	public Vector3 normal;
    }

    void Start() {
    	// verify required assets are present
    	if (mesh == null || compute == null) return;

		// get the tris, vertices, and normals from the mesh
		Vector3[] vertices = mesh.vertices;
		Vector3[] normals = mesh.normals;
		int[] triangleIndices = mesh.GetTriangles(subMeshIndex);

		// verify triangle indices are valid
		if (triangleIndices.Length%3 != 0) return;

		// create and fill the triangle array
		Triangle[] triangleArray = new Triangle[triangleIndices.Length/3];
		for (int i = 0; i < triangleArray.Length; i++) {
			int ai = triangleIndices[3*i+0];
			int bi = triangleIndices[3*i+1];
			int ci = triangleIndices[3*i+2];
			triangleArray[i].a = vertices[ai] - mesh.bounds.center;
			triangleArray[i].b = vertices[bi] - mesh.bounds.center;
			triangleArray[i].c = vertices[ci] - mesh.bounds.center;
			triangleArray[i].normal = (normals[ai]+normals[bi]+normals[ci])/3f;
		}

		// create the triangle compute buffer and set its data
		ComputeBuffer triangleBuffer = new ComputeBuffer(triangleArray.Length, 48);
		triangleBuffer.SetData(triangleArray);

		// create the texture to hold the voxel data
		RenderTextureFormat rtf = textureDepth == TextureBitDepth.Float ? RenderTextureFormat.RFloat : RenderTextureFormat.RHalf;
		voxels = new RenderTexture(textureDimension, textureDimension, 0, rtf, RenderTextureReadWrite.Linear);
		voxels.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
		voxels.enableRandomWrite = true;
		voxels.volumeDepth = textureDimension;
		voxels.wrapMode = TextureWrapMode.Clamp;
	    voxels.Create();

	    // get the compute shader info
		int kernelIndex = compute.FindKernel("CSMain");

		// upload the texture to the gpu
		compute.SetTexture(kernelIndex, "voxels", voxels);
		compute.SetInt("textureDimension", textureDimension);
		compute.SetFloat("unitsPerVoxel", unitsPerVoxel);

		// upload the triangle buffer and mesh data to the gpu
		compute.SetBuffer(kernelIndex, "triangleBuffer", triangleBuffer);
		compute.SetInt("triangleCount", triangleArray.Length);
		compute.SetFloat("boundingBoxDimension", boundingBoxDimension);

		// run the compute shader
		compute.Dispatch(kernelIndex, textureDimension/8+1, textureDimension/8+1, textureDimension/8+1);

		// free the buffer
		triangleBuffer.Release();

		// TMP set the texture
		material.SetTexture("_MainTex", voxels);
    }

    void OnValidate() {
    	if (mesh != null) {
    		boundingBoxDimension = Mathf.Max(Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y), mesh.bounds.size.z) + 2f*padding;
    		unitsPerVoxel = boundingBoxDimension / (float)textureDimension;

    		float voxelSize = textureDepth == TextureBitDepth.Float ? 32f : 16f;
    		textureSize = voxelSize * Mathf.Pow((float)textureDimension, 3f) / 1000000.0f;
    	}
    	
    }

    void OnDestroy() {
    	if (voxels != null) {
    		voxels.Release();
    	}
    }
}
