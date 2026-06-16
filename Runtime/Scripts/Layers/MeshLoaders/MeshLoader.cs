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

using CsvHelper;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using Project;
using VirgisGeometry;
using System;
using OSGeo.OGR;
using OSGeo.OSR;
using DXF = netDxf;
using netDxf.Entities;
using System.Collections;

namespace Virgis
{
    public class MeshLoader : MeshloaderPrototype<string>
    {
        private Layer m_entities;

        public SpatialReference GetCrs() {
            return m_crs as SpatialReference;
        }

        private Task<DMesh3Builder> loadObj(string filename)
        {
            TaskCompletionSource<DMesh3Builder> tcs1 = new TaskCompletionSource<DMesh3Builder>();
            Task<DMesh3Builder> t1 = tcs1.Task;
            t1.ConfigureAwait(false);

            // Start a background task that will complete tcs1.Task
            Task.Factory.StartNew(() => {

                DMesh3Builder meshBuilder = new DMesh3Builder();
                try {
                    IOReadResult result = StandardMeshReader.ReadFile(filename, new ReadOptions(), meshBuilder);
                } catch (Exception e)  {
                    Debug.LogError("Failed to Load" + filename + " : " + e.ToString());
                    meshBuilder = new DMesh3Builder();
                }
                tcs1.SetResult(meshBuilder);
            });
            return t1;
        }

        private Task<DXF.DxfDocument> loadDxf(string filename) {
            TaskCompletionSource<DXF.DxfDocument> tcs1 = new TaskCompletionSource<DXF.DxfDocument>();
            Task<DXF.DxfDocument> t1 = tcs1.Task;
            t1.ConfigureAwait(false);

            // Start a background task that will complete tcs1.Task
            Task.Factory.StartNew(() => {

                DXF.DxfDocument doc;
                try {
                    using (Stream stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                        doc = DXF.DxfDocument.Load(stream);
                        stream.Close();
                    }
                } catch (Exception e) {
                    Debug.LogError("Failed to Load" + filename + " : " + e.ToString());
                    doc = new DXF.DxfDocument();
                    tcs1.SetResult(doc);
                    throw e;
                };
            });
            return t1;
        }

        private void saveObj(string filename, List<WriteMesh> meshes) {
            using (TextWriter writer = File.CreateText(filename)) {
                OBJWriter objWriter = new OBJWriter();
                try {
                    WriteOptions opts = new WriteOptions() {
                        bWriteBinary = false,
                        bPerVertexColors = meshes[0].Mesh.HasVertexColors,
                        bPerVertexNormals = meshes[0].Mesh.HasVertexNormals,
                        bPerVertexUVs = meshes[0].Mesh.HasVertexUVs
                    };
                    objWriter.Write(writer, meshes, opts);
                } catch (Exception e)  {
                    Debug.LogError("Failed to Write" + filename + " : " + e.ToString());
                }
            }  
        }

        public override async Task _init() {
            RecordSet layer = _layer as RecordSet;
            m_symbology = layer.Units;
            Load();
            IsWriteable = true;

            string ex = Path.GetExtension(layer.Source).ToLower();
            if (ex != ".dxf") {
                DMesh3Builder meshes = await loadObj(layer.Source);
                m_Meshes = meshes.Meshes;
                foreach (DMesh3 mesh in m_Meshes) {
                    foreach (int idx in mesh.VertexIndices()) {
                        Vector3d vtx = mesh.GetVertex(idx);
                        mesh.SetVertex(idx, new Vector3d(vtx.x, vtx.z, vtx.y));
                        mesh.RemoveMetadata("properties");
                        mesh.AttachMetadata("properties", new Dictionary<string, object>{
                    { "Name", layer.DisplayName }
                });
                        if (layer.ContainsKey("Crs") && layer.Crs != null && layer.Crs != "") {
                            mesh.RemoveMetadata("CRS");
                            mesh.AttachMetadata("CRS", layer.Crs);
                        };
                    }
                }
                m_symbology = layer.Units;
            }

            if (ex == ".dxf") {
                //
                // Try opening with netDxf - this will only open files in autoCAD version 2000 or later
                //
                if (layer.Crs != null && layer.Crs != "") SetCrs(OsrExtensions.TextToSR(layer.Crs));
                DMesh3Builder builder = new();
                DxfReader reader = new();
                IOReadResult result;
                using (Stream stream = File.Open(layer.Source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    result = reader.Read(stream, new ReadOptions(), builder);
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

                        m_entities = ogrReader.GetLayers()[0];
                        SetCrs(OgrReader.getSR(m_entities, layer));
                        AxisOrder ax = GetCrs().GetAxisOrder();
                        RecordSet metadata = GetMetadata() as RecordSet;
                        if (metadata.Properties.BBox != null) {
                            m_entities.SetSpatialFilterRect(metadata.Properties.BBox[0], metadata.Properties.BBox[1],
                                metadata.Properties.BBox[2], metadata.Properties.BBox[3]);
                        }

                        await ogrReader.GetFeaturesAsync(m_entities);
                        foreach (Feature feature in ogrReader.features) {
                            Geometry geom = feature.GetGeometryRef();
                            if (geom == null)
                                continue;
                            wkbGeometryType ftype = geom.GetGeometryType();
                            OgrReader.Flatten(ref ftype);
                            //
                            // Get the faces
                            //
                            if (ftype == wkbGeometryType.wkbPolygon) {
                                List<Geometry> LinearRings = new List<Geometry>();
                                List<DCurve3> curves = new List<DCurve3>();
                                for (int i = 0; i < geom.GetGeometryCount(); i++)
                                    LinearRings.Add(geom.GetGeometryRef(i));
                                //
                                // Load the faces as a list of DCurve3
                                //
                                foreach (Geometry LinearRing in LinearRings) {
                                    wkbGeometryType type = LinearRing.GetGeometryType();
                                    if (type == wkbGeometryType.wkbLinearRing ||
                                        type == wkbGeometryType.wkbLineString25D ||
                                        type == wkbGeometryType.wkbLineString) {
                                        LinearRing.CloseRings();
                                        DCurve3 curve = LinearRing.ToCurve(GetCrs());
                                        if (curve.VertexCount > 4) {
                                            Debug.LogError("incorrect face size");
                                        } else {
                                            if (curve.VertexCount == 3) {
                                                curves.Add(curve);
                                            } else {
                                                List<Vector3d> vertices = curve.Vertices as List<Vector3d>;
                                                Vector3d[] tri1 = new Vector3d[4] {
                                                    vertices[0], vertices[1], vertices[2], vertices[0]
                                                };
                                                DCurve3 curve1 = new();
                                                curve1.SetVertices(tri1);
                                                curve1.Closed = geom.IsRing();
                                                curves.Add(curve1);
                                                Vector3d[] tri2 = new Vector3d[4] {
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

                DMesh3 dmesh = builder.Meshes[0];

                try {
                    if (GetCrs() != null) {
                        dmesh.RemoveMetadata("CRS");
                        dmesh.AttachMetadata("CRS", layer.Crs);
                    }
                    
                    dmesh.Transform();
                    dmesh.CompactInPlace();
                } catch (Exception e) {
                    Debug.LogError(e.Message);
                }

                if (!dmesh.CheckValidity(out MeshResult res2)) {
                    UnityEngine.Debug.Log("Loading Mesh created a defective mesh " + res2.ToString());
                }

                MeshConnectedComponents components = new(dmesh);

                // Find connected components
                components.FindConnectedT();

                m_Meshes = new();

                if (components.Components.Count > 1) {
                    // Extract each connected submesh
                    foreach (var comp in components.Components) {
                        DMesh3 submesh = new DSubmesh3Legacy(dmesh, comp.Indices).SubMesh;
                        m_Meshes.Add(submesh);
                    }
                } else {
                    m_Meshes.Add(dmesh);
                }
            }
        }

        public override Task _save()
        {
            RecordSet layer = _layer as RecordSet;
            layer.Position = ((Vector3d)transform.position).ToPoint();
            layer.Transform.Position = Vector3.zero;
            layer.Transform.Rotate = transform.rotation;
            layer.Transform.Scale = transform.localScale;
            EditableMesh[] meshes = GetComponentsInChildren<EditableMesh>();
            string ex = Path.GetExtension(layer.Source).ToLower();
            m_Meshes = new List<DMesh3>();
            foreach (EditableMesh mesh in meshes) {
                m_Meshes.Add(mesh.GetMesh());
            }
            if (ex == ".obj") {
                List<WriteMesh> wmeshes = new List<WriteMesh>();
                foreach (DMesh3 dmesh in m_Meshes) {
                    DMesh3 mesh = new DMesh3(dmesh);
                    foreach (int idx in mesh.VertexIndices()) {
                        Vector3d vtx = mesh.GetVertex(idx);
                        mesh.SetVertex(idx, new Vector3d(vtx.x, vtx.z, vtx.y));
                    }
                    wmeshes.Add(new WriteMesh(mesh, ""));
                }
                saveObj(layer.Source, wmeshes);
            }
            if (ex == ".dxf") {
                
                CoordinateTransformation trans = null;
                if (GetCrs() != null) {
                    trans = AppState.instance.projectOutTransformer(GetCrs());
                }
                List<WriteMesh> wmeshes = new ();
                foreach (DMesh3 dmesh in m_Meshes) {
                    wmeshes.Add(new WriteMesh(dmesh));
                }
                
                using (Stream stream = File.Open(layer.Source, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite)) {
                    DxfWriter writer = new ();
                    writer.Write(stream, wmeshes, new WriteOptions());
                    stream.Close();
                }
            }
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

