# Signed Distance Field Generator

This tool creates signed distance field volumes (stored as a Texture3D asset) from Mesh objects in Unity.

Signed distance fields have many uses, such as complex mesh-particle collision detection, soft shadow rendering, subsurface scattering, etc.

![bunny bilinear](https://i.imgur.com/GXRBKJY.png)

The [Stanford bunny](https://sketchfab.com/3d-models/bunny-4cc18d8e0552459b8897948b81cb20ad) rendered from a 64x64x64 texture with half precision, and bilinear filtering.

![bunny point](https://i.imgur.com/lSGj6tp.png)

The same bunny, with point filtering applied.

## How to install

1. Create a new Unity project (version 2018.3 or greater).
2. Clone this repository.
3. Replace the new projects `Asset` folder with the one from the cloned repository.

## How to create an SDF 

1. Right click anywhere in the Asset browser and select `Create > Signed Distance Field` from the popup menu.
2. Select the newly created SDF asset and assign its `Mesh` property.
3. If the mesh is composed of more than one submesh, select the `index` of the submesh you want to use. (This can usually be left at 0).
4. Adjust the `padding` settings (in Unity units) to surround the object within the generated texture.
5. Select the `texture size` (64 usually yields a good mix of size and clarity).
6. Select the sign computation method (Only select `Dot Product` if `Intersection Counter` yields artefacts).
7. Press `Bake`.

## How to visualize

1. Drag a cube primitive into the scene, and set its `Transform` position to (0, 0, 0).
2. Create a new `Material` and select `SignedDistanceFieldVisualizer` as the shader.
3. Drag a `Texture3D` asset from within a `SignedDistanceField` instance into the `Volume` slot of the material.
4. Adjust the other material parameters as necessary.

`Render As Solid` renders the SDF as a solid object with a simple Lambertian shading model.

`Density` adjusts the density of the solid when `Render As Solid` is turned off.

`Maximum Steps` sets the number of steps taken when raytracing through the volume. Setting this requires some trial and error. Too small and detail is lost as sample positions are missed when tracing. Too big and performance suffers.

## Notes

This implementation utilizes compute shaders to speed up the computation process. Unity currently has various bugs related to RenderTextures, Texture3D objects, and Compute shaders. As such, I compute each voxels value and store it in a 1D buffer (rather than the more natural 3D render texture). The buffer is then read back into a Texture3D Object on the CPU, and saved as an asset.