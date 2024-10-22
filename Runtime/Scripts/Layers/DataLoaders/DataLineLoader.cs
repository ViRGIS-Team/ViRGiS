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
using g3;
using System.Data;
using System;

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
                curve.AppendVertex(new Vector3d(x, y, z));
            }
            await _drawFeatureAsync(curve);
        }

        public override Task _save()
        {
            return Task.CompletedTask;
        }
    }
}
