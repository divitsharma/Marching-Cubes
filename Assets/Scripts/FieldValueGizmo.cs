using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldValueGizmo : MonoBehaviour
{
    [Range(0f, 1f)]
    public float value;

    public Vector3 point;

    // This should be a singleton
    ScalarField scalarField;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (scalarField == null)
            scalarField = GameObject.FindObjectOfType<ScalarField>();

        scalarField.SetValue(point, value);
        //GetComponent<MeshRenderer>().material.SetColor("_Color", Color.Lerp(Color.black, Color.white, value));
    }
#endif
    
    public void SetPoint(Vector3 point)
    {
        this.point = point;
    }

    public void SetValue(float value)
    {
        this.value = value;
    }
}
