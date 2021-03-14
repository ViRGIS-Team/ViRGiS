// copyright Runette Software Ltd, 2020. All rights reserved
using Project;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using OSGeo.OGR;
using System;

namespace Virgis {

    public class GraphLayer : VirgisLayer<RecordSet, Layer[]> {
        // The prefab for the data points to be instantiated
        public GameObject PointLayer;
        public GameObject LineLayer;
        public GameObject PolygonLayer;


        private List<VirgisLayer<RecordSet, Layer>> _layers = new List<VirgisLayer<RecordSet, Layer>>();


        protected override Task _init() {
            return Task.CompletedTask;
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry) {
            throw new NotImplementedException();
        }



        protected override Task _draw() {
            return Task.CompletedTask;
        }


        protected override void _checkpoint() {
        }


        protected async override Task _save() {

            foreach (VirgisLayer thisLayer in _layers) {
                await thisLayer.Save();
            }
            return;
        }
    }
}
