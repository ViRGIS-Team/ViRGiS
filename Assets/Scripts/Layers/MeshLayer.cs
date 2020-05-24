﻿using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using Project;
using g3;
using System;
using GeoJSON.Net.Geometry;

namespace Virgis
{

    public class MeshLayer : Layer<GeographyCollection, MeshData>
    {
        // The prefab for the data points to be instantiated
        public Material material;
        public GameObject handle;
        public List<GameObject> meshes;

        private async Task<SimpleMeshBuilder> loadObj(string filename)
        {
            using (StreamReader reader = File.OpenText(filename))
            {
                OBJReader objReader = new OBJReader();
                SimpleMeshBuilder meshBuilder = new SimpleMeshBuilder();
                try
                {
                    IOReadResult result = objReader.Read(reader, new ReadOptions(), meshBuilder);
                }
                catch (Exception e) when (
                 e is UnauthorizedAccessException ||
                 e is DirectoryNotFoundException ||
                 e is FileNotFoundException ||
                 e is NotSupportedException
                 )
                {
                    Debug.LogError("Failed to Load" + filename + " : " + e.ToString());
                    meshBuilder = new SimpleMeshBuilder();
                }
                return meshBuilder;
            }
        }

        protected override async Task _init(GeographyCollection layer)
        {
            MeshData Mesh = new MeshData();
            Mesh.Mesh = await loadObj(layer.Source);
            features = Mesh;
        }

        protected override void _addFeature(MoveArgs args)
        {
            throw new System.NotImplementedException();
        }
        protected override void _draw()
        {
            transform.position = layer.Position.Coordinates.Vector3();
            transform.Translate(AppState.instance.map.transform.TransformVector((Vector3)layer.Transform.Position * AppState.instance.abstractMap.WorldRelativeScale));
            Dictionary<string, Unit> symbology = layer.Properties.Units;
            meshes = new List<GameObject>();

            foreach (SimpleMesh simpleMesh in (features as MeshData).Mesh.Meshes)
            {
                GameObject meshGameObject = new GameObject();
                MeshFilter mf = meshGameObject.AddComponent<MeshFilter>();
                MeshRenderer renderer = meshGameObject.AddComponent<MeshRenderer>();
                renderer.material = material;
                meshGameObject.transform.parent = gameObject.transform;
                meshGameObject.transform.localPosition = Vector3.zero;
                mf.mesh = simpleMesh.ToMesh();
                meshes.Add(meshGameObject);
            }
            transform.rotation = layer.Transform.Rotate;
            transform.localScale = layer.Transform.Scale;
            GameObject centreHandle = Instantiate(handle, gameObject.transform.position, Quaternion.identity);
            centreHandle.transform.parent = transform;
            centreHandle.transform.localScale = transform.InverseTransformVector(AppState.instance.map.transform.TransformVector((Vector3)symbology["handle"].Transform.Scale * AppState.instance.abstractMap.WorldRelativeScale));
            centreHandle.SendMessage("SetColor", (Color)symbology["handle"].Color);

        }

        public override void Translate(MoveArgs args)
        {
            foreach (GameObject mesh in meshes)
            {
                if (args.translate != Vector3.zero) transform.Translate(args.translate, Space.World);
                changed = true;
            }
        }

        /// https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
        public override void MoveAxis(MoveArgs args)
        {
            if (args.translate != Vector3.zero) transform.Translate(args.translate, Space.World);
            args.rotate.ToAngleAxis(out float angle, out Vector3 axis);
            transform.RotateAround(args.pos, axis, angle);
            Vector3 A = transform.localPosition;
            Vector3 B = transform.parent.InverseTransformPoint(args.pos);
            Vector3 C = A - B;
            float RS = args.scale;
            Vector3 FP = B + C * RS;
            if (FP.magnitude < float.MaxValue)
            {
                transform.localScale = transform.localScale * RS;
                transform.localPosition = FP;
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform T = transform.GetChild(i);
                    if (T.GetComponent<Datapoint>() != null)
                    {
                        T.localScale /= RS;
                    }
                }
            }
            changed = true;
        }

        protected override void _checkpoint() { }

        protected override void _save()
        {
            layer.Position = transform.position.ToPoint();
            layer.Transform.Position = Vector3.zero;
            layer.Transform.Rotate = transform.rotation;
            layer.Transform.Scale = transform.localScale;
        }

        public override GameObject GetFeatureShape() {
            return handle;
        }

    }
}

