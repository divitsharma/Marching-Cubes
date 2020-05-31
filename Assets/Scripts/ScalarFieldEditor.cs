using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(ScalarField))]
public class ScalarFieldEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ScalarField fieldTarget = (ScalarField)target;

        if (GUILayout.Button("Print Values"))
        {
                for (int z = 0; z < fieldTarget.Width; z++)
                {
                    string s = "";
                    for (int y = 0; y < fieldTarget.Height; y++)
                    {
                        for (int x = 0; x < fieldTarget.Length; x++)
                        {
                            s += fieldTarget.ValueAt(new Vector3(x, y, z)) + " ";
                        }
                        s += "\n";
                    }
                    Debug.Log(s);
                }
        }

        if (GUILayout.Button("Clear Values"))
        {
            fieldTarget.GenerateValues(0);
        }

        if (GUILayout.Button("Regenerate Values"))
        {
            fieldTarget.GenerateValues();
        }
    }
}
#endif
