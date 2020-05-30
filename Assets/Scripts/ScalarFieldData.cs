using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ScalarFieldData", menuName = "ScriptableObjects/ScalarFieldData")]
public class ScalarFieldData : ScriptableObject
{
    public float[] values;

    public int height;
    public int length;
    public int width;

}
