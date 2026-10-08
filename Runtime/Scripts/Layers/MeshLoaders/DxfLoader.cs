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
using Project;
using VirgisGeometry;
using System;
using OSGeo.OGR;
using OSGeo.OSR;
using System.Collections;
using System.Linq;

namespace Virgis
{
    public class DxfLoader : MeshloaderPrototype<string>
    {
        private Layer _mEntities;

        public SpatialReference GetCrs() {
            return MCrs as SpatialReference;
        }

        public override async Task _init() {
            RecordSet layer = (RecordSet) Layer;
            if (layer?.Source is null) return;
            DataUnit = new() { Representation = DataUnitRepresent.Manifold };
            MSymbology = layer.Units.ToDictionary(x => x.Key, x => (UnitPrototype) x.Value);
            ReadSymbology();
            IsWriteable = !layer.Properties.ReadOnly;

            //
            // Try opening with netDxf - this will only open files in autoCAD version 2000 or later
            //
            if (!string.IsNullOrEmpty(layer.Crs)) SetCrs(OsrExtensions.TextToSR(layer.Crs));
            DMesh3Builder builder = new();
            DxfReader reader = new();
            IOReadResult result;

            await using (Stream stream = File.Open(layer.Source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                result = reader.Read(stream, new ReadOptions(), builder, GetCrs().GetAxisOrder());
                stream.Close();
            }
            switch (result.code) {
                case IOCode.Ok:
                    break;
                case IOCode.GenericReaderError:
                    throw new Exception(result.message);
                case IOCode.FormatNotSupportedError: {
                    //
                    // if netDXF fails - try opening in GDAL that can open AutoCAD 2 file
                    //
                    builder = new DMesh3Builder();
                    builder.AppendNewMesh(false, false, false, false);
                    using OgrReader ogrReader = new OgrReader();
                    await ogrReader.Load(layer.Source, layer.Properties.ReadOnly ? 0 : 1,
                        layer.Properties.SourceType);

                    _mEntities = ogrReader.GetLayers()[0];
                    SetCrs(OgrReader.getSR(_mEntities, layer));
                    AxisOrder ax = GetCrs().GetAxisOrder();
                    RecordSet metadata = GetMetadata() as RecordSet;
                    if (metadata?.Properties.BBox != null) {
                        _mEntities.SetSpatialFilterRect(metadata.Properties.BBox[0], metadata.Properties.BBox[1],
                            metadata.Properties.BBox[2], metadata.Properties.BBox[3]);
                    }

                    await ogrReader.GetFeaturesAsync(_mEntities);
                    foreach (Feature feature in ogrReader.Features) {
                        Geometry geom = feature.GetGeometryRef();
                        if (geom == null)
                            continue;
                        wkbGeometryType fType = geom.GetGeometryType();
                        OgrReader.Flatten(ref fType);
                        //
                        // Get the faces
                        //
                        if (fType == wkbGeometryType.wkbPolygon) {
                            List<Geometry> linearRings = new List<Geometry>();
                            List<DCurve3> curves = new List<DCurve3>();
                            for (int i = 0; i < geom.GetGeometryCount(); i++)
                                linearRings.Add(geom.GetGeometryRef(i));
                            //
                            // Load the faces as a list of DCurve3
                            //
                            foreach (Geometry linearRing in linearRings) {
                                wkbGeometryType type = linearRing.GetGeometryType();
                                if (type == wkbGeometryType.wkbLinearRing ||
                                    type == wkbGeometryType.wkbLineString25D ||
                                    type == wkbGeometryType.wkbLineString) {
                                    linearRing.CloseRings();
                                    DCurve3 curve = linearRing.ToCurve(GetCrs());
                                    if (curve.VertexCount > 4) {
                                        Debug.LogError("incorrect face size");
                                    } else {
                                        if (curve.VertexCount == 3) {
                                            curves.Add(curve);
                                        } else {
                                            if (curve.Vertices is List<Vector3d> vertices) {
                                                Vector3d[] tri1 = new Vector3d[] {
                                                    vertices[0], vertices[1], vertices[2], vertices[0]
                                                };
                                                DCurve3 curve1 = new();
                                                curve1.SetVertices(tri1);
                                                curve1.Closed = geom.IsRing();
                                                curves.Add(curve1);

                                                Vector3d[] tri2 = new Vector3d[] {
                                                    vertices[0], vertices[2], vertices[3], vertices[0]
                                                };
                                                DCurve3 curve2 = new();
                                                curve2.SetVertices(tri2);
                                                curve2.Closed = true;
                                                curves.Add(curve2);
                                            }
                                        }
                                    }
                                }
                            }

                            //
                            // for each tri, check to make sure that vertices are in the vertex list and add the tri to the tri list
                            //
                            foreach (DCurve3 curve in curves) {
                                Vector3d v = curve.GetVertex(0);
                                int a = builder.AppendVertex(v.x, v.y, v.z);
                                v = curve.GetVertex(1);
                                int b = builder.AppendVertex(v.x, v.y, v.z);
                                v = curve.GetVertex(2);
                                int c = builder.AppendVertex(v.x, v.y, v.z);
                                builder.AppendTriangle(a, b, c);
                            }
                        }
                    }

                    break;
                }
            }

            DMesh3 dMesh = builder.Meshes[0];

            try {
                if (GetCrs() != null) {
                    dMesh.RemoveMetadata("CRS");
                    dMesh.AttachMetadata("CRS", layer.Crs);
                }

                dMesh.Transform();
                dMesh.CompactInPlace();
            } catch (Exception e) {
                Debug.LogError(e.Message);
            }

            if (!dMesh.CheckValidity(out MeshResult res2)) {
                Debug.Log("Loading Mesh created a defective mesh " + res2.ToString());
            }

            MeshConnectedComponents components = new(dMesh);

            // Find connected components
            components.FindConnectedT();

            MMeshes = new();

            if (components.Components.Count > 1) {
                // Extract each connected submesh
                foreach (var comp in components.Components) {
                    DMesh3 submesh = new DSubmesh3Legacy(dMesh, comp.Indices).SubMesh;
                    MMeshes.Add(submesh);
                }
            } else {
                MMeshes.Add(dMesh);
            }
        }

        public override Task _save() {
            RecordSet layer = (RecordSet)Layer;
            layer.Position = ((Vector3d) transform.position).ToPoint();
            layer.Transform.Position = Vector3.zero;
            layer.Transform.Rotate = transform.rotation;
            layer.Transform.Scale = transform.localScale;
            EditableMesh[] meshes = GetComponentsInChildren<EditableMesh>();
            MMeshes = new List<DMesh3>();
            foreach (EditableMesh mesh in meshes) {
                MMeshes.Add(mesh.GetMesh());
            }

            List<WriteMesh> wMeshes = new();
            foreach (DMesh3 dMesh in MMeshes) {
                wMeshes.Add(new WriteMesh(dMesh));
            }
            
            using (Stream stream = File.Open(layer.Source, FileMode.OpenOrCreate, FileAccess.Write,
                       FileShare.ReadWrite)) {
                DxfWriter writer = new();
                writer.Write(stream, wMeshes, new WriteOptions());
                stream.Close();
            }

            return Task.CompletedTask;
        }

        protected override object GetNextFid() {
            throw new NotImplementedException();
        }

        protected override IEnumerator Hydrate() {
            throw new NotImplementedException();
        }
    }
}

