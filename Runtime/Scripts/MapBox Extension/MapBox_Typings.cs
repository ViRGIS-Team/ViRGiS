#if USE_MAPBOX
using GeoJSON.Net.Geometry;
using Mapbox.Utils;


namespace Virgis {

    public static class MapboxExtensionMethods {
        /// <summary>
        /// Converts Iposition to Vector2D
        /// </summary>
        /// <param name="position">IPosition</param>
        /// <returns>Mapbox.Utils.Vector2d</returns>
        public static Vector2d Vector2d(this IPosition position) {
            return new Vector2d(position.Latitude, position.Longitude);
        }
    }
}
#endif
