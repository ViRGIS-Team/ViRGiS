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
using VirgisGeometry;

namespace Virgis
{
    public class PointCloudLoaderPrototype<T> : VirgisLoader<T>
    {
        protected PointCloudLayer parent;

        protected Dictionary<string, Unit> m_Symbology;
        protected PointCloud m_model;

        protected Task<int> Load() {
            parent = m_parent as PointCloudLayer;
            return Task.FromResult(0);
        }

        protected VirgisFeature _addFeature(Vector3[] geometry)
        {
            throw new System.NotImplementedException();
        }


        public override void _checkpoint() { }

        public override Task _save()
        {
            _layer.Position = ((Vector3d)parent.transform.position).ToPoint();
            _layer.Transform.Position = Vector3.zero;
            _layer.Transform.Rotate = parent.transform.rotation;
            _layer.Transform.Scale = parent.transform.localScale;
            return Task.CompletedTask;
        }
    }
}
