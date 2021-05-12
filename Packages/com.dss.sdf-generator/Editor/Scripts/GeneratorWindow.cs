// Copyright 2021 Daniel Shervheim

using UnityEngine;
using UnityEditor;

namespace DSS.SDF {

public class GeneratorWindow : EditorWindow {
  private Generator generator = new Generator();

  [MenuItem("SDF/Generator")]
  private static void Window() {
    GeneratorWindow window  = (GeneratorWindow)EditorWindow.GetWindow(typeof(GeneratorWindow), true, "Generate SDF");
    window.ShowUtility();
  }

  private void OnGUI() {
    // Verify that compute shaders are supported.
    if (!SystemInfo.supportsComputeShaders) {
      EditorGUILayout.HelpBox("This tool requires a GPU that supports compute shaders.", MessageType.Error);

      if (GUILayout.Button("Close")) {
        Close();
      }
      return;
    }

    // Assign the mesh.
    generator.Mesh = EditorGUILayout.ObjectField("Mesh", generator.Mesh, typeof(Mesh), false) as Mesh;

    // If the mesh is null, don't draw the rest of the GUI.
    if (generator.Mesh == null) {
      if (GUILayout.Button("Close")) {
        Close();
      }
      return;
    }

    // Assign the sub-mesh index, if there are more than 1 in the mesh.
    if (generator.Mesh.subMeshCount > 1) {
      generator.SubMeshIndex = (int)Mathf.Max(EditorGUILayout.IntField("Submesh Index", generator.SubMeshIndex), 0f);
    }

    // Assign the padding around the mesh.
    generator.Padding = EditorGUILayout.Slider("Padding", generator.Padding, 0f, 1f);

    // Assign the SDF resolution.
    generator.Resolution = (int)Mathf.Max(EditorGUILayout.IntField("Resolution", generator.Resolution), 1f);

    if (GUILayout.Button("Create")) {
      CreateSDF();
    }

    if (GUILayout.Button("Close")) {
      Close();
    }
  }

  private void OnInspectorUpdate() {
    Repaint();
  }

  private void CreateSDF() {
    // Prompt the user to save the file.
    string path = EditorUtility.SaveFilePanelInProject("Save As", generator.Mesh.name + "_SDF", "asset", "");

    // ... If they hit cancel.
    if (path == null || path.Equals("")) {
      return;
    }

    // Get the Texture3D representation of the SDF.
    Texture3D voxels = generator.Generate();

    // Save the Texture3D asset at path.
    AssetDatabase.CreateAsset(voxels, path);
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    // Close the window.
    Close();

    // Select the SDF in the project view.
    Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path);
  }
}

}  // namespace DSS.SDF
