using System.Threading.Tasks;
using System.Data;
using System.Collections.Generic;
using Project;
using System.Linq;

namespace Virgis {

    public abstract class DataLoaderPrototype : VirgisLoader<DataTable> {

        public async override Task _init() {
            RecordSet layer = _layer as RecordSet;
            DataLayerPrototype parent = m_parent as DataLayerPrototype;
            List<DataUnit> dataUnits = layer.DataUnits;
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
                        plloader.Unit = subLayer;
                        await pll.SubInit(layer);
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
