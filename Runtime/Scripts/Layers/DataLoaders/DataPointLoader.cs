using UnityEngine;
using System.Data;
using System.Threading.Tasks;
using Project;
using System;
using System.Collections;
using System.Collections.Generic;
using VirgisGeometry;

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
            List<Task<int>> tasks = new();
            AxisOrder ax = Unit.AxisOrder;
            if (ax == default)
                ax = AxisOrder.ENU;
            foreach (DataRow row in features.Rows) {
                double x = 0;
                double y = 0;
                double z = 0;
                try {
                    x = double.Parse(row.Field<string>(features.Columns[Unit.XRange]));
                    y = double.Parse(row.Field<string>(features.Columns[Unit.YRange]));
                    z = Unit.ZRange != null ?
                        double.Parse(row.Field<string>(features.Columns[Unit.ZRange])) :
                        0;
                } catch(Exception) {
                    throw new Exception($"DataUnit {Unit.Name} had invalid data");
                }
                string label = "";
                if (Unit.LabelRange != null && features.Columns.Contains(Unit.LabelRange)) {
                    label = row.Field<string>(features.Columns[Unit.LabelRange]);
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
            foreach (Datapoint pointFunc in pointFuncs) {
                long fid = pointFunc.GetFID<long>();
                DataRow row = features.Rows.Find(fid);
                if (row == null) {
                    row = features.NewRow();
                    row["__FID"] = fid;
                    features.Rows.Add(row);
                }
                Vector3d pos = pointFunc.gameObject.transform.position;
                row[Unit.XRange] = pos.x.ToString();
                row[Unit.YRange] = pos.y.ToString();
                if (Unit.ZRange != null)
                    row[Unit.ZRange] = pos.z.ToString();
                if (watch.ElapsedMilliseconds > 100) {
                    watch.Restart();
                    yield return null;
                }
            };
        }

        protected override object GetNextFID() {
            return "";
        }
    }
}
