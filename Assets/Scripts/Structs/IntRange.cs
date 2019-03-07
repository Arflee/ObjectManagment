using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct IntRange
{
    public int max, min;

    public int RandomValueInRange
    {
        get
        {
            return Random.Range(min, max + 1);
        }
    }
}
