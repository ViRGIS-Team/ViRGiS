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
using Project;
using g3;
using Mdal;
using System;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Virgis
{

    public class MdalLayer : MeshlayerProtoype
    {
        protected override async Task _init() {
            Stopwatch stopWatch = Stopwatch.StartNew();
            RecordSet layer = _layer as RecordSet;
            isWriteable = true;
            m_symbology = layer.Properties.Units;
            Datasource ds = await Datasource.LoadAsync(layer.Source);
            features = new List<DMesh3>();
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
                features.Add(mesh);
            }
            Debug.Log($"Mdal Layer Load took : {stopWatch.Elapsed.TotalSeconds}");
            return;
        }

        protected override async Task _draw()
        {
            Stopwatch stopWatch = Stopwatch.StartNew();
            RecordSet layer = GetMetadata();
            Dictionary<string, Unit> symbology = GetMetadata().Properties.Units;
            m_meshes = new List<Transform>();
            Material mat = Instantiate(MeshMaterial);

            if (symbology.TryGetValue("body", out Unit bodySymbology)) {
                if (bodySymbology.TextureImage is not null ) {
                    mat = ImageMaterial;
                    Texture tex = await TextureImage.Get(new Uri(bodySymbology.TextureImage));
                    if (tex != null) {
                        tex.wrapMode = TextureWrapMode.Clamp;
                    }
                    mat.SetTexture("_BaseMap", tex);
                }
            }

            foreach (DMesh3 dMesh in features) {
                await dMesh.CalculateMapUVsAsync(bodySymbology);
                Debug.Log($"Time before Transform {stopWatch.Elapsed.TotalSeconds}");
                dMesh.Transform();
                Debug.Log($"Time after Transform {stopWatch.Elapsed.TotalSeconds}");
                m_meshes.Add(Instantiate(Mesh, transform).GetComponent<EditableMesh>().Draw(dMesh, mat, WireframeMaterial));
            }
            transform.SetPositionAndRotation(AppState.instance.map.transform.TransformVector((Vector3) layer.Transform.Position), layer.Transform.Rotate);
            transform.localScale = layer.Transform.Scale;
            Debug.Log($"Mdal Layer Draw took : {stopWatch.Elapsed.TotalSeconds}");
        }

        protected override Task _save()
        {
            _layer.Transform.Position = AppState.instance.map.transform.InverseTransformVector(transform.position);
            _layer.Transform.Rotate = transform.rotation;
            _layer.Transform.Scale = transform.localScale;
            return Task.CompletedTask;
        }
    }
}

