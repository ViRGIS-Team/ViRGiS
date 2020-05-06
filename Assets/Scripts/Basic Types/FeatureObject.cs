using UnityEngine;
using System;
using GeoJSON.Net.Feature;
using UnityEngine.InputSystem.XR.Haptics;

namespace Virgis
{

    /// <summary>
    /// abstract parent for generic datasets
    /// </summary>
    public abstract class FeatureObject : ScriptableObject
    {
        Type Type;
        FeatureCollection FeatURes;
        MeshData mesh;
        ParticleData PointCloud;

        public Vector3 coords;
    }
}
