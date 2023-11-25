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
using g3;
using Project;
using System.Threading.Tasks;
using System;

namespace Virgis
{

    public abstract class MeshloaderPrototype : VirgisLoader<List<DMesh3>>
    {
        protected List<Transform> m_meshes; // List of the meshes in the layer
        protected Dictionary<string, Unit> m_symbology;
        protected Unit m_bodySymbology;

        public override Task _init(){
            RecordSet layer = GetMetadata() as RecordSet;
            m_symbology = layer.Units;
            m_symbology.TryGetValue("body", out m_bodySymbology);
            return Task.CompletedTask;
        }

        public override IVirgisFeature _addFeature<T>(T geometry)
        {
            throw new System.NotImplementedException();
        }

        public async override Task _draw() {
            RecordSet layer = GetMetadata() as RecordSet;
            MeshlayerPrototype parent = m_parent as MeshlayerPrototype;
            m_meshes = new List<Transform>();

            transform.position = layer.Position != null ?
                layer.Position.ToVector3() :
                Vector3.zero;
            transform.Translate(AppState.instance.map.transform
                .TransformVector((Vector3) layer.Transform.Position)
            );
            
            bool HasVertexColors = false;
            foreach (DMesh3 dMesh in features)
                if (dMesh.HasVertexColors) HasVertexColors = true;
            await SetMaterial(HasVertexColors);

            foreach (DMesh3 dMesh in features) {
                HasVertexColors |= dMesh.HasVertexColors;
                await dMesh.CalculateMapUVsAsync(m_bodySymbology);
                m_meshes.Add(Instantiate(parent.Mesh, transform)
                    .GetComponent<EditableMesh>()
                    .Draw(dMesh));
            }
            transform.rotation = layer.Transform.Rotate;
            transform.localScale = layer.Transform.Scale;
            return;
        }

        protected VirgisFeature _addFeature(DMesh3 mesh) {
            MeshlayerPrototype parent = m_parent as MeshlayerPrototype;
            features.Add(mesh);
            EditableMesh emesh = Instantiate(parent.Mesh, transform).GetComponent<EditableMesh>();
            m_meshes.Add(emesh.Draw(mesh));
            return emesh;
        }
      

        public override void _checkpoint() { }

        protected async Task SetMaterial(bool hasColor = false) 
        {
            Dictionary<string, float> prop = new();
            Texture2D tex;

            if (hasColor) {
                prop.Add("_hasColor", 1f);
            } else {
                prop.Add("_hasColor", 0f);
            }
            m_parent.SetMaterial(m_bodySymbology.Color, prop);
            m_parent.SetMaterial(m_bodySymbology.Color);
            if (m_bodySymbology.TextureImage is not null) {
                tex = await TextureImage.Get(new Uri(m_bodySymbology.TextureImage));
                if (tex != null) {
                    tex.wrapMode = TextureWrapMode.Clamp;
                }
                m_parent.SetMaterial(m_bodySymbology.Color, tex);
            }
        }
    }
}