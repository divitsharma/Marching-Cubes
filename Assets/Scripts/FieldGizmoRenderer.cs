using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


public class FieldGizmoRenderer : MonoBehaviour
{

    ScalarField scalarField;
    public bool hideUnderValued;

    public GameObject gizmoPrefab;
    List<GameObject> gizmos = new List<GameObject>();

    [SerializeField] bool showUnityGizmos = false;

    private void Start()
    {
        scalarField.AddObserver(UpdateGizmos);
    }

#if UNITY_EDITOR
    // Don't do everything in edit mode!
    private void OnValidate()
    {
        if (scalarField == null)
            scalarField = GameObject.FindObjectOfType<ScalarField>();

        scalarField.RemoveObserver(UpdateGizmos);
    }
#endif


    private void OnDrawGizmos()
    {
        if (!showUnityGizmos)
        {
            return;
        }

        float gizmoDrawScale = scalarField.GridScale;
        for (int x = 0; x < scalarField.Length; x++)
        {
            for (int y = 0; y < scalarField.Height; y++)
            {
                for (int z = 0; z < scalarField.Width; z++)
                {
                    float value = scalarField.ValueAt(new Vector3(x, y, z));

                    if (!hideUnderValued || scalarField.ValueAt(x,y,z) > scalarField.GetSurfaceLevel())
                    {
                        Gizmos.color = Color.Lerp(Color.black, Color.white, value);
                        Gizmos.DrawSphere(new Vector3(x, y, z) * gizmoDrawScale, 1f / gizmoDrawScale);
                    }
                }
            }
        }
    }


    void UpdateGizmos(ScalarField s)
    {
        if (gizmos == null || gizmos.Count == 0)
        {
            //Debug.LogError("Gizmos list doesn't exist");
            return;
        }

        for (int x = 0; x < scalarField.Length; x++)
        {
            for (int y = 0; y < scalarField.Height; y++)
            {
                for (int z = 0; z < scalarField.Width; z++)
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


        float gizmoDrawScale = scalarField.GridScale;
        for (int x = 0; x < scalarField.Length; x++)
        {
            for (int y = 0; y < scalarField.Height; y++)
            {
                for (int z = 0; z < scalarField.Width; z++)
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

    public void ClearGizmos()
    {
        foreach (GameObject go in gizmos)
        {
            DestroyImmediate(go);
        }
        gizmos.RemoveRange(0, gizmos.Count);

        //scalarField.GenerateValues(scalarField.gridScale, value);
    }


}

#if UNITY_EDITOR

[CustomEditor(typeof(FieldGizmoRenderer))]
public class FieldGizmoRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        FieldGizmoRenderer fieldTarget = (FieldGizmoRenderer)target;

        if (GUILayout.Button("Instantiate Gizmos"))
        {
            fieldTarget.RenderGizmos(null);
        }

        if (GUILayout.Button("Clear Gizmos"))
        {
            fieldTarget.ClearGizmos();
        }
    }
}

#endif
