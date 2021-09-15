// copyright Runette Software Ltd, 2020. All rights reserved
using Project;
using System.Threading.Tasks;
using UnityEngine;
using OSGeo.OGR;
using System.Linq;
using System;

namespace Virgis {
    public class OgrLayer : ContainerLayer<RecordSet, Layer[]> {
        // The prefab for the data points to be instantiated
        public GameObject PointLayer;
        public GameObject LineLayer;
        public GameObject PolygonLayer;
        public GameObject TinLayer;
        public GameObject ContainerLayer;

        // used to read the source file for this layer
        private OgrReader ogrReader;

        private void OnDestroy() {
            ogrReader?.Dispose();
        }

        protected override async Task _init() {
            //
            // Load Dataset
            //
            RecordSet layer = _layer as RecordSet;
            ogrReader = new OgrReader();
            await ogrReader.Load(layer.Source, layer.Properties.ReadOnly ? 0 : 1, layer.Properties.SourceType);

            //
            // Get and process features
            //
            features = ogrReader.GetLayers().ToArray();
            foreach (Layer thisLayer in features) {
                wkbGeometryType type = thisLayer.GetGeomType();
                OgrReader.Flatten(ref type);
                switch (type) {
                    case wkbGeometryType.wkbPoint:
                        subLayers.Add(Instantiate(PointLayer, transform).GetComponent<PointLayer>());
                        (subLayers.Last() as VirgisLayer<RecordSet, Layer>).SetFeatures(thisLayer);
                        subLayers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                        subLayers.Last().SetMetadata(layer);
                        subLayers.Last().sourceName = thisLayer.GetName();
                        await subLayers.Last().SubInit(layer);
                        break;
                    case wkbGeometryType.wkbLineString:
                        subLayers.Add(Instantiate(LineLayer, transform).GetComponent<LineLayer>());
                        (subLayers.Last() as VirgisLayer<RecordSet, Layer>).SetFeatures(thisLayer);
                        subLayers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                        subLayers.Last().SetMetadata(layer);
                        subLayers.Last().sourceName = thisLayer.GetName();
                        await subLayers.Last().SubInit(layer);
                        break;
                    case wkbGeometryType.wkbPolygon:
                        subLayers.Add(Instantiate(PolygonLayer, transform).GetComponent<PolygonLayer>());
                        (subLayers.Last() as VirgisLayer<RecordSet, Layer>).SetFeatures(thisLayer);
                        subLayers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                        subLayers.Last().SetMetadata(layer);
                        subLayers.Last().sourceName = thisLayer.GetName();
                        await subLayers.Last().SubInit(layer);
                        break;
                    case wkbGeometryType.wkbTIN:
                        subLayers.Add(Instantiate(TinLayer, transform).GetComponent<TinLayer>());
                        (subLayers.Last() as VirgisLayer<RecordSet, Layer>).SetFeatures(thisLayer);
                        subLayers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                        subLayers.Last().SetMetadata(layer);
                        subLayers.Last().sourceName = thisLayer.GetName();
                        await subLayers.Last().SubInit(layer);
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
                            Geometry geom = feature.GetGeometryRef();
                            if (geom == null)
                                continue;
                            wkbGeometryType ftype = geom.GetGeometryType();
                            OgrReader.Flatten(ref ftype);
                            VirgisLayer<RecordSet, Layer> layerToAdd = null;
                            switch (ftype) {
                                case wkbGeometryType.wkbLineString:
                                    foreach (VirgisLayer<RecordSet, Layer> l in subLayers) {
                                        if (l.GetType() == typeof(LineLayer)) {
                                            layerToAdd = l;
                                            break;
                                        }
                                    }
                                    if (layerToAdd == null) {
                                        subLayers.Add(Instantiate(LineLayer, transform).GetComponent<LineLayer>());
                                        subLayers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                                        (subLayers.Last() as VirgisLayer<RecordSet, Layer>).SetFeatures(thisLayer);
                                        subLayers.Last().SetMetadata(layer);
                                        await subLayers.Last().SubInit(layer);
                                    }
                                    break;
                                case wkbGeometryType.wkbPolygon:
                                    foreach (VirgisLayer<RecordSet, Layer> l in subLayers) {
                                        if (l.GetType() == typeof(PolygonLayer)) {
                                            layerToAdd = l;
                                            break;
                                        }
                                    }
                                    if (layerToAdd == null) {
                                        subLayers.Add(Instantiate(PolygonLayer, transform).GetComponent<PolygonLayer>());
                                        subLayers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                                        (subLayers.Last() as VirgisLayer<RecordSet, Layer>).SetFeatures(thisLayer);
                                        subLayers.Last().SetMetadata(layer);
                                        await subLayers.Last().SubInit(layer);
                                    }
                                    break;
                                case wkbGeometryType.wkbPoint:
                                    foreach (VirgisLayer<RecordSet, Layer> l in subLayers) {
                                        if (l.GetType() == typeof(PointLayer)) {
                                            layerToAdd = l;
                                            break;
                                        }
                                    }
                                    if (layerToAdd == null) {
                                        subLayers.Add(Instantiate(PointLayer, transform).GetComponent<PointLayer>());
                                        subLayers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                                        (subLayers.Last() as VirgisLayer<RecordSet, Layer>).SetFeatures(thisLayer);
                                        subLayers.Last().SetMetadata(layer);
                                        await subLayers.Last().SubInit(layer);
                                    }
                                    break;
                                case wkbGeometryType.wkbTIN:
                                    foreach (VirgisLayer<RecordSet, Layer> l in subLayers) {
                                        if (l.GetType() == typeof(TinLayer)) {
                                            layerToAdd = l;
                                            break;
                                        }
                                    }
                                    if (layerToAdd == null) {
                                        subLayers.Add(Instantiate(TinLayer, transform).GetComponent<TinLayer>());
                                        subLayers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                                        (subLayers.Last() as VirgisLayer<RecordSet, Layer>).SetFeatures(thisLayer);
                                        subLayers.Last().SetMetadata(layer);
                                        await subLayers.Last().SubInit(layer);
                                    }
                                    geom.Dispose();
                                    break;
                            }
                        }
                        return;
                }
            }
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry) {
            throw new NotImplementedException();
        }



        protected override Task _draw() {
            return Task.CompletedTask;
        }
    }
}
