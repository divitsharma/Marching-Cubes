

[System.Serializable]
public class ScalarFieldData
{
    [UnityEngine.HideInInspector]
    public float[] values;

    public int height;
    public int length;
    public int width;

    public ScalarFieldData(int length, int height, int width)
    {
        this.length = length;
        this.height = height;
        this.width = width;
    }
}
