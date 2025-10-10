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
using OSGeo.OSR;
using System.Linq;

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
                await LoadGDAL(layer);
            }
        }

        /// <summary>
        /// Load using GDAL
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        private async Task LoadGDAL(RecordSet layer) {

            m_Meshes = new List<DMesh3>();

            //bool value() {
                // Get the raster
            Dataset raster = Gdal.Open(layer.Source, Access.GA_ReadOnly);
            int numBands = raster.RasterCount;
            if (numBands <= 0)
                throw new NotSupportedException($" No Data in file {layer.Source}");

            //Get the CoordinateTransformoer
            SpatialReference sr = raster.GetSpatialRef();

            // band-1 is elevation
            Band band1 = raster.GetRasterBand(1);

            // get the null value
            band1.GetNoDataValue(out double noDataValue, out int hasval);
            if (hasval == 0)
                noDataValue = 0;
            band1.GetMinimum(out double min, out int hasMin);
            band1.GetMaximum(out double max, out int hasMax);

            if (band1.ToMesh(out DMesh3 mesh)) {
                mesh.EnableVertexColors(Color.white);
                foreach (int vid in mesh.VertexIndices()) {
                    if (Math.Abs(mesh.GetVertex(vid).z) >= Math.Abs(noDataValue)) {
                        MeshResult result = mesh.RemoveVertex(vid);
                        if (result != MeshResult.Ok) {
                            Debug.Log("vertex removal failed " + result.ToString());
                        };
                    } else {
                        switch (m_ColorInterp) {
                            case e_ColorInterp.Interpolate:
                                mesh.SetVertexColor(vid, Grad.Evaluate((float) ((mesh.GetVertex(vid).z - min) / (max - min))));
                                break;
                            case e_ColorInterp.CategoryValue:
                                mesh.SetVertexColor(vid, m_bodySymbology.ColorMap.GetCategoryValue((float)((mesh.GetVertex(vid).z - min) / (max - min))));
                                break;
                            default:
                                mesh.SetVertexColor(vid, (Color)m_bodySymbology.Color);
                                break;
                        }
                        
                    }
                }
                Reducer r = new(mesh);
                r.MinimizeQuadricPositionError = false;
                mesh.RemoveMetadata("CRS");
                mesh.AttachMetadata("CRS", sr);
                mesh.axisOrder = sr.GetAxisOrder();
                mesh.Transform();
                //r.ReduceToTriangleCount(20000);
                m_Meshes.Add(mesh);
            }

            band1.FlushCache();
            raster.FlushCache();
            raster.Dispose();
            return;

            //}
            //Task<(long, Pipeline)> task = new(value);
            //task.Start();
            //(long pointCount, Pipeline pipeLine) = await task;
        }

        /// <summary>
        /// Load using PDAL
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        private async Task LoadPDAL(RecordSet layer, SourceType sourceType) {

            (long, Pipeline) value() {

                m_Meshes = new List<DMesh3>();

                List<object> pipe = new();

                // special treatment for .xyz files that are not handled well by the defaults
                if (sourceType == SourceType.XYZ)
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
            _layer.Position = ((Vector3d)transform.position).ToPoint();
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
