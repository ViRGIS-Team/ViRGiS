// copyright Runette Software Ltd, 2020. All rights reserved
using Project;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using OSGeo.OGR;
using System.Linq;
using System;

namespace Virgis {

    public class OgrContainerLayer : ContainerLayer<RecordSet, Layer> {
    }

    public class OgrLayer : ContainerLayer<RecordSet, Layer[]> {
        // The prefab for the data points to be instantiated
        public GameObject PointLayer;
        public GameObject LineLayer;
        public GameObject PolygonLayer;
        public GameObject TinLayer;
        public GameObject ContainerLayer;

        // used to read the GeoJSON file for this layer
        private OgrReader ogrReader;


        private void OnDestroy() {
            ogrReader?.Dispose();
        }

        protected override async Task _init() {
            //
            // Load Dataset
            //
            RecordSet layer = _layer as RecordSet;
            using (ogrReader = new OgrReader()) {
                await ogrReader.Load(layer.Source, layer.Properties.ReadOnly ? 0 : 1, layer.Properties.SourceType);

                //
                // Get and process features
                //
                features = ogrReader.GetLayers().ToArray();
                foreach (Layer thisLayer in features) {
                    string name = thisLayer.GetName();
                    RecordSet newLayer = new RecordSet() { DisplayName = name };
                    GameObject go = Instantiate(ContainerLayer, transform);
                    OgrContainerLayer container = go.AddComponent<OgrContainerLayer>();
                    List<VirgisLayer<RecordSet, Layer>> _layers = new List<VirgisLayer<RecordSet, Layer>>();
                    container.SetCrs(OgrReader.getSR(thisLayer, layer));
                    container.SetFeatures(thisLayer);
                    await container.Init(newLayer);
                    wkbGeometryType type = thisLayer.GetGeomType();
                    FeatureDefn ld = thisLayer.GetLayerDefn();
                    int geoCount = ld.GetGeomFieldCount();
                    if (geoCount > 1)
                        type = wkbGeometryType.wkbUnknown;
                    OgrReader.Flatten(ref type);
                    switch (type) {
                        case wkbGeometryType.wkbPoint:
                            _layers.Add(Instantiate(PointLayer, container.transform).GetComponent<PointLayer>());
                            _layers.Last().SetFeatures(thisLayer);
                            _layers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                            await _layers.Last().Init(layer);
                            break;
                        case wkbGeometryType.wkbLineString:
                            _layers.Add(Instantiate(LineLayer, container.transform).GetComponent<LineLayer>());
                            _layers.Last().SetFeatures(thisLayer);
                            _layers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                            await _layers.Last().Init(layer);
                            break;
                        case wkbGeometryType.wkbPolygon:
                            _layers.Add(Instantiate(PolygonLayer, container.transform).GetComponent<PolygonLayer>());
                            _layers.Last().SetFeatures(thisLayer);
                            _layers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                            await _layers.Last().Init(layer);
                            break;
                        case wkbGeometryType.wkbTIN:
                            _layers.Add(Instantiate(TinLayer, container.transform).GetComponent<TinLayer>());
                            _layers.Last().SetFeatures(thisLayer);
                            _layers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                            await _layers.Last().Init(layer);
                            break;
                        //
                        // If feature type is unknown, process each feature seperately
                        //
                        case wkbGeometryType.wkbUnknown:
                            RecordSet metadata = GetMetadata();
                            if (metadata.Properties.BBox != null) {
                                thisLayer.SetSpatialFilterRect(metadata.Properties.BBox[0], metadata.Properties.BBox[1], metadata.Properties.BBox[2], metadata.Properties.BBox[3]);
                            }
                            await ogrReader.GetFeaturesAsync(thisLayer);
                            foreach (Feature feature in ogrReader.features) {
                                if (feature == null)
                                    continue;
                                for (int j = 0; j < geoCount; j++) {
                                    Geometry geom = feature.GetGeomFieldRef(j);
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
                                                _layers.Add(Instantiate(LineLayer, container.transform).GetComponent<LineLayer>());
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
                                                _layers.Add(Instantiate(PolygonLayer, container.transform).GetComponent<PolygonLayer>());
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
                                                _layers.Add(Instantiate(PointLayer, container.transform).GetComponent<PointLayer>());
                                                _layers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                                                _layers.Last().SetFeatures(thisLayer);
                                                await _layers.Last().Init(layer);
                                            }
                                            break;
                                        case wkbGeometryType.wkbTIN:
                                            foreach (VirgisLayer<RecordSet, Layer> l in _layers) {
                                                if (l.GetType() == typeof(TinLayer)) {
                                                    layerToAdd = l;
                                                    break;
                                                }
                                            }
                                            if (layerToAdd == null) {
                                                _layers.Add(Instantiate(TinLayer, container.transform).GetComponent<TinLayer>());
                                                _layers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                                                _layers.Last().SetFeatures(thisLayer);
                                                await _layers.Last().Init(layer);
                                            }
                                            break;
                                        default:
                                            throw new NotSupportedException($"Geometry type {ftype.ToString()} is not supported");
                                    }
                                    geom.Dispose();
                                }
                            }
                            break;
                    }
                }
            }
        }
    }
}
