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

        public async override Task _init() {
            m_Symbology = (DataUnit as DataUnit)?.Units;
            if (m_Symbology.TryGetValue("point", out Unit unit )){
                SetupColormap(unit);
            }
            ReadSymbology();
        }
        public override Task _draw() {
            BakedPointCloud bpc = new((ulong)features.Rows.Count);
            NativeArray<Color> positions = bpc.PositionMap.GetRawTextureData<Color>();
            NativeArray<Color32> colors = bpc.ColorMap.GetRawTextureData<Color32>();
            if (DataUnit.XRange == null ||
                !features.Columns.Contains(DataUnit.XRange) ||
                DataUnit.YRange == null ||
                !features.Columns.Contains(DataUnit.YRange) ||
                (DataUnit.ZRange != null && !features.Columns.Contains(DataUnit.ZRange)) ||
                (DataUnit.LabelRange != null && !features.Columns.Contains(DataUnit.LabelRange))
               ) {
                throw new Exception($"DataUnit {DataUnit.Name} has invalid columns");
            }
            List<Task<int>> tasks = new();
            AxisOrder ax = DataUnit.AxisOrder;
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
                    x = float.Parse(row.Field<string>(features.Columns[DataUnit.XRange]));
                    y = float.Parse(row.Field<string>(features.Columns[DataUnit.YRange]));
                    z = DataUnit.ZRange != null ?
                        float.Parse(row.Field<string>(features.Columns[DataUnit.ZRange])) :
                        0;
                    m = DataUnit.LabelRange != null ?
                        float.Parse(row.Field<string>(features.Columns[DataUnit.LabelRange])) :
                        0;
                } catch (Exception) {
                    throw new Exception($"DataUnit {DataUnit.Name} had invalid data");
                }
                string label = "";
                if (DataUnit.LabelRange != null && features.Columns.Contains(DataUnit.LabelRange)) {
                    label = row.Field<string>(features.Columns[DataUnit.LabelRange]);
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
            if (DataUnit.LabelRange != null && MColorInterp != EColorInterp.None) {
                for (int i = 0; i < bpc.PointCount; i++) {
                    switch (MColorInterp) {
                        case EColorInterp.Interpolate:
                            colors[i] = grad.Evaluate((positions[i].a - min) / range);
                            break;
                        case EColorInterp.CategoryValue:
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
