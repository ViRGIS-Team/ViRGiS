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
using g3;
using Project;

namespace Virgis
{

    public abstract class MeshloaderPrototype : VirgisLoader<List<DMesh3>>
    {
        protected List<Transform> m_meshes; // List of the meshes in the layer
        protected Dictionary<string, Unit> m_symbology;

        public override IVirgisFeature _addFeature<T>(T geometry)
        {
            throw new System.NotImplementedException();
        }

        protected VirgisFeature _addFeature(DMesh3 mesh) {
            MeshlayerPrototype parent = m_parent as MeshlayerPrototype;
            features.Add(mesh);
            EditableMesh emesh = Instantiate(parent.Mesh, transform).GetComponent<EditableMesh>();
            m_meshes.Add(emesh.Draw(mesh, parent.MeshMaterial, parent.WireframeMaterial));
            return emesh;
        }
      

        public override void _checkpoint() { }
    }
}

