#if USE_MAPBOX

using UnityEngine;
using Mapbox.Unity.Map;

namespace Virgis {

    public class VirgisAbstractMap : AbstractMap {

        public Material tileMaterial;

        public void SetProperties(ImagerySourceType imageSource, ElevationLayerType elevationLayerType, ElevationSourceType elevationSource, int size = 1) {
            _imagery.SetProperties(imageSource, false, false, false);
            _terrain.SetProperties(elevationSource, elevationLayerType);
            MapOptions mapOptions = Options;
            MapExtentOptions extentOptions = mapOptions.extentOptions;
            extentOptions.extentType = MapExtentType.RangeAroundCenter;
            extentOptions.defaultExtents.rangeAroundCenterOptions.east = size;
            extentOptions.defaultExtents.rangeAroundCenterOptions.west = size;
            extentOptions.defaultExtents.rangeAroundCenterOptions.north = size;
            extentOptions.defaultExtents.rangeAroundCenterOptions.south = size;
            SetTileMaterial(tileMaterial);
        }
    }
}
#endif