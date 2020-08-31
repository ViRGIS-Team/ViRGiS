using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Project;
using pdal;
using Newtonsoft.Json;
using Mdal;
using g3;
using OSGeo.GDAL;
using OSGeo.OSR;
using System;
using Mapbox.Unity.Map;

namespace Virgis
{


    public class DemLayer : VirgisLayer<GeographyCollection, List<DMesh3>> {
        // The prefab for the data points to be instantiated
        public GameObject Mesh;
        public Material MeshMaterial;

        private List<Transform> meshes;
        private Dictionary<string, Unit> symbology;

        private void Start() {
            featureType = FeatureType.MESH;
        }


        protected override async Task _init() {
            GeographyCollection layer = _layer as GeographyCollection;
            string ex = Path.GetExtension(layer.Source).ToLower();
            string sourcetype = null;
            Datasource ds = null;
            string proj = null;
            double scalingFactor = 0;
            string headerString;

            // Determine the DAL to be used to load the data.
            // GDAL data is loaded throu PDAL to get a mesh - but the pipeline is radically different
            //
            if (".2dm .nc .dat .adf .out .grb .hdf .slf .sww .xdmf .xmdf .tin".Contains(ex))
                sourcetype = "mdal";
            else if (".bpf .json .e57 .mat .txt .las .nitf .npy .csd .pcd .ply .pts .qi .rxp .rdbx .sbet .slpk .bin .xyz ".Contains(ex) || new Regex(@"\.m\d\d").IsMatch(ex ))
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
                    pipe.Add(ci);
                }

                // create a Mesh using Delaunay traingulation
                pipe.Add(new {
                    type = "filters.delaunay"
                });

                pipe.Add(new {
                    type = "writers.ply",
                    filename =Path.ChangeExtension(layer.Source, "tmp"),
                    faces = true
                });



                // serialize the pipeline to json
                string json = JsonConvert.SerializeObject(new {
                    pipeline = pipe.ToArray()
                });

                Debug.Log(json);

                // create and run the piplene
                Pipeline pipeline = new Pipeline(json);
                long pointCount = pipeline.Execute();
                pipeline.Dispose();
                ds = new Datasource(Path.ChangeExtension(layer.Source, "tmp"));
            } else {
                // for MDAL files - load the mesh directly
                ds = new Datasource(layer.Source);
            }

            // Get the mesh()es
            features = new List<DMesh3>();
            for (int i = 0; i < ds.meshes.Length; i++) {
                DMesh3 mesh = ds.GetMesh(i);
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
            if (sourcetype != "mdal") {
                File.Delete(Path.ChangeExtension(layer.Source, "tmp"));
            }
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry)
        {
            throw new System.NotImplementedException();
        }

        protected override void _draw() {
            GeographyCollection layer = GetMetadata();
            Dictionary<string, Unit> symbology = GetMetadata().Properties.Units;
            meshes = new List<Transform>();

            foreach (DMesh3 dMesh in features) {
                dMesh.CalculateUVs();
                meshes.Add(Instantiate(Mesh, transform).GetComponent<DataMesh>().Draw(dMesh, MeshMaterial));
            }
            transform.position = AppState.instance.map.transform.TransformVector((Vector3) layer.Transform.Position);
            transform.rotation = layer.Transform.Rotate;
            transform.localScale = layer.Transform.Scale;

        }

        public override void _set_visible() {
            base._set_visible();
            
        }


        public override void Translate(MoveArgs args)
        {

            if (args.translate != Vector3.zero) transform.Translate(args.translate, Space.World);
            changed = true;
        }


        // https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
        public override void MoveAxis(MoveArgs args)
        {
            if (args.translate != Vector3.zero) transform.Translate(args.translate, Space.World);
            args.rotate.ToAngleAxis(out float angle, out Vector3 axis);
            transform.RotateAround(args.pos, axis, angle);
            Vector3 A = transform.localPosition;
            Vector3 B = transform.parent.InverseTransformPoint(args.pos);
            Vector3 C = A - B;
            float RS = args.scale;
            Vector3 FP = B + C * RS;
            if (FP.magnitude < float.MaxValue)
            {
                transform.localScale = transform.localScale * RS;
                transform.localPosition = FP;
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform T = transform.GetChild(i);
                    if (T.GetComponent<Datapoint>() != null)
                    {
                        T.localScale /= RS;
                    }
                }
            }
            changed = true;
        }

        protected override void _checkpoint() { }

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
