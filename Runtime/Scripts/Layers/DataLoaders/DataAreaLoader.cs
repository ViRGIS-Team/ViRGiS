/* MIT License

Copyright (c) 2020 - 23 Runette Software

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
using VirgisGeometry;
using System.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace Virgis
{

    /// <summary>
    /// The parent entity for an instance of a Line Layer - that holds one MultiLineString FeatureCollection
    /// </summary>
    public class DataAreaLoader : PolygonLoaderPrototype<DataTable>
    {
        
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
               ) {
                throw new Exception($"DataUnit {DataUnit.Name} has invalid columns");
            }
            DCurve3 curve = new() { Closed = false };
            AxisOrder ax = DataUnit.AxisOrder;
            if (ax == default)
                ax = AxisOrder.ENU;
            curve.axisOrder = ax;
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
                } catch (Exception) {
                    throw new Exception($"DataUnit {DataUnit.Name} had invalid data");
                }
                Vector3d pos3D = new Vector3d(x, y, z) { axisOrder = ax};
                curve.AppendVertex(pos3D);
                curve.SetData(row.Field<long>("__FID"));
            }
            DCurve3 ring = new() { axisOrder = ax };

            for (int i=0; i<curve.VertexCount; i++) {
                Vector3d v = curve.GetVertex(i);
                long fid = curve.GetData<long>(i);
                // Insert top vertex for this data point
                ring.InsertVertex(v, i);
                ring.InsertData(fid, i);

                // Insert bottom vertex for this data point
                ring.InsertVertex(
                    ax == AxisOrder.EUN
                        ? new Vector3d(v.x, 0, v.z) { axisOrder = v.axisOrder }
                        : new Vector3d(v.x, v.y, 0) { axisOrder = v.axisOrder }, i + 1);
                ring.InsertData((long)-1, i + 1);
            }
            await _drawFeatureAsync(new List<DCurve3>() { ring }, "data", 0);
        }

        protected override object GetNextFid() {
            return "";
        }

        protected override IEnumerator Hydrate() {
            System.Diagnostics.Stopwatch watch = new();
            AxisOrder ax = DataUnit.AxisOrder;
            if (ax == default)
                ax = AxisOrder.ENU;
            watch.Start();
            Dataline[] lineFuncs = gameObject.GetComponentsInChildren<Dataline>();
            foreach (Dataline lineFunc in lineFuncs) {
                if (!lineFunc.changed) continue;
                using IEnumerator<long> fids = lineFunc.Curve.GetDataItr<long>().GetEnumerator();
                foreach (Vector3d v in lineFunc.Curve.Vertices) {
                    long fid;
                    if (fids.MoveNext()) {
                        fid = fids.Current;
                    } else {
                        continue;
                    }
                    if (fid == -1) continue;
                    DataRow row = features.Rows.Find(fid);
                    if (row == null) {
                        row = features.NewRow();
                        row["__FID"] = fid;
                        features.Rows.Add(row);
                    }
                    v.ChangeAxisOrderTo(ax);
                    row[DataUnit.XRange] = v.x.ToString(CultureInfo.InvariantCulture);
                    row[DataUnit.YRange] = v.y.ToString(CultureInfo.InvariantCulture);
                    if (DataUnit.ZRange != null)
                        row[DataUnit.ZRange] = v.z.ToString(CultureInfo.InvariantCulture);
                    if (watch.ElapsedMilliseconds < 100) continue;
                    yield return null;
                    watch.Restart();
                }
            }
        }
    }
}
