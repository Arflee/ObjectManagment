using UnityEngine;

[System.Serializable]
public class ColorRangeHSV
{
    [FloatRangeSlider(0f, 1f)]
    public FloatRange hue, saturation, value;

    public Color RandomInRange
    {
        get
        {
            return Random.ColorHSV(
                hue.min, hue.max,
                saturation.min, saturation.max,
                value.min, value.max,
                1f, 1f
            );
        }
    }
}