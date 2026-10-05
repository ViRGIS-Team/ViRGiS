using UnityEngine;
using System.Data;
using System.Threading.Tasks;
using Project;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using VirgisGeometry;

namespace Virgis {
    public class DataPointLoader : PointLoaderPrototype<DataTable> {
        
        public override Task _init() {
            MSymbology = (DataUnit as DataUnit)?.Units.ToDictionary(x => x.Key, x => (UnitPrototype)x.Value);
            ReadSymbology();
            return Task.CompletedTask;
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
                double x;
                double y;
                double z;
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

                Vector3d pos3D = new Vector3d(x, y, z) { axisOrder = ax };
                tasks.Add(DrawFeatureAsync(
                    (Vector3)pos3D,
                    row.Field<long>("__FID"),
                    0,
                    label
                ));
            }
            await Task.WhenAll(tasks);
        }

        protected override IEnumerator Hydrate() {
            System.Diagnostics.Stopwatch watch = new();
            watch.Start();
            Datapoint[] pointFuncs = gameObject.GetComponentsInChildren<Datapoint>();
            AxisOrder ax = DataUnit.AxisOrder;
            if (ax == default)
                ax = AxisOrder.ENU;
            foreach (Datapoint pointFunc in pointFuncs) {
                if (! pointFunc.changed) continue;
                long fid = pointFunc.GetFid<long>();
                DataRow row = features.Rows.Find(fid);
                if (row == null) {
                    row = features.NewRow();
                    row["__FID"] = fid;
                    features.Rows.Add(row);
                }
                Vector3d pos = pointFunc.gameObject.transform.localPosition;
                pos.ChangeAxisOrderTo(ax);
                row[DataUnit.XRange] = pos.x.ToString(CultureInfo.InvariantCulture);
                row[DataUnit.YRange] = pos.y.ToString(CultureInfo.InvariantCulture);
                if (DataUnit.ZRange != null)
                    row[DataUnit.ZRange] = pos.z.ToString(CultureInfo.InvariantCulture);
                if (watch.ElapsedMilliseconds < 100) continue;
                yield return null;
                watch.Restart(); 
            }
        }

        protected override object GetNextFid() {
            return "";
        }
    }
}
