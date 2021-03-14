// copyright Runette Software Ltd, 2020. All rights reserved
using Project;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using OSGeo.OGR;
using System.Linq;
using System;

namespace Virgis {

    public class OgrLayer : VirgisLayer<RecordSet, Layer[]> {
        // The prefab for the data points to be instantiated
        public GameObject PointLayer;
        public GameObject LineLayer;
        public GameObject PolygonLayer;

        // used to read the GeoJSON file for this layer
        private OgrReader ogrReader;

        private List<VirgisLayer<RecordSet, Layer>> _layers = new List<VirgisLayer<RecordSet, Layer>>();

        private void Start() {
            isContainer = true;
        }

        private void OnDestroy() {
            ogrReader?.Dispose();
        }

        protected override async Task _init() {
            RecordSet layer = _layer as RecordSet;
            ogrReader = new OgrReader();
            if (layer.Properties.SourceType == SourceType.WFS) {
                await ogrReader.LoadWfs(layer.Source, layer.Properties.ReadOnly ? 0 : 1);
            } else {
                await ogrReader.Load(layer.Source, layer.Properties.ReadOnly ? 0 : 1);
            }
            features = ogrReader.GetLayers().ToArray();
            foreach (Layer thisLayer in features) {
                wkbGeometryType type = thisLayer.GetGeomType();
                OgrReader.Flatten(ref type);
                switch (type) {
                    case wkbGeometryType.wkbPoint:
                        _layers.Add(Instantiate(PointLayer, transform).GetComponent<PointLayer>());
                        _layers.Last().SetFeatures(thisLayer);
                        _layers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                        await _layers.Last().Init(layer);
                        break;
                    case wkbGeometryType.wkbLineString:
                        _layers.Add( Instantiate(LineLayer,transform).GetComponent<LineLayer>());
                        _layers.Last().SetFeatures(thisLayer);
                        _layers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                        await _layers.Last().Init(layer);
                        break;
                    case wkbGeometryType.wkbPolygon:
                        _layers.Add( Instantiate(PolygonLayer, transform).GetComponent<PolygonLayer>());
                        _layers.Last().SetFeatures(thisLayer);
                        _layers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                        await _layers.Last().Init(layer);
                        break;
                    case wkbGeometryType.wkbUnknown:
                        RecordSet metadata = GetMetadata();
                        if (metadata.Properties.BBox != null) {
                            thisLayer.SetSpatialFilterRect(metadata.Properties.BBox[0], metadata.Properties.BBox[1], metadata.Properties.BBox[2], metadata.Properties.BBox[3]);
                        }
                        await ogrReader.GetFeaturesAsync(thisLayer);
                        foreach (Feature feature in ogrReader.features) {
                            if (feature == null)
                                continue;
                            Geometry geom = feature.GetGeometryRef();
                            if (geom == null)
                                continue;
                            wkbGeometryType ftype = geom.GetGeometryType();
                            OgrReader.Flatten(ref ftype);
                            VirgisLayer<RecordSet, Layer> layerToAdd = null;
                            switch (ftype) {
                                case wkbGeometryType.wkbLineString:
                                    foreach (VirgisLayer<RecordSet, Layer> l in _layers) {
                                        if (l.GetType() == typeof(LineLayer)) {
                                            layerToAdd = l;
                                            break;
                                        }
                                     }
                                    if (layerToAdd == null) {
                                        _layers.Add( Instantiate(LineLayer, transform).GetComponent<LineLayer>());
                                        _layers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                                        _layers.Last().SetFeatures(thisLayer);
                                        await _layers.Last().Init(layer);
                                    }
                                    break;
                                case wkbGeometryType.wkbPolygon:
                                    foreach (VirgisLayer<RecordSet, Layer> l in _layers) {
                                        if (l.GetType() == typeof(PolygonLayer)) {
                                            layerToAdd = l;
                                            break;
                                        }
                                    }
                                    if (layerToAdd == null) {
                                        _layers.Add( Instantiate(PolygonLayer, transform).GetComponent<PolygonLayer>());
                                        _layers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                                        _layers.Last().SetFeatures(thisLayer);
                                        await _layers.Last().Init(layer);
                                    }
                                    break;
                                case wkbGeometryType.wkbPoint:
                                    foreach (VirgisLayer<RecordSet, Layer> l in _layers) {
                                        if (l.GetType() == typeof(PointLayer)) {
                                            layerToAdd = l;
                                            break;
                                        }
                                    }
                                    if (layerToAdd == null) {
                                        _layers.Add( Instantiate(PointLayer, transform).GetComponent<PointLayer>());
                                        _layers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                                        _layers.Last().SetFeatures(thisLayer);
                                        await _layers.Last().Init(layer);
                                    }
                                    break;
                                default:
                                    throw new NotSupportedException($"Geometry type {ftype.ToString()} is not supported");
                            }
                        }
                        break;
                }
                return;
            }
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry) {
            throw new NotImplementedException();
        }



        protected override Task _draw() {
            return Task.CompletedTask;
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
