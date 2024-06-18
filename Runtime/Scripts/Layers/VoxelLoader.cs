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
using UnityEngine.VFX;
using System.Threading.Tasks;
using Project;
using Mdal;
using Pdal;
using OSGeo.OSR;
using VirgisGeometry;


namespace Virgis
{

    public class VoxelLoader : VirgisLoader<List<VoxelMesh>>
    {
        public GameObject pointCloud;


        private GameObject m_model;

        protected new void Awake()
        {
            base.Awake();
        }

        public override async Task _init()
        {
            RecordSet layer = _layer as RecordSet;
            //isWriteable = true;
            Datasource ds = await Datasource.LoadAsync(layer.Source);
            features = new List<VoxelMesh>();
            for (int i = 0; i < ds.meshes.Length; i++) {
                MdalMesh mesh = await ds.GetMeshAsync(i);
                int groupCount = mesh.GetGroupCount();
                for (int j = 0; j < groupCount; j++) {
                    MdalDatasetGroup group = mesh.GetGroup(j);
                    if (group.GetLocation() == MDAL_DataLocation.DataOnVolumes) {
                        group.GetDatasets();
                        features.Add(await group.datasets[0].ToVoxelMeshAsync());
                    }
;                }
            }
            return;
        }

        public override void _checkpoint()
        {
            foreach (IVirgisLayer layer in subLayers)
            {
                layer.CheckPoint();
            }
        }

        public override Task _draw() {
            RecordSet layer = GetMetadata() as RecordSet;
            transform.position = layer.Position != null ? layer.Position.ToVector3() : Vector3.zero;
            if (layer.Transform != null)
                transform.Translate(AppState.instance.Map.transform.TransformVector((Vector3) layer.Transform.Position));
            Dictionary<string, Unit> symbology = layer.Units;

            SpatialReference crs = null;

            if (layer.ContainsKey("Crs") && layer.Crs != null && layer.Crs != "") {
                crs = Convert.TextToSR(layer.Crs);
            }

                foreach (VoxelMesh vmesh in features) {

                /*akedPointCloud pc = BakedPointCloud.Initialize(vmesh.ToBpcData(crs));

                m_model = Instantiate(pointCloud, transform, false);

                VisualEffect vfx = m_model.GetComponent<VisualEffect>();
                vfx.SetTexture("_Positions", pc.positionMap);
                vfx.SetTexture("_Colors", pc.colorMap);
                vfx.SetInt("_pointCount", pc.pointCount);
                vfx.SetVector3("_size", new Vector3(10,10,10));
                vfx.Play();*/
            }

            //if (layer.Transform != null) {
            //    transform.rotation = layer.Transform.Rotate;
            //    transform.localScale = layer.Transform.Scale;
            //    vfx.SetVector3("_scale", layer.Transform.Scale);
            //}
            return Task.CompletedTask;
        }

        public override Task _save()
        {
            return Task.CompletedTask;
        }
    }

    public static class VoxelMeshExtensions {

/*        public static BpcData ToBpcData(this VoxelMesh vmesh, SpatialReference crs = null) {
            BpcData bpc = new BpcData();
            bpc.size = (ulong)vmesh.Count;
            List<Vector3d> positions = new List<Vector3d>();
            List<Colorf> colors = new List<Colorf>();
            if (crs == null) {
                if (vmesh.MetaData.ContainsKey("CRS") && vmesh.MetaData["CRS"] != null && vmesh.MetaData["CRS"] != "") {
                    crs = Convert.TextToSR(vmesh.MetaData["CRS"]);
                }
            }
            foreach (Voxel voxel in vmesh) {
                Vector3d position = Vector3d.Zero;
                foreach (Vector3d vertex in voxel.vertices) {
                    position += vertex;
                }
                position /= (double) voxel.vertices.Count;
                if (crs != null) {
                    CoordinateTransformation trans = AppState.instance.projectTransformer(crs);
                    double[] dV = new double[3] { position.x, position.y, position.z };
                    trans.TransformPoint(dV);
                    AppState.instance.mapTrans.TransformPoint(dV);
                    position = new Vector3d(dV[0], dV[1], dV[2]);
                }
                positions.Add(position);
                colors.Add(voxel.color);
            }
            bpc.positions = positions;
            //bpc.colors = colors;
            return bpc;
        }*/
    }
}


