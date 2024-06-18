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

using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Project;
using Pdal;
using OSGeo.GDAL;
using Mdal;
using VirgisGeometry;
using System;
using Stopwatch = System.Diagnostics.Stopwatch;
using System.Collections;

namespace Virgis
{
    public class DemLoader : MeshloaderPrototype<string> {

        private enum SourceType {
            PDAL,
            GDAL,
            MDAL,
            XYZ,
            None
        }

        public override async Task _init() {
            await base._init();
            Stopwatch stopWatch = Stopwatch.StartNew();
            RecordSet layer = _layer as RecordSet;
            m_symbology = layer.Units;
            Load();
            await LoadLayer(layer);
            Debug.Log($"Dem Layer Load took {stopWatch.Elapsed.TotalSeconds}");
        }

        private async Task LoadLayer(RecordSet layer) {
            string ex = Path.GetExtension(layer.Source).ToLower();
            // Determine the DAL to be used to load the data.
            // GDAL data is loaded throu PDAL to get a mesh - but the pipeline is radically different
            //
            if (".2dm .nc .dat .adf .out .grb .hdf .slf .sww .xdmf .xmdf .tin".Contains(ex)) {
                await LoadMdal(layer);
            } else if (".bpf .json .e57 .mat .txt .las .nitf .npy .csd .pcd .ply .pts .qi .rxp .rdbx .sbet .slpk .bin ".Contains(ex) || new Regex(@"\.m\d\d").IsMatch(ex)) {
                await LoadPDAL(layer, SourceType.PDAL);
            } else if (".xyz".Contains(ex)) {
                await LoadPDAL(layer, SourceType.XYZ);
            }
            {
                await LoadPDAL(layer, SourceType.GDAL);
            }
        }

        /// <summary>
        /// Load using PDAL
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        private async Task LoadPDAL(RecordSet layer, SourceType sourceType) {
            string proj = null;
            double scalingFactor = 0;
            string headerString;


            (long, Pipeline) value() {

                m_Meshes = new List<DMesh3>();

                List<object> pipe = new();

                // Set up the pipline for GDAL data
                // Get the metadata through GDAL first
                if (sourceType == SourceType.GDAL) {
                    Dataset raster = Gdal.Open(layer.Source, Access.GA_ReadOnly);
                    int numBands = raster.RasterCount;
                    if (numBands <= 0)
                        throw new NotSupportedException($" No Data in file {layer.Source}");
                    proj = raster.GetProjection();

                    //Make the header string from the number of bands - assume band-1 is elevation
                    headerString = "Z";
                    for (int i = 1; i < numBands; i++) {
                        headerString += $",M{i}";
                    }
                    pipe.Add(new {
                        type = "readers.gdal",
                        filename = layer.Source,
                        header = headerString
                    });

                    //get the null value and filter out null data
                    Band band1 = raster.GetRasterBand(1);
                    band1.GetNoDataValue(out double noDataValue, out int hasval);
                    if (hasval == 1) {
                        if (noDataValue < 0)
                            pipe.Add(new {
                                type = "filters.range",
                                limits = $"Z[{noDataValue + 1}:]"
                            });
                        else
                            pipe.Add(new {
                                type = "filters.range",
                                limits = $"Z[:{noDataValue - 1}]"
                            });
                    }

                    // Get the size and pixel size of the raster
                    // if the raster has more than 40,000 data points, using poisson sampling to down size
                    long datapoints = raster.RasterXSize * raster.RasterYSize;
                    if (datapoints > 40000) {
                        try {
                            double[] geoTransform = new double[6];
                            raster.GetGeoTransform(geoTransform);
                            if (geoTransform == null && geoTransform[1] == 0) {
                                throw new Exception();
                            }
                            scalingFactor = Math.Sqrt(datapoints / 40000d * geoTransform[1]);
                        } catch {
                            scalingFactor = Math.Sqrt(datapoints / 40000d);
                        };

                        pipe.Add(new {
                            type = "filters.sample",
                            radius = scalingFactor
                        });
                    }
                    band1.FlushCache();
                    band1.Dispose();
                    raster.FlushCache();
                    raster.Dispose();
                    // special treatment for .xyz files that are not handled well by the defaults
                } else if (sourceType == SourceType.XYZ)
                    pipe.Add(new {
                        type = "readers.text",
                        filename = layer.Source,
                    });

                // for PDAL data - use the default reader
                else
                    pipe.Add(layer.Source);

                // if there is a filter definituion in the RecordSet, add that
                if (layer.Properties.Filter != null) {
                    foreach (Dictionary<string, object> item in layer.Properties.Filter)
                        pipe.Add(item);
                }

                // if there is a Color Interpolation definition in the RecordSet, add that
                if (m_bodySymbology.GetCI(out Dictionary<string, object> ci)) {
                    pipe.Add(ci);
                }

                // create a Mesh using Delaunay traingulation
                pipe.Add(new {
                    type = "filters.delaunay"
                });

                // serialize the pipeline to json
                string json = JsonConvert.SerializeObject(new {
                    pipeline = pipe.ToArray()
                });

                Debug.Log(json);

                // create and run the piplene
                Pipeline pipeline = new(json);
                long pointCount = pipeline.Execute();
                return (pointCount, pipeline);
            }
            Task<(long, Pipeline)> task = new(value);
            task.Start();
            (long pointCount, Pipeline pipeLine) = await task;

            // Process Pipeline
            using (PointViewIterator views = pipeLine.Views) {
                views.Reset();
                while (views.HasNext()) {
                    PointView view = views.Next;
                    if (view != null) {
                        BakedMesh bm = await BakedMesh.Initialize(view);
                        DMesh3 mesh = bm.Dmesh;
                        mesh.RemoveMetadata("properties");
                        // set the CRS based on what is known
                        if (proj != null) {
                            mesh.RemoveMetadata("CRS");
                            mesh.AttachMetadata("CRS", proj);
                        }
                        if (layer.ContainsKey("Crs") && layer.Crs != null) {
                            mesh.RemoveMetadata("CRS");
                            mesh.AttachMetadata("CRS", layer.Crs);
                        };
                        mesh.Transform();
                        mesh.Clockwise = true;
                        m_Meshes.Add(mesh);
                    }
                }
            }
            pipeLine.Dispose();
        }

        private async Task LoadMdal(RecordSet layer) {
            // for MDAL files - load the mesh directly
            Datasource ds = await Datasource.LoadAsync(layer.Source);
            m_Meshes = new List<DMesh3>();
            for (int i = 0; i < ds.meshes.Length; i++) {
                DMesh3 mesh = await ds.GetMeshAsync(i);
                mesh.RemoveMetadata("properties");
                mesh.AttachMetadata("properties", new Dictionary<string, object>{
                    { "Name", ds.meshes[i] }
                });
                if (layer.ContainsKey("Crs") && layer.Crs != null && layer.Crs != "") {
                    mesh.RemoveMetadata("CRS");
                    mesh.AttachMetadata("CRS", layer.Crs);
                };
                mesh.Transform();
                m_Meshes.Add(mesh);
            }
        }

        public override Task _save()
        {
            _layer.Position = transform.position.ToPoint();
            _layer.Transform.Position = Vector3.zero;
            _layer.Transform.Rotate = transform.rotation;
            _layer.Transform.Scale = transform.localScale;
            return Task.CompletedTask;
        }

        protected override object GetNextFID() {
            throw new NotImplementedException();
        }

        protected override IEnumerator hydrate() {
            throw new NotImplementedException();
        }
    }
}
