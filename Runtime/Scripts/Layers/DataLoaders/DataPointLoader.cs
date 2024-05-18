using UnityEngine;
using System.Data;
using System.Threading.Tasks;
using Project;
using System;

namespace Virgis {
    public class DataPointLoader : PointLoaderPrototype<DataTable> {

        public DataUnit Unit;

        public override async Task _init() {
            m_symbology = Unit.Units;
            await Load();
        }

        public override async Task _draw() {
            if (Unit.XRange == null ||
                !features.Columns.Contains(Unit.XRange) ||
                Unit.YRange == null ||
                !features.Columns.Contains(Unit.YRange) ||
                (Unit.ZRange != null && !features.Columns.Contains(Unit.ZRange))
               )
            {
                throw new Exception($"DataUnit {Unit.Name} has invalid columns");
            }
            foreach (DataRow row in features.Rows) {
                float x = 0;
                float y = 0;
                float z = 0;
                try {
                    x = float.Parse(row.Field<string>(features.Columns[Unit.XRange]));
                    y = float.Parse(row.Field<string>(features.Columns[Unit.YRange]));
                    z = Unit.ZRange != null ?
                        float.Parse(row.Field<string>(features.Columns[Unit.ZRange])) :
                        0;
                } catch(Exception) {
                    throw new Exception($"DataUnit {Unit.Name} had invalid data");
                }
                string label = "";
                if (Unit.LabelRange != null && features.Columns.Contains(Unit.LabelRange)) {
                    label = row.Field<string>(features.Columns[Unit.LabelRange]);
                }
                await DrawFeatureAsync(new Vector3(x, y, z), label);
            }
        }

        public override Task _save() {
            //Datapoint[] pointFuncs = gameObject.GetComponentsInChildren<Datapoint>();
            //List<Feature> thisFeatures = new List<Feature>();
            //long n = features.GetFeatureCount(0);
            //for (int i = 0; i < (int) n; i++) features.DeleteFeature(i);
            //foreach (Datapoint pointFunc in pointFuncs) {
            //    Feature feature = pointFunc.feature as Feature;
            //    Geometry geom = (pointFunc.gameObject.transform.position.ToGeometry());
            //    geom.TransformTo(GetCrs());
            //    feature.SetGeometryDirectly(geom);
            //    features.CreateFeature(feature);
            //}
            //features.SyncToDisk();
            return Task.CompletedTask;
        }
    }
}
