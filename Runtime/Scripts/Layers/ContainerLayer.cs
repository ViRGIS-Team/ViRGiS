using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Project;

namespace Virgis {

    public class ContainerLayer<T, S> : VirgisLayer<T, S> where T : RecordSet {

        protected new void Awake() {
            base.Awake();
            isContainer = true;
            subLayers = new List<IVirgisLayer>();
        }

        protected override Task _init() {
            return Task.CompletedTask;
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry) {
            throw new System.NotImplementedException();
        }

        protected override void _checkpoint() {
            foreach (IVirgisLayer layer in subLayers) {
                layer.CheckPoint();
            }
        }

        protected override async Task _draw() {
            foreach (IVirgisLayer layer in subLayers) {
                await layer.Draw();
            }
            return;
        }

        protected override async Task _save() {
            foreach (IVirgisLayer layer in subLayers) {
                await layer.Save();
            }
            return;
        }
    }
}

