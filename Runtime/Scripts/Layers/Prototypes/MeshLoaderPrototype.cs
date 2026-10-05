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
using VirgisGeometry;
using Project;
using System.Threading.Tasks;
using System.Collections;
using OSGeo.GDAL;
using System;

namespace Virgis
{

    public abstract class MeshloaderPrototype<T> : VirgisLoader<T>
    {
        // List of the meshes in the layer - as DMesh3 in Local Space coordinates
        protected List<DMesh3> MMeshes = new();
        protected UnitPrototype MBodySymbology;

        public override void ReadSymbology(){
            RecordSet layer = GetMetadata() as RecordSet;
            if (MSymbology.TryGetValue("body", out MBodySymbology )) {
                SetupColormap(MBodySymbology);
            } else {
                MBodySymbology = new ();
            }
        }

        public override IVirgisFeature _addFeature<S>(S geometry) {
            switch (geometry) {
                case DMesh3 mesh:
                    changed = true;
                    MeshlayerPrototype parent = MParent as MeshlayerPrototype;
                    MMeshes.Add(mesh);
                    EditableMesh emesh = Instantiate(parent.Mesh, transform).GetComponent<EditableMesh>();
                    emesh.Draw(mesh, MBodySymbology);
                    emesh.OnEdit(true);
                    return emesh;
                default:
                    throw new NotImplementedException();
            }
        }

        public async override Task _draw() {
            RecordSet layer = GetMetadata() as RecordSet;
            MeshlayerPrototype parent = MParent as MeshlayerPrototype;
            parent.IsWriteable = ! layer.Properties.ReadOnly;
            transform.position = layer.Position != null ?
                (Vector3)layer.Position.ToVector3d() :
                Vector3.zero;
            transform.Translate(AppState.Instance.Map.transform
                .TransformVector((Vector3) layer.Transform.Position)
            );
            
            bool HasVertexColors = false;

            foreach (DMesh3 dMesh in MMeshes) {
                HasVertexColors |= dMesh.HasVertexColors;
                string textureImage = (MBodySymbology as Unit).TextureImage;
                if ( ! String.IsNullOrEmpty(textureImage) 
                ) {
                    Dataset raster = Gdal.Open(textureImage, Access.GA_ReadOnly);
                    await dMesh.CalculateMapUVsAsync(raster);
                } else {
                    dMesh.CalculateUVs();
                }
                Instantiate(parent.Mesh, transform)
                    .GetComponent<EditableMesh>()
                    .Draw(dMesh, MBodySymbology);
            }
            transform.rotation = layer.Transform.Rotate;
            transform.localScale = layer.Transform.Scale;
        }

        protected abstract object GetNextFid();

        public async override Task _save() {
            IEnumerator saver = Hydrate();
            while (saver.MoveNext()) {
                await Task.Yield();
            }
            await transform.parent.GetComponent<VirgisLayer>().GetLoader()._save();
        }
        

    }
}