// from https://answers.unity.com/questions/956047/serialize-quaternion-or-vector3.html

using UnityEngine;
 using System;
using System.Collections.Generic;

namespace Project
{


    /// <summary>
    /// Since unity doesn't flag the Vector3 as serializable, we
    /// need to create our own version. This one will automatically convert
    /// between Vector3 and SerializableVector3
    /// </summary>
    [System.Serializable]
    public class SerializableVector3 : Serializable
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

        public SerializableVector3() { }

        public override void Update(IList<float> r)
        {
            x = r[0];
            y = r[2];
            z = r[1];
        }

        /// <summary>
        /// Returns a string representation of the object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("[{0}, {1}, {2}]", x, z, y);
        }

        public override float[] ToArray()
        {
            return new float[3] { x, z, y };
        }

        public new float magnitude
        {
            get
            {
                Vector3 v = this;
                return v.magnitude;
            }
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
    public class SerializableQuaternion : Serializable
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

        public SerializableQuaternion() { }

        public override void Update(IList<float> r)
        {
            x = r[0];
            y = r[2];
            z = r[1];
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

        public override float[] ToArray()
        {
            return new float[4] { x, z, y, w };
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

    [System.Serializable]
    public class SerializableColor : Serializable
    {

        public float r;
        public float g;
        public float b;
        public float a;


        public SerializableColor(float rr, float rg, float rb, float ra)
        {
            r = rr;
            g = rg;
            b = rb;
            a = ra;
        }

        public SerializableColor() { }

        public override void Update(IList<float> color)
        {
            r = color[0] / 255;
            g = color[1] / 255;
            b = color[2] / 255;
            a = color[3];
        }

        //makes this class usable as Color, Color normalColor = mySerializableColor;
        public static implicit operator Color(SerializableColor r)
        {
            return new Color(r.r, r.g, r.b, r.a); ;
        }

        //makes this class assignable by Color, SerializableColor myColor = Color.white;
        public static implicit operator SerializableColor(Color color)
        {
            return new SerializableColor(color.r, color.g, color.b, color.a);
        }

        public override string ToString()
        {
            return String.Format("[{0}, {1}, {2}, {3}]", r * 255f, g * 255f, b * 255f, a);
        }

        public override float[] ToArray()
        {
            return new float[4] { r * 255, g * 255, b * 255, a };
        }
    }

    public abstract class Serializable
    {
        public abstract void Update(IList<float> v);
        public abstract float[] ToArray();
        public float magnitude;
    }
}
