# Signed Distance Field Generator

A Unity tool to generate signed distance field volumes (as `Texture3D` assets) from meshes.

![demo](https://imgur.com/nHyQgX9.gif)

## To Install

Download the [.unitypackage](https://github.com/danielshervheim/Signed-Distance-Field-Generator/releases/download/1.0/sdf_generator_1.0.unitypackage), or clone this repository.

Tested on Unity 2018.4, should work on earlier and later versions as well. Requires a GPU that supports Compute shaders.

## To Use

1. Select `Signed Distance Field > Generator` from the Unity menu bar, the tool window will pop up.
2. Press `Create` and a save dialog will pop up.
3. Choose a location and name to save your SDF as.

## Options

__Mesh__: the mesh you want to make an SDF from.

__Resolution__: the resulting `Texture3D` resolution (I recommend you keep this below 64).

__Submesh Index__: for multi-part meshes, the index of the submesh you want to use.

__Padding__: the padding to surround your mesh within the SDF (only set this to be non-zero if you see artifacts).

__Method__: the method used to determine the sign of each voxel (only use `DotProduct` if `IntersectionCounter` yields artifacts).

## To Visualize

I also included a simple raymarching shader to visualize the resulting SDFs.

1. Drag a cube primitive into the scene, and set its `Transform` position to (0, 0, 0).
2. Change its material to `SDF > Materials > SignedDistanceField_Visualizer`.
3. Drag one of your created SDF assets into the `Volume` slot of the material.
4. Adjust the other material parameters as necessary.

__Render As Solid__: renders the SDF as a solid object with a simple Lambertian shading model.

__Density__: adjusts the density of the solid when `Render As Solid` is turned off.

__Maximum Steps__: sets the number of steps taken when raytracing through the volume. Setting this requires some trial and error. Too small and detail is lost as sample positions are missed when tracing. Too big and performance suffers.
