using UnityEngine;
using System;
using GeoJSON.Net.Feature;


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
        CSVData CSVData;

        public Vector3 coords;
    }
}
