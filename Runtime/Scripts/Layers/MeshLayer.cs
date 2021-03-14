using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Threading.Tasks;
using System.IO;
using Project;
using g3;
using System;
using OSGeo.OGR;
using OSGeo.OSR;
using DXF = netDxf;
using netDxf.Entities;

namespace Virgis
{
    public class MeshLayer : MeshlayerProtoype
    {
        private List<Feature> ogrFeatures;
        private Layer entities;

        private Task<DMesh3Builder> loadObj(string filename)
        {
            TaskCompletionSource<DMesh3Builder> tcs1 = new TaskCompletionSource<DMesh3Builder>();
            Task<DMesh3Builder> t1 = tcs1.Task;
            t1.ConfigureAwait(false);

            // Start a background task that will complete tcs1.Task
            Task.Factory.StartNew(() => {

                using (StreamReader reader = File.OpenText(filename)) {
                    OBJReader objReader = new OBJReader();
                    DMesh3Builder meshBuilder = new DMesh3Builder();
                    try {
                        IOReadResult result = objReader.Read(reader, new ReadOptions(), meshBuilder);
                    } catch (Exception e) when (
                       e is UnauthorizedAccessException ||
                       e is DirectoryNotFoundException ||
                       e is FileNotFoundException ||
                       e is NotSupportedException
                       ) {
                        Debug.LogError("Failed to Load" + filename + " : " + e.ToString());
                        meshBuilder = new DMesh3Builder();
                    }
                    tcs1.SetResult(meshBuilder);
                }
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
                } catch (Exception e) when (
                   e is UnauthorizedAccessException ||
                   e is DirectoryNotFoundException ||
                   e is FileNotFoundException ||
                   e is NotSupportedException
                   ) {
                    Debug.LogError("Failed to Write" + filename + " : " + e.ToString());
                }
            }  
        }

        protected override async Task _init() {
            RecordSet layer = _layer as RecordSet;
            string ex = Path.GetExtension(layer.Source).ToLower();
            if (ex == ".obj") {
                DMesh3Builder meshes = await loadObj(layer.Source);
                features = meshes.Meshes;
                symbology = layer.Properties.Units;
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
                    List<DCurve3> curves = new List<DCurve3>();
                    CoordinateTransformation transform = new CoordinateTransformation(GetCrs(), AppState.instance.mapProj);
                    foreach (Face3d face in faces) {
                        List<Vector3d> tri = new List<Vector3d>();
                        tri.Add(face.FirstVertex.ToVector3d(transform));
                        tri.Add(face.SecondVertex.ToVector3d(transform));
                        tri.Add(face.ThirdVertex.ToVector3d(transform));
                        if (face.FourthVertex != face.ThirdVertex) {
                            Debug.Log(" Not a Tringle");
                        }
                        curves.Add(new DCurve3(tri, false, true));
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
                        if (layer.Properties.SourceType == SourceType.WFS) {
                            await ogrReader.LoadWfs(layer.Source, layer.Properties.ReadOnly ? 0 : 1);
                        } else {
                            await ogrReader.Load(layer.Source, layer.Properties.ReadOnly ? 0 : 1);
                        }
                        entities = ogrReader.GetLayers()[0];
                        SetCrs(OgrReader.getSR(entities, layer));
                        RecordSet metadata = GetMetadata();
                        if (metadata.Properties.BBox != null) {
                            entities.SetSpatialFilterRect(metadata.Properties.BBox[0], metadata.Properties.BBox[1], metadata.Properties.BBox[2], metadata.Properties.BBox[3]);
                        }
                        await ogrReader.GetFeaturesAsync(entities);
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
                features = new List<DMesh3>();
                features.Add(dmesh.Compactify());
                symbology = layer.Properties.Units;
                return;
            }
        }

        protected override Task _draw()
        {
            RecordSet layer = GetMetadata();
            transform.position = layer.Position != null ? layer.Position.ToVector3() : Vector3.zero;
            transform.Translate(AppState.instance.map.transform.TransformVector((Vector3) layer.Transform.Position));
            Dictionary<string, Unit> symbology = GetMetadata().Properties.Units;
            meshes = new List<Transform>();

            foreach (DMesh3 dMesh in features) {
                meshes.Add(Instantiate(Mesh, transform).GetComponent<EditableMesh>().Draw(dMesh, MeshMaterial, WireframeMaterial, false));
            }
            transform.rotation = layer.Transform.Rotate;
            transform.localScale = layer.Transform.Scale;
            return Task.CompletedTask;
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
                foreach (DMesh3 dmesh in features)
                    wmeshes.Add(new WriteMesh(dmesh, ""));
                saveObj(layer.Source, wmeshes);
            }
            if (ex == ".dxf") {
                DXF.DxfDocument doc = new DXF.DxfDocument();
                CoordinateTransformation transform = null;
                if (GetCrs() != null) {
                    transform = new CoordinateTransformation(AppState.instance.mapProj, GetCrs());
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

