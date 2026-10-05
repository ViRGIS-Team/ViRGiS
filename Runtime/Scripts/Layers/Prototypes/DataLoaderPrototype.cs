using System.Threading.Tasks;
using System.Data;
using System.Collections.Generic;
using Project;
using System.Linq;

namespace Virgis {

    public abstract class DataLoaderPrototype : VirgisLoader<DataTable> {

        public async override Task _init() {
            RecordSet layer = Layer as RecordSet;
            DataLayerPrototype parent = MParent as DataLayerPrototype;
            List<DataUnit> dataUnits = layer.DataUnits;

            // set up sub layers
            foreach (DataUnit subLayer in dataUnits) {
                switch (subLayer.Representation) {
                    case DataUnitRepresent.Points:
                        MParent.AddSubLayer(Instantiate(parent.PointLayer, transform).GetComponent<PointLayer>());
                        PointLayer pl = SubLayers.Last() as PointLayer;
                        if (!pl.Spawn(transform))
                            throw new System.Exception("reparenting failed");
                        pl.transform.position = subLayer.Transform.Position;
                        pl.transform.rotation = subLayer.Transform.Rotate;
                        pl.transform.localScale = subLayer.Transform.Scale;
                        pl.SourceName = subLayer.Name;
                        pl.IsWriteable = true;
                        DataPointLoader ploader = pl.gameObject.AddComponent<DataPointLoader>();
                        ploader.SetFeatures(features);
                        ploader.DataUnit = subLayer;
                        await pl.SubInit(layer);
                        break;
                    case DataUnitRepresent.Line:
                        MParent.AddSubLayer(Instantiate(parent.LineLayer, transform).GetComponent<LineLayer>());
                        LineLayer ll = SubLayers.Last() as LineLayer;
                        if (!ll.Spawn(transform))
                            throw new System.Exception("reparenting failed");
                        ll.transform.position = subLayer.Transform.Position;
                        ll.transform.rotation = subLayer.Transform.Rotate;
                        ll.transform.localScale = subLayer.Transform.Scale;
                        ll.SourceName = subLayer.Name;
                        ll.IsWriteable = true;
                        DataLineLoader loader = ll.gameObject.AddComponent<DataLineLoader>();
                        loader.SetFeatures(features);
                        loader.DataUnit = subLayer;
                        await ll.SubInit(layer);
                        break;
                    case DataUnitRepresent.Area:
                        MParent.AddSubLayer(Instantiate(parent.AreaLayer, transform).GetComponent<PolygonLayer>());
                        PolygonLayer pll = SubLayers.Last() as PolygonLayer;
                        if (!pll.Spawn(transform))
                            throw new System.Exception("reparenting failed");
                        pll.transform.position = subLayer.Transform.Position;
                        pll.transform.rotation = subLayer.Transform.Rotate;
                        pll.transform.localScale = subLayer.Transform.Scale;
                        pll.SourceName = subLayer.Name;
                        pll.IsWriteable = true;
                        DataAreaLoader plloader = pll.gameObject.AddComponent<DataAreaLoader>();
                        plloader.SetFeatures(features);
                        plloader.DataUnit = subLayer;
                        await pll.SubInit(layer);
                        break;
                    case DataUnitRepresent.Manifold:
                        MParent.AddSubLayer(Instantiate(parent.ManifoldLayer, transform).GetComponent<MeshLayer>());
                        MeshLayer ml = SubLayers.Last() as MeshLayer;
                        if (!ml.Spawn(transform))
                            throw new System.Exception("reparenting failed");
                        ml.transform.position = subLayer.Transform.Position;
                        ml.transform.rotation = subLayer.Transform.Rotate;
                        ml.transform.localScale = subLayer.Transform.Scale;
                        ml.SourceName = subLayer.Name;
                        ml.IsWriteable = true;
                        DataManifoldLoader mloader = ml.gameObject.AddComponent<DataManifoldLoader>();
                        mloader.SetFeatures(features);
                        mloader.DataUnit = subLayer;
                        await ml.SubInit(layer);
                        break;
                    case DataUnitRepresent.PointCloud:
                        MParent.AddSubLayer(Instantiate(parent.PointCloudLayer, transform).GetComponent<PointCloudLayer>());
                        PointCloudLayer pc = SubLayers.Last() as PointCloudLayer;
                        if (!pc.Spawn(transform))
                            throw new System.Exception("reparenting failed");
                        pc.transform.position = subLayer.Transform.Position;
                        pc.transform.rotation = subLayer.Transform.Rotate;
                        pc.transform.localScale = subLayer.Transform.Scale;
                        pc.SourceName = subLayer.Name;
                        pc.IsWriteable = true;
                        DataPointCloudLoader pcloader = pc.gameObject.AddComponent<DataPointCloudLoader>();
                        pcloader.SetFeatures(features);
                        pcloader.DataUnit = subLayer;
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
