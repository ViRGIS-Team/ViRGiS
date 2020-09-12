// copyright Runette Software Ltd, 2020. All rights reserved
using Project;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
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
            if (layer.Properties.isWfs) {
                await ogrReader.LoadWfs(layer.Source, 0);
            } else {
                await ogrReader.Load(layer.Source, 1);
            }
            features = ogrReader.GetLayers().ToArray();
            foreach (Layer thisLayer in features) {
                wkbGeometryType type = thisLayer.GetGeomType();
                OgrReader.Flatten(ref type);
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
                    case wkbGeometryType.wkbUnknown:
                        GeographyCollection metadata = GetMetadata();
                        if (metadata.Properties.BBox != null) {
                            thisLayer.SetSpatialFilterRect(metadata.Properties.BBox[0], metadata.Properties.BBox[1], metadata.Properties.BBox[2], metadata.Properties.BBox[3]);
                        }
                        thisLayer.ResetReading();
                        Feature feature = thisLayer.GetNextFeature();
                        while (feature != null) {
                            if (feature == null)
                                continue;
                            Geometry geom = feature.GetGeometryRef();
                            if (geom == null)
                                continue;
                            wkbGeometryType ftype = geom.GetGeometryType();
                            OgrReader.Flatten(ref ftype);
                            VirgisLayer<GeographyCollection, Layer> layerToAdd = null;
                            switch (ftype) {
                                case wkbGeometryType.wkbLineString:
                                    foreach (VirgisLayer<GeographyCollection, Layer> l in _layers) {
                                        if (l.GetType() == typeof(LineLayer)) {
                                            layerToAdd = l;
                                            break;
                                        }
                                     }
                                    if (layerToAdd == null) {
                                        _layers.Add(await Instantiate(LineLayer, transform).GetComponent<LineLayer>().Init(layer));
                                        _layers.Last().SetCrs(thisLayer.GetSpatialRef());
                                        _layers.Last().SetFeatures(thisLayer);
                                    }
                                    break;
                                case wkbGeometryType.wkbPolygon:
                                    foreach (VirgisLayer<GeographyCollection, Layer> l in _layers) {
                                        if (l.GetType() == typeof(PolygonLayer)) {
                                            layerToAdd = l;
                                            break;
                                        }
                                    }
                                    if (layerToAdd == null) {
                                        _layers.Add(await Instantiate(PolygonLayer, transform).GetComponent<PolygonLayer>().Init(layer));
                                        _layers.Last().SetCrs(thisLayer.GetSpatialRef());
                                        _layers.Last().SetFeatures(thisLayer);
                                    }
                                    break;
                                case wkbGeometryType.wkbPoint:
                                    foreach (VirgisLayer<GeographyCollection, Layer> l in _layers) {
                                        if (l.GetType() == typeof(PointLayer)) {
                                            layerToAdd = l;
                                            break;
                                        }
                                    }
                                    if (layerToAdd == null) {
                                        _layers.Add(await Instantiate(PointLayer, transform).GetComponent<PointLayer>().Init(layer));
                                        _layers.Last().SetCrs(thisLayer.GetSpatialRef());
                                        _layers.Last().SetFeatures(thisLayer);
                                    }
                                    break;
                                default:
                                    throw new NotSupportedException($"Geometry type {ftype.ToString()} is not supported");
                            }
                            feature = thisLayer.GetNextFeature();
                        }
                        break;
                }
            }
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry) {
            throw new NotImplementedException();
        }



        protected override void _draw() {
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
