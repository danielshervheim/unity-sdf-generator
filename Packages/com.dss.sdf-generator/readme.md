# sdf-generator

A Unity tool to generate signed distance field volumes (as `Texture3D` assets) from meshes.

![demo](https://imgur.com/nHyQgX9.gif)

## How To Install

The sdf-generator package uses the [scoped registry](https://docs.unity3d.com/Manual/upm-scoped.html) feature to import
dependent packages. Please add the following sections to the package manifest
file (`Packages/manifest.json`).

To the `scopedRegistries` section:

```
{
  "name": "DSS",
  "url": "https://registry.npmjs.com",
  "scopes": [ "com.dss" ]
}
```

To the `dependencies` section:

```
"com.dss.sdf-generator": "1.0.2"
```

After changes, the manifest file should look like below:

```
{
  "scopedRegistries": [
    {
      "name": "DSS",
      "url": "https://registry.npmjs.com",
      "scopes": [ "com.dss" ]
    }
  ],
  "dependencies": {
    "com.dss.sdf-generator": "1.0.2",
    ...
```

## To Use

**In Editor**

1. Select `SDF > Generator` from the Unity menu bar, the tool window will pop up.
2. Set the relevant options (described in the next section).
3. Press `Create` and a save dialog will pop up.
3. Choose a location and name to save your SDF to.

**Through a Script**

```csharp
// create a new generator instance
DSS.SDF.Generator generator = new DSS.SDF.Generator();

// set the relevant options (described in the next section)
generator.Mesh = someMeshInstance;
generator.Resolution = 32;
// ...etc...

// generate the Texture3D
// note: this can take a long time, so definitely don't do it each frame
// and probably only do it once when the application first starts
Texture3D sdf = generator.Generate();
```

## Options

**Mesh**: the mesh you want to generate an SDF from.

**Resolution**: the resulting `Texture3D` resolution (I recommend that you keep this below 64).

**Submesh Index**: for multi-part meshes, the index of the sub-mesh you want to use.

**Padding**: the padding to surround your mesh within the SDF (only set this to be non-zero if you see artifacts).