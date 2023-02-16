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
using OSGeo.OGR;
using System.Linq;

namespace Virgis {
    public class OgrLoader : VirgisLoader<Layer[]> {

        // used to read the source file for this layer
        private OgrReader m_ogrReader;
        private OgrLayer parent;

        protected new void OnDestroy() {
            base.OnDestroy();
            m_ogrReader?.Dispose();
        }

        public override async Task _init() {
            //
            // Load Dataset
            //
            RecordSet layer = _layer as RecordSet;
            parent = m_parent as OgrLayer;
            m_ogrReader = new OgrReader();
            await m_ogrReader.Load(layer.Source, layer.Properties.ReadOnly ? 0 : 1,
                layer.Properties.SourceType);

            //
            // Get and process features
            //
            features = m_ogrReader.GetLayers().ToArray();
            foreach (Layer thisLayer in features) {
                wkbGeometryType type = thisLayer.GetGeomType();
                OgrReader.Flatten(ref type);
                VirgisLayer l;
                IVirgisLoader loader;
                switch (type) {
                    case wkbGeometryType.wkbPoint:
                        subLayers.Add(Instantiate(parent.PointLayer, transform).GetComponent<PointLayer>());
                        l = subLayers.Last() as PointLayer;
                        if (! l.Spawn(transform)) throw new System.Exception("reparenting failed");
                        l.sourceName = thisLayer.GetName();
                        l.isWriteable = m_ogrReader.isWriteable;
                        loader = l.gameObject.AddComponent<PointLoader>();
                        (loader as PointLoader).SetFeatures(thisLayer);
                        (loader as PointLoader).SetCrs(OgrReader.getSR(thisLayer, layer));
                        await l.SubInit(layer);
                        break;
                    case wkbGeometryType.wkbLineString:
                        subLayers.Add(Instantiate(parent.LineLayer, transform).GetComponent<LineLayer>());
                        l = subLayers.Last() as LineLayer;
                        if (!l.Spawn(transform))
                            throw new System.Exception("reparenting failed");
                        l.sourceName = thisLayer.GetName();
                        l.isWriteable = m_ogrReader.isWriteable;
                        await l.SubInit(layer);
                        loader = l.gameObject.AddComponent<LineLoader>();
                        (loader as LineLoader).SetFeatures(thisLayer);
                        (loader as LineLoader).SetCrs(OgrReader.getSR(thisLayer, layer));
                        break;
                    case wkbGeometryType.wkbPolygon:
                        subLayers.Add(Instantiate(parent.PolygonLayer, transform).GetComponent<PolygonLayer>());
                        l = subLayers.Last() as PolygonLayer;
                        if (!l.Spawn(transform))
                            throw new System.Exception("reparenting failed");
                        l.sourceName = thisLayer.GetName();
                        l.isWriteable = m_ogrReader.isWriteable;
                        loader = l.gameObject.AddComponent<PolygonLoader>();
                        (loader as PolygonLoader).SetFeatures(thisLayer);
                        (loader as PolygonLoader).SetCrs(OgrReader.getSR(thisLayer, layer));
                        await l.SubInit(layer);
                        break;
                    case wkbGeometryType.wkbTIN:
                    case wkbGeometryType.wkbPolyhedralSurface:
                        subLayers.Add(Instantiate(parent.TinLayer, transform).GetComponent<TinLayer>());
                        l = subLayers.Last() as TinLayer;
                        if (!l.Spawn(transform))
                            throw new System.Exception("reparenting failed");
                        l.sourceName = thisLayer.GetName();
                        l.isWriteable = m_ogrReader.isWriteable;
                        loader = l.gameObject.AddComponent<TinLoader>();
                        (loader as TinLoader).SetFeatures(thisLayer);
                        (loader as TinLoader).SetCrs(OgrReader.getSR(thisLayer, layer));
                        await l.SubInit(layer);
                        break;
                    //
                    // If feature type is unknown, process each feature seperately
                    //
                    case wkbGeometryType.wkbUnknown:
                        RecordSet metadata = GetMetadata() as RecordSet;
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
                            VirgisLayer layerToAdd = null;
                            switch (ftype) {
                                case wkbGeometryType.wkbLineString:
                                    foreach (VirgisLayer lay in subLayers) {
                                        if (lay.GetType() == typeof(LineLayer)) {
                                            layerToAdd = lay;
                                            break;
                                        }
                                    }
                                    if (layerToAdd == null) {
                                        subLayers.Add(Instantiate(parent.LineLayer, transform).GetComponent<LineLayer>());
                                        l = subLayers.Last() as LineLayer;
                                        if (!l.Spawn(transform))
                                            throw new System.Exception("reparenting failed");
                                        l.isWriteable = m_ogrReader.isWriteable;
                                        loader = l.gameObject.AddComponent<LineLoader>();
                                        (loader as LineLoader).SetFeatures(thisLayer);
                                        (loader as LineLoader).SetCrs(OgrReader.getSR(thisLayer, layer));
                                        await l.SubInit(layer);
                                    }
                                    break;
                                case wkbGeometryType.wkbPolygon:
                                    foreach (VirgisLayer lay in subLayers) {
                                        if (lay.GetType() == typeof(PolygonLayer)) {
                                            layerToAdd = lay;
                                            break;
                                        }
                                    }
                                    if (layerToAdd == null) {
                                        subLayers.Add(Instantiate(parent.PolygonLayer, transform).GetComponent<PolygonLayer>());
                                        l = subLayers.Last() as PolygonLayer;
                                        if (!l.Spawn(transform))
                                            throw new System.Exception("reparenting failed");
                                        l.isWriteable = m_ogrReader.isWriteable;
                                        loader = l.gameObject.AddComponent<PolygonLoader>();
                                        (loader as PolygonLoader).SetFeatures(thisLayer);
                                        (loader as PolygonLoader).SetCrs(OgrReader.getSR(thisLayer, layer));
                                        await l.SubInit(layer);
                                    }
                                    break;
                                case wkbGeometryType.wkbPoint:
                                    foreach (VirgisLayer lay in subLayers) {
                                        if (lay.GetType() == typeof(PointLayer)) {
                                            layerToAdd = lay;
                                            break;
                                        }
                                    }
                                    if (layerToAdd == null) {
                                        subLayers.Add(Instantiate(parent.PointLayer, transform).GetComponent<PointLayer>());
                                        l = subLayers.Last() as PointLayer;
                                        if (!l.Spawn(transform))
                                            throw new System.Exception("reparenting failed");
                                        l.isWriteable = m_ogrReader.isWriteable;
                                        loader = l.gameObject.AddComponent<PointLoader>();
                                        (loader as PointLoader).SetFeatures(thisLayer);
                                        (loader as PointLoader).SetCrs(OgrReader.getSR(thisLayer, layer));
                                        await l.SubInit(layer);
                                    }
                                    break;
                                case wkbGeometryType.wkbTIN:
                                case wkbGeometryType.wkbPolyhedralSurface:
                                    foreach (VirgisLayer lay in subLayers) {
                                        if (lay.GetType() == typeof(TinLayer)) {
                                            layerToAdd = lay;
                                            break;
                                        }
                                    }
                                    if (layerToAdd == null) {
                                        subLayers.Add(Instantiate(parent.TinLayer, transform).GetComponent<TinLayer>());
                                        l = subLayers.Last() as TinLayer;
                                        if (!l.Spawn(transform))
                                            throw new System.Exception("reparenting failed");
                                        l.isWriteable = m_ogrReader.isWriteable;
                                        loader = l.gameObject.AddComponent<TinLoader>();
                                        (loader as TinLoader).SetFeatures(thisLayer);
                                        (loader as TinLoader).SetCrs(OgrReader.getSR(thisLayer, layer));
                                        await l.SubInit(layer);
                                    }
                                    geom.Dispose();
                                    break;
                            }
                        }
                        return;
                }
            }
        }

        public override Task _draw() {
            return Task.CompletedTask;
        }

        public override Task _save() {
            return Task.CompletedTask;
        }
    }
}
