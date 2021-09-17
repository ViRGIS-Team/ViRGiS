/* MIT License

Copyright (c) 2020 - 21 Runette Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (and subsidiary notices) shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */


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
        private OgrReader m_ogrReader;

        private void OnDestroy() {
            m_ogrReader?.Dispose();
        }

        protected override async Task _init() {
            //
            // Load Dataset
            //
            RecordSet layer = _layer as RecordSet;
            m_ogrReader = new OgrReader();
            await m_ogrReader.Load(layer.Source, layer.Properties.ReadOnly ? 0 : 1, layer.Properties.SourceType);

            //
            // Get and process features
            //
            features = m_ogrReader.GetLayers().ToArray();
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
                        subLayers.Last().isWriteable = m_ogrReader.isWriteable;
                        await subLayers.Last().SubInit(layer);
                        break;
                    case wkbGeometryType.wkbLineString:
                        subLayers.Add(Instantiate(LineLayer, transform).GetComponent<LineLayer>());
                        (subLayers.Last() as VirgisLayer<RecordSet, Layer>).SetFeatures(thisLayer);
                        subLayers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                        subLayers.Last().SetMetadata(layer);
                        subLayers.Last().sourceName = thisLayer.GetName();
                        subLayers.Last().isWriteable = m_ogrReader.isWriteable;
                        await subLayers.Last().SubInit(layer);
                        break;
                    case wkbGeometryType.wkbPolygon:
                        subLayers.Add(Instantiate(PolygonLayer, transform).GetComponent<PolygonLayer>());
                        (subLayers.Last() as VirgisLayer<RecordSet, Layer>).SetFeatures(thisLayer);
                        subLayers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                        subLayers.Last().SetMetadata(layer);
                        subLayers.Last().sourceName = thisLayer.GetName();
                        subLayers.Last().isWriteable = m_ogrReader.isWriteable;
                        await subLayers.Last().SubInit(layer);
                        break;
                    case wkbGeometryType.wkbTIN:
                        subLayers.Add(Instantiate(TinLayer, transform).GetComponent<TinLayer>());
                        (subLayers.Last() as VirgisLayer<RecordSet, Layer>).SetFeatures(thisLayer);
                        subLayers.Last().SetCrs(OgrReader.getSR(thisLayer, layer));
                        subLayers.Last().SetMetadata(layer);
                        subLayers.Last().sourceName = thisLayer.GetName();
                        subLayers.Last().isWriteable = m_ogrReader.isWriteable;
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
                        await m_ogrReader.GetFeaturesAsync(thisLayer);
                        foreach (Feature feature in m_ogrReader.features) {
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
                                        subLayers.Last().isWriteable = m_ogrReader.isWriteable;
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
                                        subLayers.Last().isWriteable = m_ogrReader.isWriteable;
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
                                        subLayers.Last().isWriteable = m_ogrReader.isWriteable;
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
                                        subLayers.Last().isWriteable = m_ogrReader.isWriteable;
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
