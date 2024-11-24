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

using System;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using System.Threading.Tasks;
using Project;
using Pdal;
using OSGeo.GDAL;
using OSGeo.OSR;
using Stopwatch = System.Diagnostics.Stopwatch;
using VirgisGeometry;

namespace Virgis
{
    public class RasterLoader : PointCloudLoaderPrototype<BakedPointCloud>
    {
        private double m_PixelSize;
        private const ulong m_MaxPoints = 100000;

        public override async Task _init() {
            Stopwatch stopWatch = Stopwatch.StartNew();
            RecordSet layer = _layer as RecordSet;
            m_Symbology = layer.Units;
            if (m_Symbology.TryGetValue("point", out Unit unit)) {
                SetupColormap(unit);
            }
            parent = m_parent as RasterLayer;
            Load(layer);
            Debug.Log($"Raster Layer Load took : {stopWatch.Elapsed.TotalSeconds}");
        }


        protected Task LoadAsync(RecordSet layer) {

            Task t1 = new(() => {
                Load(layer);
            });
            t1.Start();
            return t1;
        }

        protected void Load(RecordSet layer) {
            Dataset raster = Gdal.Open(layer.Source, Access.GA_ReadOnly);
            int numBands = raster.RasterCount;
            if (numBands <= 0)
                throw new NotSupportedException($" No Data in file {layer.Source}");
            SpatialReference sr = raster.GetSpatialRef();

            // band-1 is data
            Band band1 = raster.GetRasterBand(1);

            Dataset dem = null;
            bool hasDem = false;
            SpatialReference demSR = null;
            Band elevation = null;
            double[] elevationValues = null;
            double demWidth = 0;
            double demHeight = 0;

            if (layer.Properties.Dem != null) {
                hasDem = true;
                dem = Gdal.Open(layer.Properties.Dem, Access.GA_ReadOnly);
                numBands = dem.RasterCount;
                if (numBands <= 0)
                    throw new NotSupportedException($" No Data in file {layer.Properties.Dem}");
                demSR = dem.GetSpatialRef();
                // band-1 is elevation
                elevation = dem.GetRasterBand(1);
                if (!elevation.ToArray(out elevationValues))
                    throw new NotSupportedException($" No Data in file {layer.Properties.Dem}");
                demWidth = dem.RasterXSize;
                demHeight = dem.RasterYSize;
            }

            // get the null value
            band1.GetNoDataValue(out double noDataValue, out int hasval);
            if (hasval == 0)
                noDataValue = 0;
            band1.GetMinimum(out double min, out int hasMin);
            band1.GetMaximum(out double max, out int hasMax);
            double range = max - min;

            // Get the size and pixel size of the raster
            // if the raster has more than m_MaxPoints data points, using poisson sampling to down size
            ulong datapoints = (ulong) (raster.RasterXSize * raster.RasterYSize);
            double[] geoTransform = new double[6];
            raster.GetGeoTransform(geoTransform);
            if (geoTransform == null && geoTransform[1] == 0) {
                throw new Exception("Could not acces GeoTransform");
            }

            double[] demGeoTransform = new double[6];
            double3x3 revGT = default;
            if (hasDem) {
                dem.GetGeoTransform(demGeoTransform);
                if (geoTransform == null && geoTransform[1] == 0) {
                    throw new Exception("Could not acces GeoTransform");
                }
                revGT = math.inverse(demGeoTransform.ToTransform());
            }


            ulong scaleFactor = 1;
            m_PixelSize = geoTransform[1] * scaleFactor;
            if (datapoints > m_MaxPoints) {
                scaleFactor = (datapoints / m_MaxPoints);
            };

            features = new(datapoints / scaleFactor);
            NativeArray<Color> positionMap = features.PositionMap.GetRawTextureData<Color>();
            NativeArray<Color32> colorMap = features.ColorMap.GetRawTextureData<Color32>();

            // set the stride based on the BPC width for accuracy
            int stride = scaleFactor==1? 1: (int)datapoints / (features.Width * features.Height);
            CoordinateTransformation transformer = AppState.instance.projectTransformer(sr);
            CoordinateTransformation demTransformer = null;
            CoordinateTransformation demOutTransformer = null;
            if (demSR != null) {
                demTransformer = AppState.instance.projectTransformer(demSR);
                demOutTransformer = AppState.instance.projectOutTransformer(demSR);
            }

            Unit value;
            Color color = Color.white;
            if (m_Symbology.TryGetValue("point", out value)) {
                color = value.Color;
            }

            if (band1.ToArray(out double[] data)) {
                for (int i = 0; i < features.Width; i++)
                    for (int j = 0; j < features.Height; j++) {
                        int ptr = i * features.Width + j;
                        if (ptr > positionMap.Length - 1) {
                            throw new Exception("Data Pointer out of range in Raster access");
                        }
                        int dptr = i * stride * raster.RasterXSize + j * stride;
                        if (dptr > data.Length - 1) {
                            break;
                        }
                        double m = data[dptr];
                        if ((float)m == (float)noDataValue) {
                            continue;
                        }

                        double[] position = new double[3] {
                                geoTransform[0] + j * stride * geoTransform[1] + i * stride * geoTransform[2],
                                geoTransform[3] + j * stride * geoTransform[4] + i * stride * geoTransform[5],
                                0
                            };

                        // transform point to project coords
                        transformer.TransformPoint(position);

                        if (hasDem) {
                            // transform position to dem coords
                            if (demTransformer != null) {
                                demOutTransformer.TransformPoint(position);
                            };
                            double3 flatverts = new(position[0], position[1], 1);
                            double3 xy = math.mul(revGT, flatverts);
                            if (xy.x < demWidth && xy.y < demHeight) {
                                position[2] = elevationValues[(int) (xy.y * demWidth + xy.x)];
                            }

                            // transform back to project coordinates
                            if (demTransformer != null) {
                                demTransformer.TransformPoint(position);
                            }
                        }
                        positionMap[ptr] = new Color(
                            (float) position[0],
                            (float) position[2],
                            (float) position[1],
                            1.0f
                            );
                        if (m_ColorInterp != e_ColorInterp.None) {
                            switch (m_ColorInterp) {
                                case e_ColorInterp.Interpolate:
                                    colorMap[ptr] = Grad.Evaluate((float) ((m - min) / range));
                                    break;
                                case e_ColorInterp.CategoryValue:
                                    colorMap[ptr] = value.ColorMap.GetCategoryValue((float) ((m - min) / range));
                                    break;
                            }
                        } else {
                            colorMap[ptr] = color;
                        }
                    }
            }
            features.PositionMap.Apply();
            features.ColorMap.Apply();
            band1.FlushCache();
            raster.FlushCache();
            raster.Dispose();
            elevation?.FlushCache();
            dem?.FlushCache();
            dem?.Dispose();
            return;
        }

        public override Task _draw()
        {
            Stopwatch stopWatch = Stopwatch.StartNew();
            RecordSet _layer = GetMetadata() as RecordSet;
            transform.position = _layer.Position != null ?
                (Vector3)_layer.Position.ToVector3d() : Vector3.zero ;
            if (_layer.Transform != null) transform
                    .Translate(AppState.instance.Map.transform
                    .TransformVector((Vector3)_layer.Transform.Position ));

            m_model = Instantiate(parent.pointCloud, transform, false)
                .GetComponent<PointCloud>();
            m_model.Spawn(transform);
            m_model.Bpc.Set(features.PositionMap, features.ColorMap, features.PointCount, (float) m_PixelSize * 9f);

            if (_layer.Transform != null) {
                transform.rotation = _layer.Transform.Rotate;
                transform.localScale = _layer.Transform.Scale;
            }
            Debug.Log($"Raster Layer Draw took {stopWatch.Elapsed.TotalSeconds}");
            return Task.CompletedTask;
        }

        public override void _checkpoint() { }

        public override Task _save()
        {
            _layer.Position = ((Vector3d)transform.position).ToPoint();
            _layer.Transform.Position = Vector3.zero;
            _layer.Transform.Rotate = transform.rotation;
            _layer.Transform.Scale = transform.localScale;
            return Task.CompletedTask;
        }
    }
}
