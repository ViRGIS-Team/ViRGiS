using UnityEngine;
using System.Threading.Tasks;
using Project;

namespace Virgis {

    public class MapBoxLayer : VirgisLayer<RecordSet, MapBox> { 

        protected override async Task _init() {
#if USE_MAPBOX
            MapBox layer = _layer as MapBox;
            MapBox.MapBoxData props = layer.Properties;
            VirgisAbstractMap mbLayer = GetComponent<VirgisAbstractMap>();
            mbLayer.UseWorldScale();
            mbLayer.SetProperties(props.imagerySourceType, props.elevationLayerType, props.elevationSourceType, props.MapSize);
            mbLayer.Initialize(appState.project.Origin.Coordinates.Vector2d(), props.MapScale);
            Debug.Log(" Mapbox Options : " + mbLayer.Options.ToString());
#endif
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry) {
            throw new System.NotImplementedException();
        }

        protected override void _checkpoint() {
            //do nothing
        }

        protected override void _draw() {
            // do nothing
        }

        protected override Task _save() {
            return Task.CompletedTask;
        }
    }
}


