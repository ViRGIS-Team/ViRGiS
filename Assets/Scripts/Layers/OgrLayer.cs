// copyright Runette Software Ltd, 2020. All rights reserved
using Project;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using OSGeo.GDAL;
using OSGeo.OGR;
using System.Linq;
using System;

namespace Virgis {

    public class OgrLayer : VirgisLayer<GeographyCollection, Layer[]> {
        // The prefab for the data points to be instantiated
        public GameObject PointLayer;
        public GameObject LineLayer;
        public GameObject PolygonLayer;


        // used to read the GeoJSON file for this layer
        private OgrReader ogrReader;

        private List<VirgisLayer<GeographyCollection, Layer>> _layers = new List<VirgisLayer<GeographyCollection, Layer>>();


        protected override async Task _init() {
            GeographyCollection layer = _layer as GeographyCollection;
            ogrReader = new OgrReader();
            await ogrReader.Load(layer.Source);
            features = ogrReader.GetLayers().ToArray();
            foreach (Layer thisLayer in features) {
                wkbGeometryType type = OgrReader.Flatten(new Geometry(thisLayer.GetGeomType()));
                switch (type) {
                    case wkbGeometryType.wkbPoint:
                        _layers.Add( await Instantiate(PointLayer,transform).GetComponent<PointLayer>().Init(layer));
                        _layers.Last().SetFeatures(thisLayer);
                        _layers.Last().SetCrs(thisLayer.GetSpatialRef());
                        break;
                    case wkbGeometryType.wkbLineString:
                        _layers.Add(await Instantiate(LineLayer,transform).GetComponent<LineLayer>().Init(layer));
                        _layers.Last().SetFeatures(thisLayer);
                        _layers.Last().SetCrs(thisLayer.GetSpatialRef());
                        break;
                    case wkbGeometryType.wkbPolygon:
                        _layers.Add(await Instantiate(PolygonLayer, transform).GetComponent<PolygonLayer>().Init(layer));
                        _layers.Last().SetFeatures(thisLayer);
                        _layers.Last().SetCrs(thisLayer.GetSpatialRef());
                        break;
                }
            }
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry) {
            throw new NotImplementedException();
        }



        protected override void _draw() {
            throw new NotImplementedException();
        }


        protected override void _checkpoint() {
        }


        protected async override Task _save() {

            foreach (VirgisLayer thisLayer in _layers) {
                await thisLayer.Save();
            }
            return;
        }
    }
}
