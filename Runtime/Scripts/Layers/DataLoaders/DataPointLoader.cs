using UnityEngine;
using System.Data;
using System.Threading.Tasks;
using Project;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using VirgisGeometry;

namespace Virgis {
    public class DataPointLoader : PointLoaderPrototype<DataTable> {
        
        public override async Task _init() {
            MSymbology = (DataUnit as DataUnit)?.Units.ToDictionary(x => x.Key, x => (UnitPrototype)x.Value);
            ReadSymbology();
        }

        public override async Task _draw() {
            if (DataUnit.XRange == null ||
                !features.Columns.Contains(DataUnit.XRange) ||
                DataUnit.YRange == null ||
                !features.Columns.Contains(DataUnit.YRange) ||
                (DataUnit.ZRange != null && !features.Columns.Contains(DataUnit.ZRange))
               )
            {
                throw new Exception($"DataUnit {DataUnit.Name} has invalid columns");
            }
            List<Task<int>> tasks = new();
            AxisOrder ax = DataUnit.AxisOrder;
            if (ax == default)
                ax = AxisOrder.ENU;
            foreach (DataRow row in features.Rows) {
                double x = 0;
                double y = 0;
                double z = 0;
                try {
                    x = double.Parse(row.Field<string>(features.Columns[DataUnit.XRange]));
                    y = double.Parse(row.Field<string>(features.Columns[DataUnit.YRange]));
                    z = DataUnit.ZRange != null ?
                        double.Parse(row.Field<string>(features.Columns[DataUnit.ZRange])) :
                        0;
                } catch(Exception) {
                    throw new Exception($"DataUnit {DataUnit.Name} had invalid data");
                }
                string label = "";
                if (DataUnit.LabelRange != null && features.Columns.Contains(DataUnit.LabelRange)) {
                    label = row.Field<string>(features.Columns[DataUnit.LabelRange]);
                }

                Vector3d pos3d = new Vector3d(x, y, z) { axisOrder = ax };
                tasks.Add(DrawFeatureAsync(
                    (Vector3)pos3d,
                    row.Field<long>("__FID"),
                    label
                ));
            }
            await Task.WhenAll(tasks);
        }

        protected override IEnumerator hydrate() {
            System.Diagnostics.Stopwatch watch = new();
            watch.Start();
            Datapoint[] pointFuncs = gameObject.GetComponentsInChildren<Datapoint>();
            AxisOrder ax = DataUnit.AxisOrder;
            if (ax == default)
                ax = AxisOrder.ENU;
            foreach (Datapoint pointFunc in pointFuncs) {
                if (! pointFunc.changed) continue;
                long fid = pointFunc.GetFID<long>();
                DataRow row = features.Rows.Find(fid);
                if (row == null) {
                    row = features.NewRow();
                    row["__FID"] = fid;
                    features.Rows.Add(row);
                }
                Vector3d pos = pointFunc.gameObject.transform.localPosition;
                pos.ChangeAxisOrderTo(ax);
                row[DataUnit.XRange] = pos.x.ToString();
                row[DataUnit.YRange] = pos.y.ToString();
                if (DataUnit.ZRange != null)
                    row[DataUnit.ZRange] = pos.z.ToString();
                if (watch.ElapsedMilliseconds < 100) continue;
                yield return null;
                watch.Restart(); 
            }
        }

        protected override object GetNextFID() {
            return "";
        }
    }
}
