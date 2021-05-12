// Copyright 2021 Daniel Shervheim

using UnityEngine;

namespace DSS.SDF {

/// @brief A tool to generate signed distance fields from Mesh assets.
/// TODO: determine if we lose precision or negative values by using Color struct.
/// Perhaps we should return float[,,] instead?
public class Generator : UnityEngine.Object {
  private Mesh mesh = null;
  public Mesh Mesh {
    get {
      return mesh;
    }
    set {
      mesh = value;
    }
  }

  private int subMeshIndex = 0;
  public int SubMeshIndex {
    get {
      return subMeshIndex;
    }
    set {
      if (value < 0) {
        throw new System.IndexOutOfRangeException("SubMeshIndex must be >= 0");
      }
      subMeshIndex = value;
    }
  }

  private float padding = 0f;
  public float Padding {
    get {
      return padding;
    }
    set {
      if (value < 0f) {
        throw new System.ArgumentException("Padding must be >= 0");
      }
      padding = value;
    }
  }

  private int resolution = 32;
  public int Resolution {
    get {
      return resolution;
    }
    set {
      if (value < 1) {
        throw new System.ArgumentException("Resolution must be >= 1");
      }
      resolution = value;
    }
  }

  private struct Triangle {
    public Vector3 a, b, c;
  }

  public Texture3D Generate() {
    if (mesh == null) {
      throw new System.ArgumentException("Mesh must have been assigned");
    }

    // Create the voxel texture.
    Texture3D voxels = new Texture3D(resolution, resolution, resolution, TextureFormat.RGBAHalf, false);
    voxels.anisoLevel = 1;
    voxels.filterMode = FilterMode.Bilinear;
    voxels.wrapMode = TextureWrapMode.Clamp;

    // Get an array of pixels from the voxel texture, create a buffer to
    // hold them, and upload the pixels to the buffer.
    Color[] pixelArray = voxels.GetPixels(0);
    ComputeBuffer pixelBuffer = new ComputeBuffer(pixelArray.Length, sizeof(float) * 4);
    pixelBuffer.SetData(pixelArray);

    // Get an array of triangles from the mesh.
    Vector3[] meshVertices = mesh.vertices;
    int[] meshTriangles = mesh.GetTriangles(subMeshIndex);
    Triangle[] triangleArray = new Triangle[meshTriangles.Length / 3];
    for (int t = 0; t < triangleArray.Length; t++) {
        triangleArray[t].a = meshVertices[meshTriangles[3 * t + 0]];  // - mesh.bounds.center;
        triangleArray[t].b = meshVertices[meshTriangles[3 * t + 1]];  // - mesh.bounds.center;
        triangleArray[t].c = meshVertices[meshTriangles[3 * t + 2]];  // - mesh.bounds.center;
    }

    // Create a buffer to hold the triangles, and upload them to the buffer.
    ComputeBuffer triangleBuffer = new ComputeBuffer(triangleArray.Length, sizeof(float) * 3 * 3);
    triangleBuffer.SetData(triangleArray);

    // Instantiate the compute shader from resources.
    ComputeShader compute = Instantiate(Resources.Load("GenerateSDF")) as ComputeShader;
    int kernel = compute.FindKernel("CSMain");

    // Upload the pixel buffer to the GPU.
    compute.SetBuffer(kernel, "pixelBuffer", pixelBuffer);
    compute.SetInt("pixelBufferSize", pixelArray.Length);

    // Upload the triangle buffer to the GPU.
    compute.SetBuffer(kernel, "triangleBuffer", triangleBuffer);
    compute.SetInt("triangleBufferSize", triangleArray.Length);

    // Calculate and upload the other necessary parameters.
    compute.SetInt("textureSize", resolution);
    Vector3 minExtents = Vector3.zero;
    Vector3 maxExtents = Vector3.zero;
    foreach (Vector3 v in mesh.vertices) {
      for (int i = 0; i < 3; i++) {
        minExtents[i] = Mathf.Min(minExtents[i], v[i]);
        maxExtents[i] = Mathf.Max(maxExtents[i], v[i]);
      }
    }
    compute.SetVector("minExtents", minExtents - Vector3.one*padding);
    compute.SetVector("maxExtents", maxExtents + Vector3.one*padding);

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

    // Return the voxel texture.
    return voxels;
  }
}

}  // namespace DSS.SDF