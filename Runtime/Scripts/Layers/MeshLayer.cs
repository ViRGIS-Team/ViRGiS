using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using Project;
using g3;
using System;


namespace Virgis
{

    public class MeshLayer : VirgisLayer<RecordSet, List<DMesh3>>
    {
        // The prefab for the data points to be instantiated
        public GameObject Mesh;
        public Material MeshMaterial;

        private List<Transform> meshes;
        private Dictionary<string, Unit> symbology;

        private void Start() {
            featureType = FeatureType.MESH;
        }

        private async Task<DMesh3Builder> loadObj(string filename)
        {
            using (StreamReader reader = File.OpenText(filename))
            {
                OBJReader objReader = new OBJReader();
                DMesh3Builder meshBuilder = new DMesh3Builder();
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
                    meshBuilder = new DMesh3Builder();
                }
                return meshBuilder;
            }
        }

        protected override async Task _init() {
            RecordSet layer = _layer as RecordSet;
            DMesh3Builder meshes = await loadObj(layer.Source);
            features = meshes.Meshes;
            symbology = layer.Properties.Units;
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry)
        {
            throw new System.NotImplementedException();
        }
        protected override void _draw()
        {
            RecordSet layer = GetMetadata();
            transform.position = layer.Position != null ? layer.Position.ToVector3() : Vector3.zero;
            transform.Translate(AppState.instance.map.transform.TransformVector((Vector3) layer.Transform.Position));
            Dictionary<string, Unit> symbology = GetMetadata().Properties.Units;
            meshes = new List<Transform>();

            foreach (DMesh3 dMesh in features) {
                meshes.Add(Instantiate(Mesh, transform).GetComponent<DataMesh>().Draw(dMesh, MeshMaterial));
            }
            transform.rotation = layer.Transform.Rotate;
            transform.localScale = layer.Transform.Scale;

        }

        public override void Translate(MoveArgs args)
        {
                if (args.translate != Vector3.zero) transform.Translate(args.translate, Space.World);
                changed = true;
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

        protected override Task _save()
        {
            _layer.Position = transform.position.ToPoint();
            _layer.Transform.Position = Vector3.zero;
            _layer.Transform.Rotate = transform.rotation;
            _layer.Transform.Scale = transform.localScale;
            return Task.CompletedTask;
        }
    }
}

