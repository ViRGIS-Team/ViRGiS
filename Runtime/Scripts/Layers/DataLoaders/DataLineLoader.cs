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
using UnityEngine;

namespace Virgis
{

    /// <summary>
    /// The parent entity for a instance of a Line Layer - that holds one MultiLineString FeatureCollection
    /// </summary>
    public class DataLineLoader : LineLoaderPrototype<DataTable>
    {
        public DataUnit Unit;

        public override async Task _init() {
            parent = m_parent as LineLayer;
            m_symbology = Unit.Units;
            await Load();
        }

        public override async Task _draw() {
            if (Unit.XRange == null ||
                !features.Columns.Contains(Unit.XRange) ||
                Unit.YRange == null ||
                !features.Columns.Contains(Unit.YRange) ||
                (Unit.ZRange != null && !features.Columns.Contains(Unit.ZRange))
               ) {
                throw new Exception($"DataUnit {Unit.Name} has invalid columns");
            }
            DCurve3 curve = new();
            curve.Closed = false;
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
                } catch (Exception) {
                    throw new Exception($"DataUnit {Unit.Name} had invalid data");
                }

                Vector3d pos3d = new Vector3d(x, y, z) {axisOrder = ax };
                curve.AppendVertex( pos3d);
                curve.SetData(row.Field<long>("__FID"));
            }
            await _drawFeatureAsync(curve, "data");
        }

        protected override object GetNextFID() {
            return "";
        }

        protected override IEnumerator hydrate() {
            System.Diagnostics.Stopwatch watch = new();
            watch.Start();
            Dataline[] lineFuncs = gameObject.GetComponentsInChildren<Dataline>();
            foreach (Dataline lineFunc in lineFuncs) {
                IEnumerator<long> fids = lineFunc.Curve.GetDataItr<long>().GetEnumerator();
                long fid = 0;
                foreach (Vector3d v in lineFunc.Curve.Vertices) {
                    if (fids.MoveNext()) {
                        fid = fids.Current;
                    } else {
                        throw new Exception("DataLineLoader - Invalid FIDs in Curve");
                    }
                    DataRow row = features.Rows.Find(fid);
                    if (row == null) {
                        row = features.NewRow();
                        row["__FID"] = fid;
                        features.Rows.Add(row);
                    }
                    row[Unit.XRange] = v.x.ToString();
                    row[Unit.YRange] = v.y.ToString();
                    if (Unit.ZRange != null)
                        row[Unit.ZRange] = v.z.ToString();
                    if (watch.ElapsedMilliseconds > 100) {
                        watch.Restart();
                        yield return null;
                    };
                };
            };
        }
    }
}
