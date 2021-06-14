
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using Project;
using OSGeo.OGR;

namespace Virgis {


    public class ContainerLayer<T, S> : VirgisLayer<T, S> where T : RecordSet {

        private void Start() {
            isContainer = true;
        }

        protected override Task _init() {
            return Task.CompletedTask;
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry) {
            throw new System.NotImplementedException();
        }

        protected override void _checkpoint() {
            VirgisLayer[] layers = GetComponentsInChildren<VirgisLayer>();
            foreach (VirgisLayer layer in layers) {
                layer.CheckPoint();
            }
        }

        protected override async Task _draw() {
            for (int i = 0; i < transform.childCount; i++) {
                VirgisLayer layer = transform.GetChild(i).GetComponent<VirgisLayer>();
                if (layer != null)
                    await layer.Draw();
            }
            return;
        }

        protected override async Task _save() {
            for (int i = 0; i < transform.childCount; i++) {
                VirgisLayer layer = transform.GetChild(i).GetComponent<VirgisLayer>();
                if (layer != null)
                    await layer.Save();
            }
            return;
        }
    }
}

