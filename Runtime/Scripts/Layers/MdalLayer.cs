using System.Collections.Generic;
using System.Collections;
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
            symbology = layer.Properties.Units;
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
            meshes = new List<Transform>();

            foreach (DMesh3 dMesh in features) {
                await dMesh.CalculateUVsAsync();
                meshes.Add(Instantiate(Mesh, transform).GetComponent<EditableMesh>().Draw(dMesh, MeshMaterial, WireframeMaterial, true));
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

