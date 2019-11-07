using Terrain;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates a "Build" button in the TerrainController editor window.
/// </summary>
[CustomEditor(typeof(TerrainController))]
[CanEditMultipleObjects]
public class TerrainControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var myScript = (TerrainController) target;
        DrawDefaultInspector();
        if (GUILayout.Button("Build Textures and Terrain"))
        {
            myScript.Build();
        }
    }
}