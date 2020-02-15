using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


public class FieldGizmoRenderer : MonoBehaviour
{

    ScalarField scalarField;
    public bool hideUnderValued;

    public GameObject gizmoPrefab;
    List<GameObject> gizmos = new List<GameObject>();

    private void Start()
    {
        scalarField.AddObserver(UpdateGizmos);
    }

    // Don't do everything in edit mode!
    private void OnValidate()
    {
        if (scalarField == null)
            scalarField = GameObject.FindObjectOfType<ScalarField>();

        scalarField.AddObserver(UpdateGizmos);
    }


    void UpdateGizmos(ScalarField s)
    {
        if (gizmos == null || gizmos.Count == 0)
        {
            Debug.LogError("Gizmos list doesn't exist");
            return;
        }

        for (int x = 0; x < scalarField.Resolution; x++)
        {
            for (int y = 0; y < scalarField.Resolution; y++)
            {
                for (int z = 0; z < scalarField.Resolution; z++)
                {
                    // so annoyyyyingnnng
                    FieldValueGizmo gizmo = gizmos[s.ToArrayIndex(x, y, z)].GetComponent<FieldValueGizmo>();
                    gizmo.SetValue(s.ValueAt(x, y, z));
                    if (hideUnderValued && s.ValueAt(x,y,z) <= s.GetSurfaceLevel())
                    {
                        gizmo.gameObject.SetActive(false);
                    }
                    else
                    {
                        gizmo.gameObject.SetActive(true);
                    }
                }
            }
        }
    }


    public void RenderGizmos(ScalarField s)
    {
        Debug.Log("Rendering gizmos");
        foreach (GameObject go in gizmos)
        {
            DestroyImmediate(go);
        }
        gizmos.RemoveRange(0, gizmos.Count);


        float gizmoDrawScale = scalarField.GridScale / (scalarField.Resolution - 1);
        for (int x = 0; x < scalarField.Resolution; x++)
        {
            for (int y = 0; y < scalarField.Resolution; y++)
            {
                for (int z = 0; z < scalarField.Resolution; z++)
                {
                    float value = scalarField.ValueAt(new Vector3(x,y,z));
                    // Instantiate gizmo
                    GameObject inst = Instantiate(gizmoPrefab, new Vector3(x, y, z) * gizmoDrawScale, Quaternion.identity, transform);
                    inst.GetComponent<FieldValueGizmo>().SetPoint(new Vector3(x, y, z));
                    inst.GetComponent<FieldValueGizmo>().SetValue(value);
                    inst.transform.localScale = Vector3.one * 0.05f;
                    gizmos.Add(inst);
                    // Hide if neeeded
                    if (hideUnderValued && value <= scalarField.GetSurfaceLevel())
                    {
                        inst.SetActive(false);
                    }
                }
            }
        }
    }

    public void ResetField()
    {
        foreach (GameObject go in gizmos)
        {
            DestroyImmediate(go);
        }
        gizmos.RemoveRange(0, gizmos.Count);

        scalarField.GenerateValues(scalarField.Resolution, scalarField.gridScale);

    }

    public void PrintValues()
    {
        for (int z = 0; z < scalarField.Resolution; z++)
        {
            string s = "";
            for (int y = 0; y < scalarField.Resolution; y++)
            {
                for (int x = 0; x < scalarField.Resolution; x++)
                {
                    s += scalarField.ValueAt(new Vector3(x, y, z)) + " ";
                }
                s += "\n";
            }
            Debug.Log(s);
        }
    }
}

[CustomEditor(typeof(FieldGizmoRenderer))]
public class FieldGizmoRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        FieldGizmoRenderer fieldTarget = (FieldGizmoRenderer)target;

        if (GUILayout.Button("Render"))
        {
            fieldTarget.RenderGizmos(null);
        }

        if (GUILayout.Button("Print values"))
        {
            fieldTarget.PrintValues();
        }

        if (GUILayout.Button("Reset field"))
        {
            fieldTarget.ResetField();
        }
    }
}
