/* MIT License

Copyright (c) 2020 - 21 Runette Software

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
using g3;
using System;
using OSGeo.OGR;
using CoordinateTransformation = OSGeo.OSR.CoordinateTransformation;
using DXF = netDxf;
using netDxf.Entities;

namespace Virgis
{
    public class MeshLayer : MeshlayerProtoype
    {
        private Layer m_entities;

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

        protected override async Task _init() {
            RecordSet layer = _layer as RecordSet;
            isWriteable = true;
            string ex = Path.GetExtension(layer.Source).ToLower();
            if (ex != ".dxf") {
                DMesh3Builder meshes = await loadObj(layer.Source);
                features = meshes.Meshes;
                foreach (DMesh3 mesh in features) {
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
                m_symbology = layer.Properties.Units;
            }
            if (ex == ".dxf") {
                List<Vector3d> vertexes = new List<Vector3d>();
                List<Index3i> tris = new List<Index3i>();

                try {
                    //
                    // Try opening with netDxf - this will only open files in autoCAD version 2000 or later
                    //
                    if (layer.Crs != null && layer.Crs != "") SetCrs(Convert.TextToSR(layer.Crs));
                    DXF.DxfDocument doc;
                    using (Stream stream = File.Open(layer.Source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                        doc = DXF.DxfDocument.Load(stream);
                        stream.Close();
                    }
                    string layout = doc.ActiveLayout;
                    IEnumerable<Face3d> faces = doc.Faces3d;
                    IEnumerable<PolyfaceMesh> pfs = doc.PolyfaceMeshes;
                    List<DCurve3> curves = new List<DCurve3>();
                    CoordinateTransformation transform = AppState.instance.projectTransformer(GetCrs());
                    foreach (Face3d face in faces) {
                        List<Vector3d> tri = new List<Vector3d>();
                        tri.Add(face.FirstVertex.ToVector3d(transform));
                        tri.Add(face.SecondVertex.ToVector3d(transform));
                        tri.Add(face.ThirdVertex.ToVector3d(transform));
                        if (face.FourthVertex != face.ThirdVertex) {
                            Debug.Log(" Not a Triangle");
                        }
                        curves.Add(new DCurve3(tri, false, true));
                    }
                    //
                    // Add the Polyface Meshes
                    //
                    foreach (PolyfaceMesh pfmesh in pfs) {
                        foreach (PolyfaceMeshFace face in pfmesh.Faces) {
                            List<Vector3d> tri = new List<Vector3d>();
                            List<short> verts = face.VertexIndexes;
                            for (int i = 0; i < 3; i++) {
                                tri.Add(pfmesh.Vertexes[Math.Abs(verts[0]) - 1].Position.ToVector3d(transform));
                                tri.Add(pfmesh.Vertexes[Math.Abs(verts[1]) - 1].Position.ToVector3d(transform));
                                tri.Add(pfmesh.Vertexes[Math.Abs(verts[2]) - 1].Position.ToVector3d(transform));
                            }
                            curves.Add(new DCurve3(tri, false, true));
                        }
                    }
                    //
                    // for each face, check to make sure that vertices are in the vertex list and add the tri to the tri list
                    //
                    foreach (DCurve3 curve in curves) {
                        List<int> tri = new List<int>();
                        for (int i = 0; i < 3; i++) {
                            Vector3d v = curve.GetVertex(i);
                            int index = vertexes.IndexOf(v);
                            if (index == -1) {
                                vertexes.Add(v);
                                index = vertexes.IndexOf(v);
                            }
                            tri.Add(index);
                        }
                        tris.Add(new Index3i(tri.ToArray()));
                    }
                } catch {
                    //
                    // if netDXF fails - try opening in GDAL that can open AutoCAD 2 file
                    //
                    using (OgrReader ogrReader = new OgrReader()) {
                        await ogrReader.Load(layer.Source, layer.Properties.ReadOnly ? 0 : 1,  layer.Properties.SourceType);

                        m_entities = ogrReader.GetLayers()[0];
                        SetCrs(OgrReader.getSR(m_entities, layer));
                        RecordSet metadata = GetMetadata();
                        if (metadata.Properties.BBox != null) {
                            m_entities.SetSpatialFilterRect(metadata.Properties.BBox[0], metadata.Properties.BBox[1], metadata.Properties.BBox[2], metadata.Properties.BBox[3]);
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
                                    if (type == wkbGeometryType.wkbLinearRing || type == wkbGeometryType.wkbLineString25D || type == wkbGeometryType.wkbLineString) {
                                        LinearRing.CloseRings();
                                        DCurve3 curve = new DCurve3();
                                        curve.FromGeometry(LinearRing, GetCrs());
                                        if (curve.VertexCount != 4) {
                                            Debug.LogError("incorrect face size");
                                        } else {
                                            curves.Add(curve);
                                        }
                                    }
                                }
                                //
                                // for each tri, check to make sure that vertcie are in the vertex list and add the tri to the tri list
                                //
                                foreach (DCurve3 curve in curves) {
                                    List<int> tri = new List<int>();
                                    for (int i = 0; i < 3; i++) {
                                        Vector3d v = curve.GetVertex(i);
                                        int index = vertexes.IndexOf(v);
                                        if (index == -1) {
                                            vertexes.Add(v);
                                            index = vertexes.IndexOf(v);
                                        }
                                        tri.Add(index);
                                    }
                                    tris.Add(new Index3i(tri.ToArray()));
                                }
                            }
                        }
                    }
                }
                //
                // vertexes and tris should now describe a mesh
                //
                DMesh3 dmesh = new DMesh3(false, false, false, false);
                vertexes.ForEach(v => dmesh.AppendVertex(v));
                tris.ForEach(t => dmesh.AppendTriangle(t));
                try {
                    dmesh.CompactInPlace();
                } catch { }
                features = new List<DMesh3>();
                features.Add(dmesh);
                m_symbology = layer.Properties.Units;
                return;
            }
        }

        protected async override Task _draw()
        {
            RecordSet layer = GetMetadata();
            transform.position = layer.Position != null ? layer.Position.ToVector3() : Vector3.zero;
            transform.Translate(AppState.instance.map.transform.TransformVector((Vector3) layer.Transform.Position));
            Dictionary<string, Unit> symbology = GetMetadata().Properties.Units;
            m_meshes = new List<Transform>();

            foreach (DMesh3 dMesh in features) {
                await dMesh.CalculateUVsAsync();
                dMesh.Transform();
                m_meshes.Add(Instantiate(Mesh, transform).GetComponent<EditableMesh>().Draw(dMesh, MeshMaterial, WireframeMaterial));
            }
            transform.rotation = layer.Transform.Rotate;
            transform.localScale = layer.Transform.Scale;
            return;
        }

        protected override Task _save()
        {
            RecordSet layer = _layer as RecordSet;
            layer.Position = transform.position.ToPoint();
            layer.Transform.Position = Vector3.zero;
            layer.Transform.Rotate = transform.rotation;
            layer.Transform.Scale = transform.localScale;
            EditableMesh[] meshes = GetComponentsInChildren<EditableMesh>();
            string ex = Path.GetExtension(layer.Source).ToLower();
            features = new List<DMesh3>();
            foreach (EditableMesh mesh in meshes) {
                features.Add(mesh.GetMesh());
            }
            if (ex == ".obj") {
                List<WriteMesh> wmeshes = new List<WriteMesh>();
                foreach (DMesh3 dmesh in features) {
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
                DXF.DxfDocument doc = new DXF.DxfDocument();
                CoordinateTransformation transform = null;
                if (GetCrs() != null) {
                    transform = AppState.instance.projectOutTransformer(GetCrs());
                }
                foreach (DMesh3 dmesh in features) {
                    foreach (Index3i tri in dmesh.Triangles()) {
                        DXF.Vector3 v1 = dmesh.GetVertex(tri.a).ToDxfVector3(transform);
                        DXF.Vector3 v2 = dmesh.GetVertex(tri.b).ToDxfVector3(transform);
                        DXF.Vector3 v3 = dmesh.GetVertex(tri.c).ToDxfVector3(transform);
                        doc.AddEntity(new Face3d(v1, v2, v3));
                    }
                }
                using (Stream stream = File.Open(layer.Source, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite)) {
                    doc.Save(stream);
                    stream.Close();
                }

            }
            return Task.CompletedTask;
        }
    }
}

