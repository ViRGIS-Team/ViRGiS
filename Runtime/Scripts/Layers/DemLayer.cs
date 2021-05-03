using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Project;
using Pdal;
using Newtonsoft.Json;
using Mdal;
using g3;
using OSGeo.GDAL;
using System;

namespace Virgis
{
    public class DemLayer : MeshlayerProtoype {
        protected override async Task _init() {
            await Load();
        }

        protected Task<int> Load() {

            TaskCompletionSource<int> tcs1 = new TaskCompletionSource<int>();
            Task<int> t1 = tcs1.Task;
            t1.ConfigureAwait(false);

            // Start a background task that will complete tcs1.Task
            Task.Factory.StartNew(() => {
                RecordSet layer = _layer as RecordSet;
                string ex = Path.GetExtension(layer.Source).ToLower();
                string sourcetype = null;
                Datasource ds = null;
                string proj = null;
                double scalingFactor = 0;
                string headerString;
                features = new List<DMesh3>();

                // Determine the DAL to be used to load the data.
                // GDAL data is loaded throu PDAL to get a mesh - but the pipeline is radically different
                //
                if (".2dm .nc .dat .adf .out .grb .hdf .slf .sww .xdmf .xmdf .tin".Contains(ex))
                    sourcetype = "mdal";
                else if (".bpf .json .e57 .mat .txt .las .nitf .npy .csd .pcd .ply .pts .qi .rxp .rdbx .sbet .slpk .bin .xyz ".Contains(ex) || new Regex(@"\.m\d\d").IsMatch(ex))
                    sourcetype = "pdal";
                else
                    sourcetype = "gdal";

                //Loading through PDAL
                if (sourcetype != "mdal") {
                    List<object> pipe = new List<object>();

                    // Set up the pipline for GDAL data
                    // Get the metadata thjrough GDAL first
                    if (sourcetype == "gdal") {
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
                        double noDataValue;
                        int hasval;
                        band1.GetNoDataValue(out noDataValue, out hasval);
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
                    } else if (ex == ".xyz")
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
                    if (layer.Properties.ColorInterp != null) {
                        Dictionary<string, object> ci = new Dictionary<string, object>(layer.Properties.ColorInterp);
                        ci.Add("type", "filters.colorinterp");
                        ci["dimension"] = "Z";
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
                    Pipeline pipeline = new Pipeline(json);
                    long pointCount = pipeline.Execute();
                    using (PointViewIterator views = pipeline.Views) {
                        views.Reset();
                        while (views.HasNext()) {
                            PointView view = views.Next;
                            if (view != null) {
                                DMesh3 mesh = view.getMesh();
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
                                features.Add(mesh);
                            }
                        }
                    }
                    pipeline.Dispose();
                } else {
                    // for MDAL files - load the mesh directly
                    ds = Datasource.Load(layer.Source);

                    for (int i = 0; i < ds.meshes.Length; i++) {
                        DMesh3 mesh = ds.GetMesh(i);
                        mesh.RemoveMetadata("properties");
                        mesh.AttachMetadata("properties", new Dictionary<string, object>{
                        { "Name", ds.meshes[i] }
                    });
                        // set the CRS based on what is known
                        if (proj != null) {
                            mesh.RemoveMetadata("CRS");
                            mesh.AttachMetadata("CRS", proj);
                        }
                        if (layer.ContainsKey("Crs") && layer.Crs != null) {
                            mesh.RemoveMetadata("CRS");
                            mesh.AttachMetadata("CRS", layer.Crs);
                        };
                        features.Add(mesh);
                    }
                }
                tcs1.SetResult(1);
            });
            return t1;
        }

        protected override async Task _draw() {
            RecordSet layer = GetMetadata();
            Dictionary<string, Unit> symbology = GetMetadata().Properties.Units;
            meshes = new List<Transform>();

            foreach (DMesh3 dMesh in features) {
                if (dMesh.HasVertexColors) {
                    MeshMaterial.SetInt("_hasColor", 1);
                }
                await dMesh.CalculateUVsAsync();
                meshes.Add(Instantiate(Mesh, transform).GetComponent<EditableMesh>().Draw(dMesh, MeshMaterial, WireframeMaterial, true));
            }
            transform.position = AppState.instance.map.transform.TransformVector((Vector3) layer.Transform.Position);
            transform.rotation = layer.Transform.Rotate;
            transform.localScale = layer.Transform.Scale;

        }

        protected override Task _save()
        {
            _layer.Position = transform.position.ToPoint();
            _layer.Transform.Position = Vector3.zero;
            _layer.Transform.Rotate = transform.rotation;
            _layer.Transform.Scale = transform.localScale;
            return Task.CompletedTask;
        }
    }
}
