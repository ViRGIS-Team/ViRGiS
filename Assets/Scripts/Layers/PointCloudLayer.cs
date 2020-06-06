using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using System.Threading.Tasks;
using Project;
using Pcx;
using GeoJSON.Net.Geometry;

namespace Virgis
{


    public class PointCloudLayer : VirgisLayer<GeographyCollection, ParticleData>
    {
        // The prefab for the data points to be instantiated
        public Material material;
        public GameObject handle;
        public GameObject pointCloud;
        public List<GameObject> meshes;
        public Material HandleMaterial;

        private GameObject model;
        private Dictionary<string, Unit> symbology;
        private Material mainMat;
        private Material selectedMat;


        protected override async Task _init(GeographyCollection layer)
        {
            PlyImport reader = new PlyImport();
            features = await reader.Load(layer.Source);
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
            transform.position = layer.Position.Coordinates.Vector3();
            transform.Translate(AppState.instance.map.transform.TransformVector((Vector3)layer.Transform.Position * AppState.instance.abstractMap.WorldRelativeScale));
            Dictionary<string, Unit> symbology = layer.Properties.Units;

            model = Instantiate(pointCloud, transform.position, Quaternion.identity);
            model.transform.parent = gameObject.transform;

            BakedPointCloud cloud = ScriptableObject.CreateInstance<BakedPointCloud>();
            ParticleData data = features as ParticleData;
            cloud.Initialize(data.vertices, data.colors);

            VisualEffect vfx = model.GetComponent<VisualEffect>();
            vfx.SetTexture("_Positions", cloud.positionMap);
            vfx.SetTexture("_Colors", cloud.colorMap);
            vfx.SetInt("_pointCount", cloud.pointCount);
            vfx.SetFloat("_pointSize", symbology["point"].Transform.Scale.magnitude);
            vfx.Play();

            transform.rotation = layer.Transform.Rotate;
            transform.localScale = layer.Transform.Scale;
            GameObject centreHandle = Instantiate(handle, gameObject.transform.position, Quaternion.identity);
            centreHandle.transform.parent = transform;
            centreHandle.transform.localScale = transform.InverseTransformVector(AppState.instance.map.transform.TransformVector((Vector3)symbology["handle"].Transform.Scale * AppState.instance.abstractMap.WorldRelativeScale));
            centreHandle.GetComponent<Datapoint>().SetMaterial(mainMat, selectedMat);
        }

        public override void Translate(MoveArgs args)
        {

            if (args.translate != Vector3.zero) transform.Translate(args.translate, Space.World);
            changed = true;
        }


        // https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
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

        public override GameObject GetFeatureShape()
        {
            return handle;
        }

        /*public override VirgisComponent GetClosest(Vector3 coords)
        {
            throw new System.NotImplementedException();
        }*/

    }
}
