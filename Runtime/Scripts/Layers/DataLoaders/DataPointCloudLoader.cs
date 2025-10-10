using System.Data;
using UnityEngine;
using Unity.Collections;
using Project;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using VirgisGeometry;
using Pdal;
using System.Linq;

namespace Virgis {

    public class DataPointCloudLoader : PointCloudLoaderPrototype<DataTable> {

        public DataUnit Unit;

        public async override Task _init() {
            m_Symbology = Unit.Units;
            if (m_Symbology.TryGetValue("point", out Unit unit )){
                SetupColormap(unit);
            }
            await Load();
        }
        public override Task _draw() {
            BakedPointCloud bpc = new((ulong)features.Rows.Count);
            NativeArray<Color> positions = bpc.PositionMap.GetRawTextureData<Color>();
            NativeArray<Color32> colors = bpc.ColorMap.GetRawTextureData<Color32>();
            if (Unit.XRange == null ||
                !features.Columns.Contains(Unit.XRange) ||
                Unit.YRange == null ||
                !features.Columns.Contains(Unit.YRange) ||
                (Unit.ZRange != null && !features.Columns.Contains(Unit.ZRange)) ||
                (Unit.LabelRange != null && !features.Columns.Contains(Unit.LabelRange))
               ) {
                throw new Exception($"DataUnit {Unit.Name} has invalid columns");
            }
            List<Task<int>> tasks = new();
            AxisOrder ax = Unit.AxisOrder;
            if (ax == default)
                ax = AxisOrder.ENU;

            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i =0; i < features.Rows.Count; i++) {
                DataRow row = features.Rows[i];
                float x = 0;
                float y = 0;
                float z = 0;
                float m = 0;
                try {
                    x = float.Parse(row.Field<string>(features.Columns[Unit.XRange]));
                    y = float.Parse(row.Field<string>(features.Columns[Unit.YRange]));
                    z = Unit.ZRange != null ?
                        float.Parse(row.Field<string>(features.Columns[Unit.ZRange])) :
                        0;
                    m = Unit.LabelRange != null ?
                        float.Parse(row.Field<string>(features.Columns[Unit.LabelRange])) :
                        0;
                } catch (Exception) {
                    throw new Exception($"DataUnit {Unit.Name} had invalid data");
                }
                string label = "";
                if (Unit.LabelRange != null && features.Columns.Contains(Unit.LabelRange)) {
                    label = row.Field<string>(features.Columns[Unit.LabelRange]);
                }
                if (ax == AxisOrder.EUN) {
                    positions[i] = new(x, y, z, m);
                } else {
                    positions[i] = new(x ,z, y, m);
                }
                if (m < min)
                    min = m;
                if (m > max)
                    max = m;
            }
            float range = max - min;
            float size = 1.0f;
            Unit value;
            Color color = Color.white;
            if (m_Symbology.TryGetValue("point", out value)) {
                size = value.Transform.Scale.magnitude;
                color = value.Color;
            } 
            if (Unit.LabelRange != null && m_ColorInterp != e_ColorInterp.None) {
                for (int i = 0; i < bpc.PointCount; i++) {
                    switch (m_ColorInterp) {
                        case e_ColorInterp.Interpolate:
                            colors[i] = Grad.Evaluate((positions[i].a - min) / range);
                            break;
                        case e_ColorInterp.CategoryValue:
                            colors[i] = value.ColorMap.GetCategoryValue((positions[i].a - min) / range); 
                            break;
                    }
                }
            } else {
                for (int i = 0; i < bpc.PointCount; i++)
                    colors[i] = (Color)value.Color;
            }
            bpc.PositionMap.Apply(false, false);
            bpc.ColorMap.Apply(false, false);
            RecordSet layer = GetMetadata() as RecordSet;
            transform.position = layer.Position != null ?
                (Vector3) layer.Position.ToVector3d() : Vector3.zero;
            if (layer.Transform != null)
                transform.
                    Translate(AppState.instance.Map.transform.
                    TransformVector((Vector3) layer.Transform.Position));

            m_model = Instantiate(parent.pointCloud, transform, false)
                .GetComponent<PointCloud>();
            m_model.Spawn(parent.transform);
            m_model.Symbology = m_Symbology.ToDictionary(
                item => item.Key,
                item => item.Value as UnitPrototype
            );
            
            m_model.Bpc.Set(bpc.PositionMap, bpc.ColorMap, bpc.PointCount, size);
            return Task.CompletedTask;
        }
    }
}
