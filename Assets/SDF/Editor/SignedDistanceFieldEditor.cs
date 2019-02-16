//
// Copyright © Daniel Shervheim, 2019
// www.danielshervheim.com
//

using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(SignedDistanceField))]
public class SignedDistanceFieldEditor : Editor {
    
    public override void OnInspectorGUI() {
        SignedDistanceField sdf = (SignedDistanceField)target;
    	
    	DrawDefaultInspector();
    	
    	if (GUILayout.Button("Bake")) {
    		sdf.Bake();
    	}
    }
    
}