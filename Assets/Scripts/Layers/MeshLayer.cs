using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using Project;
using g3;
using System;
using GeoJSON.Net.Geometry;

namespace Virgis
{

    public class MeshLayer : VirgisLayer<GeographyCollection, MeshData>
    {
        // The prefab for the data points to be instantiated
        public Material material;
        public GameObject handle;
        public List<GameObject> meshes;
        public Material HandleMaterial;

        private Dictionary<string, Unit> symbology;
        private Material mainMat;
        private Material selectedMat;

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
            symbology = layer.Properties.Units;

            Color col = symbology.ContainsKey("point") ? (Color)symbology["point"].Color : Color.white;
            Color sel = symbology.ContainsKey("point") ? new Color(1 - col.r, 1 - col.g, 1 - col.b, col.a) : Color.red;
            mainMat = Instantiate(HandleMaterial);
            mainMat.SetColor("_BaseColor", col);
            selectedMat = Instantiate(HandleMaterial);
            selectedMat.SetColor("_BaseColor", sel);
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry)
        {
            throw new System.NotImplementedException();
        }
        protected override void _draw()
        {
            GeographyCollection layer = GetMetadata();
            transform.position = layer.Position.ToVector3();
            transform.Translate(AppState.instance.map.transform.TransformVector((Vector3)layer.Transform.Position ));
            Dictionary<string, Unit> symbology = GetMetadata().Properties.Units;
            meshes = new List<GameObject>();

            foreach (SimpleMesh simpleMesh in (features as MeshData).Mesh.Meshes)
            {
                GameObject meshGameObject = new GameObject();
                MeshFilter mf = meshGameObject.AddComponent<MeshFilter>();
                MeshRenderer renderer = meshGameObject.AddComponent<MeshRenderer>();
                renderer.material = material;
                meshGameObject.transform.localScale = AppState.instance.map.transform.localScale;
                meshGameObject.transform.parent = transform;
                meshGameObject.transform.localPosition = Vector3.zero;
                mf.mesh = simpleMesh.ToMesh();
                meshes.Add(meshGameObject);
            }
            transform.rotation = layer.Transform.Rotate;
            transform.localScale = layer.Transform.Scale;
            GameObject centreHandle = Instantiate(handle, transform.position, Quaternion.identity);
            centreHandle.transform.localScale = AppState.instance.map.transform.TransformVector((Vector3) symbology["handle"].Transform.Scale);
            centreHandle.GetComponent<Datapoint>().SetMaterial(mainMat, selectedMat);
            centreHandle.transform.parent = transform;

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
            _layer.Position = transform.position.ToPoint();
            _layer.Transform.Position = Vector3.zero;
            _layer.Transform.Rotate = transform.rotation;
            _layer.Transform.Scale = transform.localScale;
        }
    }
}

