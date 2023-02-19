using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
[Serializable]
public class MPVector3
{
    public float x, y, z;
    public MPVector3(float _x, float _y, float _z)
    {
        x = _x;
        y = _y;
        z = _z;
    }
    static public Vector3 ConvertVector3(MPVector3 vector)
    {
        return new Vector3(vector.x, vector.y, vector.z);
    }
    static public MPVector3 ConvertMPVector3(Vector3 vector)
    {
        return new MPVector3(vector.x, vector.y, vector.z);
    }
}
