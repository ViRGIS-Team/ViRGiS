// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using Project;
using System;

namespace Virgis
{

    /// <summary>
    /// Controls an instance of a Polygon Layer
    /// </summary>
    public class XsectLayer: VirgisLayer<GeologyCollection, EgbFeatureCollection>
    {

        // The prefab for the data points to be instantiated
        public GameObject PolygonPrefab; // Prefab to be used for the polygons
        public Material Mat; // Material to be used for the Polygon
        public string ImageSource;

        private EgbReader egbReader;
        Texture2D tex;

        private void Start() {
            featureType = FeatureType.POLYGON;
        }


        protected override async Task _init() {
            GeologyCollection layer = _layer as GeologyCollection;
            egbReader = new EgbReader();
            await egbReader.Load(layer.Source);
            egbReader.Read();
            features = egbReader.features;
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry)
        {
            throw new System.NotImplementedException();
        }

        protected async override void _draw()
        {
            RecordSet layer = GetMetadata();


            foreach (EgbFeature feature in features)
            {
                Dictionary<string, object> properties = feature.image;
                string gisId = feature.Id;


                // Get the geometry
                //Create the GameObjects
                // GameObject dataLine = Instantiate(LinePrefab, origin, Quaternion.identity);
                GameObject dataPoly = Instantiate(PolygonPrefab, transform);
                if (layer.Transform != null) {
                    dataPoly.transform.position = AppState.instance.map.transform.TransformPoint(layer.Transform.Position);
                    dataPoly.transform.rotation = layer.Transform.Rotate;
                    dataPoly.transform.localScale = layer.Transform.Scale;
                }

                Vector3[] top = feature.top.TransformWorld();
                Vector3[] bottom = feature.bottom.TransformWorld();

                Dataplane com = dataPoly.GetComponent<Dataplane>();
                com.Draw(top, bottom, Mat);

                tex = null;
                if (feature.image.ContainsKey("Image") && feature.image["Image"] != null) {
                    string Url = "file:///" + Directory.GetCurrentDirectory() +  ImageSource + feature.image["Image"] as string;
                    tex = await TextureImage.Get(new Uri(Url));
                    if (tex != null) {
                        tex.wrapMode = TextureWrapMode.Clamp;
                    }
                }
                Material newMat = dataPoly.GetComponentInChildren<Renderer>().material;
                if (tex != null) newMat.SetTexture("_BaseMap", tex);

                // add the gis data from EGB
                com.gisId = gisId;
                com.gisProperties = properties;

            }
        }

        protected override void _checkpoint() { }
        protected override Task _save()
        {
            return Task.CompletedTask;
        }
    }
}
