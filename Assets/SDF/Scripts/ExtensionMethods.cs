//
// Copyright © Daniel Shervheim, 2019
// www.danielshervheim.com
//

using UnityEngine;

public static class ExtensionMethods
{
    public static Texture3D ToTexture3D(this RenderTexture rt)
    {
        if (rt.dimension != UnityEngine.Rendering.TextureDimension.Tex3D)
        {
            Debug.LogError("ToTexture3D requires that " + rt.name + " .RenderTexture be a volume.");
            return null;
        }

        // Create a blank texture3D object.
        Texture3D t3d = new Texture3D(rt.width, rt.height, rt.volumeDepth, TextureFormat.RGBAFloat, false);

        /*
        // Load the compute shader and get its handle.
        ComputeShader compute = Resources.Load("RenderTexture3DToPixelBuffer") as ComputeShader;
        int computeHandle = compute.FindKernel("CSMain");

        // Upload the render texture to the compute shader.
        compute.SetTexture(computeHandle, "voxels", rt);
        compute.SetVector("voxelDimensions", new Vector4(rt.width, rt.height, rt.volumeDepth, 0));

        // Get an array to hold the pixels.
        Color[] pixels = t3d.GetPixels();
        ComputeBuffer pixelBuffer = new ComputeBuffer(pixels.Length, 16);  // 4*sizeof(float)

        // Upload the pixels buffer to the compute shader.
        compute.SetBuffer(computeHandle, "pixels", pixelBuffer);
        compute.SetInt("pixelCount", pixels.Length);

        // Execute the compute shader to copy the render texture to the pixels buffer.
        compute.Dispatch(computeHandle, rt.width / 10 + 1, rt.height / 10 + 1, rt.volumeDepth / 10 + 1);

        // Copy the buffer back to the array, then release the buffer.
        pixelBuffer.GetData(pixels);
        pixelBuffer.Release();

        // Upload the pixels array to the texture 3d.
        t3d.SetPixels(pixels);
        t3d.Apply();
        */

        Graphics.CopyTexture(rt, t3d);

        return t3d;
    }
}