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

namespace Virgis
{

    public class MdalLayer : MeshlayerProtoype
    {
        protected override async Task _init() {
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
                if (layer.ContainsKey("Crs") && layer.Crs != null) {
                    mesh.RemoveMetadata("CRS");
                    mesh.AttachMetadata("CRS", layer.Crs);
                };
                features.Add(mesh);
            }
            return;
        }

        protected override async Task _draw()
        {
            RecordSet layer = GetMetadata();
            Dictionary<string, Unit> symbology = GetMetadata().Properties.Units;
            m_meshes = new List<Transform>();

            foreach (DMesh3 dMesh in features) {
                await dMesh.CalculateUVsAsync();
                m_meshes.Add(Instantiate(Mesh, transform).GetComponent<EditableMesh>().Draw(dMesh, MeshMaterial, WireframeMaterial, true));
            }
            transform.position = AppState.instance.map.transform.TransformVector((Vector3) layer.Transform.Position);
            transform.rotation = layer.Transform.Rotate;
            transform.localScale = layer.Transform.Scale;
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

