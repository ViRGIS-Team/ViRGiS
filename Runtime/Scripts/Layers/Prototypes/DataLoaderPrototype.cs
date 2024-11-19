using System.Threading.Tasks;
using System.Data;
using System.Collections.Generic;
using Project;
using System.Linq;
using CsvHelper.Configuration;
using UnityEngine;

namespace Virgis {

    public abstract class DataLoaderPrototype : VirgisLoader<DataTable> {

        public async override Task _init() {
            RecordSet layer = _layer as RecordSet;
            DataLayerPrototype parent = m_parent as DataLayerPrototype;
            List<DataUnit> dataUnits = layer.DataUnits;

            //set up color gradient
            Gradient grad = new();
            GradientColorKey[] colors = new GradientColorKey[3];
            colors[0] = new(Color.red, 0);
            colors[1] = new(Color.green, 0.5f);
            colors[2] = new(Color.blue, 1.0f);
            GradientAlphaKey[] alphas = new GradientAlphaKey[2];
            alphas[0] = new(1, 0);
            alphas[1] = new(1, 1);
            grad.SetKeys(colors, alphas);
            grad.mode = GradientMode.PerceptualBlend;

            // set up sub layers
            foreach (DataUnit subLayer in dataUnits) {
                switch (subLayer.Representation) {
                    case DataUnitRepresent.Points:
                        m_parent.AddSubLayer(Instantiate(parent.PointLayer, transform).GetComponent<PointLayer>());
                        PointLayer pl = subLayers.Last() as PointLayer;
                        if (!pl.Spawn(transform))
                            throw new System.Exception("reparenting failed");
                        pl.transform.position = subLayer.Transform.Position;
                        pl.transform.rotation = subLayer.Transform.Rotate;
                        pl.transform.localScale = subLayer.Transform.Scale;
                        pl.sourceName = subLayer.Name;
                        pl.IsWriteable = true;
                        DataPointLoader ploader = pl.gameObject.AddComponent<DataPointLoader>();
                        ploader.SetFeatures(features);
                        ploader.Grad = grad;
                        ploader.Unit = subLayer;
                        await pl.SubInit(layer);
                        break;
                    case DataUnitRepresent.Line:
                        m_parent.AddSubLayer(Instantiate(parent.LineLayer, transform).GetComponent<LineLayer>());
                        LineLayer ll = subLayers.Last() as LineLayer;
                        if (!ll.Spawn(transform))
                            throw new System.Exception("reparenting failed");
                        ll.transform.position = subLayer.Transform.Position;
                        ll.transform.rotation = subLayer.Transform.Rotate;
                        ll.transform.localScale = subLayer.Transform.Scale;
                        ll.sourceName = subLayer.Name;
                        ll.IsWriteable = true;
                        DataLineLoader loader = ll.gameObject.AddComponent<DataLineLoader>();
                        loader.SetFeatures(features);
                        loader.Grad = grad;
                        loader.Unit = subLayer;
                        await ll.SubInit(layer);
                        break;
                    case DataUnitRepresent.Area:
                        m_parent.AddSubLayer(Instantiate(parent.AreaLayer, transform).GetComponent<PolygonLayer>());
                        PolygonLayer pll = subLayers.Last() as PolygonLayer;
                        if (!pll.Spawn(transform))
                            throw new System.Exception("reparenting failed");
                        pll.transform.position = subLayer.Transform.Position;
                        pll.transform.rotation = subLayer.Transform.Rotate;
                        pll.transform.localScale = subLayer.Transform.Scale;
                        pll.sourceName = subLayer.Name;
                        pll.IsWriteable = true;
                        DataAreaLoader plloader = pll.gameObject.AddComponent<DataAreaLoader>();
                        plloader.SetFeatures(features);
                        plloader.Grad = grad;
                        plloader.Unit = subLayer;
                        await pll.SubInit(layer);
                        break;
                    case DataUnitRepresent.Manifold:
                        m_parent.AddSubLayer(Instantiate(parent.ManifoldLayer, transform).GetComponent<MeshLayer>());
                        MeshLayer ml = subLayers.Last() as MeshLayer;
                        if (!ml.Spawn(transform))
                            throw new System.Exception("reparenting failed");
                        ml.transform.position = subLayer.Transform.Position;
                        ml.transform.rotation = subLayer.Transform.Rotate;
                        ml.transform.localScale = subLayer.Transform.Scale;
                        ml.sourceName = subLayer.Name;
                        ml.IsWriteable = true;
                        DataManifoldLoader mloader = ml.gameObject.AddComponent<DataManifoldLoader>();
                        mloader.SetFeatures(features);
                        mloader.Grad = grad;
                        mloader.Unit = subLayer;
                        await ml.SubInit(layer);
                        break;
                    case DataUnitRepresent.PointCloud:
                        m_parent.AddSubLayer(Instantiate(parent.PointCloudLayer, transform).GetComponent<PointCloudLayer>());
                        PointCloudLayer pc = subLayers.Last() as PointCloudLayer;
                        if (!pc.Spawn(transform))
                            throw new System.Exception("reparenting failed");
                        pc.transform.position = subLayer.Transform.Position;
                        pc.transform.rotation = subLayer.Transform.Rotate;
                        pc.transform.localScale = subLayer.Transform.Scale;
                        pc.sourceName = subLayer.Name;
                        pc.IsWriteable = true;
                        DataPointCloudLoader pcloader = pc.gameObject.AddComponent<DataPointCloudLoader>();
                        pcloader.SetFeatures(features);
                        pcloader.Grad = grad;
                        pcloader.Unit = subLayer;
                        await pc.SubInit(layer);
                        break;
                }
            }
            return;
        }

        public override Task _draw() {
            return Task.CompletedTask;
        }
    }
}
