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
using Project;
using g3;
using Mdal;
using OSGeo.OSR;
using Stopwatch = System.Diagnostics.Stopwatch;
using System.IO;

namespace Virgis
{

    public class MdalLoader : MeshloaderPrototype
    {

        List<string> meshUris;


        public SpatialReference GetCrs() {
            return m_crs as SpatialReference;
        }

        /// <summary>
        /// Initialize the layer and create features as a list of Dmesh3 in local y up coordinates
        /// </summary>
        /// <returns></returns>
        public override async Task _init() {
            await base._init();
            Stopwatch stopWatch = Stopwatch.StartNew();
            meshUris = new ();
            RecordSet layer = _layer as RecordSet;
            isWriteable = true;
            m_symbology = layer.Properties.Units;
            Datasource ds = await Datasource.LoadAsync(layer.Source);
            features = new List<DMesh3>();
            if (layer.ContainsKey("Crs") && layer.Crs != null && layer.Crs != "") {
                SetCrs(Convert.TextToSR(layer.Crs));
            }

            for (int i = 0; i < ds.meshes.Length; i++) {
                MdalMesh mmesh = await ds.GetMeshAsync(i);
                meshUris.Add(mmesh.uri);
                DMesh3 mesh = mmesh;
                mmesh.Dispose();
                mesh.RemoveMetadata("properties");
                mesh.AttachMetadata("properties", new Dictionary<string, object>{
                    { "Name", ds.meshes[i] }
                });
                if (GetCrs() != null)
                {
                    mesh.RemoveMetadata("CRS");
                    mesh.AttachMetadata("CRS", layer.Crs);
                };
                mesh.Transform();
                features.Add(mesh);
            }
            Debug.Log($"Mdal Layer Load took : {stopWatch.Elapsed.TotalSeconds}");
            return;
        }


        /// <summary>
        /// Draw the MDAL mesh
        /// 
        /// Note that features is a list of DMesh3 in local y up coordinates
        /// m_mesh is a List of transforms to the individual Gameobjects created from each Dmesh3
        /// </summary>
        /// <returns></returns>
        //public override Task _draw()
        //{
        //    Stopwatch stopWatch = Stopwatch.StartNew();
        //    RecordSet layer = GetMetadata() as RecordSet;
        //    m_meshes = new List<Transform>();

        //    foreach (DMesh3 dMesh in features) {
        //        m_meshes.Add(Instantiate(parent.Mesh, transform)
        //            .GetComponent<EditableMesh>()
        //            .Draw(dMesh, m_Mat, parent.WireframeMaterial));
        //    }
        //    transform.SetPositionAndRotation(AppState.instance.map.transform
        //        .TransformVector((Vector3) layer.Transform.Position),
        //            layer.Transform.Rotate
        //        );
        //    transform.localScale = layer.Transform.Scale;
        //    Debug.Log($"Mdal Layer Draw took : {stopWatch.Elapsed.TotalSeconds}");
        //    return Task.CompletedTask;
        //}

        public override Task _save()
        {
            RecordSet layer = _layer as RecordSet;
            layer.Transform.Position = Vector3.zero;
            layer.Transform.Rotate = transform.rotation;
            layer.Transform.Scale = transform.localScale;
            EditableMesh[] emeshes = GetComponentsInChildren<EditableMesh>();
            string ex = Path.GetExtension(layer.Source).ToLower();
            features = new List<DMesh3>();
            CoordinateTransformation trans = null;
            if (GetCrs() != null) {
                trans = AppState.instance.projectOutTransformer(GetCrs());
            } else {
                //TODO
            }
            for ( int j = 0; j < emeshes.Length; j++) {
                EditableMesh mesh = emeshes[j];
                DMesh3 dmesh = mesh.GetMesh();
                if (layer.ContainsKey("Crs") && layer.Crs != null && layer.Crs != "") {
                    dmesh.RemoveMetadata("CRS");
                    dmesh.AttachMetadata("CRS", layer.Crs);
                };
                features.Add(dmesh);
                DMesh3 dmesh2 = new(dmesh);
                for (int i = 0; i < dmesh2.VertexCount; i++) {
                    if (dmesh2.IsVertex(i)) {
                        Vector3d vertex = dmesh2.GetVertex(i);
                        double[] dV = new double[3] { vertex.x, vertex.z, vertex.y };
                        trans.TransformPoint(dV);
                        dmesh2.SetVertex(i, new Vector3d(dV));
                    }
                };
                MdalMesh m = MdalMesh.SaveFromDMesh(dmesh2, meshUris[j]);
                m.Dispose();
            }
            return Task.CompletedTask;
        }
    }
}

