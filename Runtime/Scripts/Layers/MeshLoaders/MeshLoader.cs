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

using System.Threading.Tasks;
using System.IO;
using Project;
using System;
using System.Collections;
using System.Linq;

namespace Virgis
{
    public class MeshLoader : MeshloaderPrototype<string>
    {

        public override async Task _init() {
            // Get Mesh source file type
            string ex = Path.GetExtension(Layer.Source).ToLower();

            RecordSet layer = (RecordSet)Layer;

            //Create a submesh based on the layer source file type
            
            MParent.AddSubLayer(Instantiate((MParent as MeshLayerContainer)?.meshLayer, transform).GetComponent<MeshLayer>());
            MeshLayer l = (MeshLayer)SubLayers.Last();
            if (!l.Spawn(transform)) throw new System.Exception("reparenting failed");

            switch (ex) {
                case ".dxf":
                    l.gameObject.AddComponent<DxfLoader>();
                    break;
                case ".obj":
                    l.gameObject.AddComponent<DxfLoader>();
                    break;
                default:
                    l.gameObject.AddComponent<MdalLoader>();
                    break;
            }
            
            await l.SubInit(layer);

        }

        protected override object GetNextFid() {
            throw new NotImplementedException();
        }

        protected override IEnumerator Hydrate() {
            throw new NotImplementedException();
        }
    }
}

