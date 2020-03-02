// from https://answers.unity.com/questions/956047/serialize-quaternion-or-vector3.html

using UnityEngine;
 using System;
using System.Collections.Generic;

 
 /// <summary>
 /// Since unity doesn't flag the Vector3 as serializable, we
 /// need to create our own version. This one will automatically convert
 /// between Vector3 and SerializableVector3
 /// </summary>
 [System.Serializable]
 public struct SerializableVector3
 {
     /// <summary>
     /// x component
     /// </summary>
     public float x;
     
     /// <summary>
     /// y component
     /// </summary>
     public float y;
     
     /// <summary>
     /// z component
     /// </summary>
     public float z;
     
     /// <summary>
     /// Constructor
     /// </summary>
     /// <param name="rX"></param>
     /// <param name="rY"></param>
     /// <param name="rZ"></param>
     public SerializableVector3(float rX, float rY, float rZ)
     {
         x = rX;
         y = rY;
         z = rZ;
     }

    public SerializableVector3(IList<float> r)
    {
        x = r[0];
        y = r[1];
        z = r[2];
    }

    /// <summary>
    /// Returns a string representation of the object
    /// </summary>
    /// <returns></returns>
    public override string ToString()
     {
         return String.Format("[{0}, {1}, {2}]", x, z, y);
     }
     
     /// <summary>
     /// Automatic conversion from SerializableVector3 to Vector3
     /// </summary>
     /// <param name="rValue"></param>
     /// <returns></returns>
     public static implicit operator Vector3(SerializableVector3 rValue)
     {
         return new Vector3(rValue.x, rValue.y, rValue.z);
     }
     
     /// <summary>
     /// Automatic conversion from Vector3 to SerializableVector3
     /// </summary>
     /// <param name="rValue"></param>
     /// <returns></returns>
     public static implicit operator SerializableVector3(Vector3 rValue)
     {
         return new SerializableVector3(rValue.x, rValue.y, rValue.z);
     }
 }

[System.Serializable]
public struct SerializableQuaternion
{
    /// <summary>
    /// x component
    /// </summary>
    public float x;

    /// <summary>
    /// y component
    /// </summary>
    public float y;

    /// <summary>
    /// z component
    /// </summary>
    public float z;

    /// <summary>
    /// w component
    /// </summary>
    public float w;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="rX"></param>
    /// <param name="rY"></param>
    /// <param name="rZ"></param>
    /// <param name="rW"></param>
    public SerializableQuaternion(float rX, float rY, float rZ, float rW)
    {
        x = rX;
        y = rY;
        z = rZ;
        w = rW;
    }

    public SerializableQuaternion(IList<float> r)
    {
        x = r[0];
        y = r[1];
        z = r[2];
        w = r[3];
    }

    /// <summary>
    /// Returns a string representation of the object
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return String.Format("[{0}, {1}, {2}, {3}]", x, z, y, w);
    }

    /// <summary>
    /// Automatic conversion from SerializableQuaternion to Quaternion
    /// </summary>
    /// <param name="rValue"></param>
    /// <returns></returns>
    public static implicit operator Quaternion(SerializableQuaternion rValue)
    {
        return new Quaternion(rValue.x, rValue.y, rValue.z, rValue.w);
    }

    /// <summary>
    /// Automatic conversion from Quaternion to SerializableQuaternion
    /// </summary>
    /// <param name="rValue"></param>
    /// <returns></returns>
    public static implicit operator SerializableQuaternion(Quaternion rValue)
    {
        return new SerializableQuaternion(rValue.x, rValue.y, rValue.z, rValue.w);
    }
}
