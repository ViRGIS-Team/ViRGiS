// copyright Runette Software Ltd, 2020. All rights reserved
using Project;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using OSGeo.OGR;
using System.Linq;
using System;

namespace Virgis {

    public class GraphLayer : VirgisLayer<GeographyCollection, Layer[]> {
        // The prefab for the data points to be instantiated
        public GameObject PointLayer;
        public GameObject LineLayer;
        public GameObject PolygonLayer;


        private List<VirgisLayer<GeographyCollection, Layer>> _layers = new List<VirgisLayer<GeographyCollection, Layer>>();


        protected override async Task _init() {
            
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry) {
            throw new NotImplementedException();
        }



        protected override void _draw() {
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
