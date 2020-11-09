using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;


namespace Project {

    public class MapBox : RecordSet {
        [JsonProperty(PropertyName = "properties")]
        public new MapBoxData Properties;

        public struct MapBoxData {
#if USE_MAPBOX
using Mapbox.Unity.Map;
            [JsonProperty(PropertyName = "mapscale", Required = Required.Always)]
            public Int32 MapScale;
            [JsonProperty(PropertyName = "map_size")]
            public int MapSize;
            [JsonProperty(PropertyName = "elevation_source_type", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            [DefaultValue(ElevationSourceType.MapboxTerrain)]
            [JsonConverter(typeof(StringEnumConverter))]
            public ElevationSourceType elevationSourceType;
            [JsonProperty(PropertyName = "elevation_layer_type", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            [DefaultValue(ElevationLayerType.FlatTerrain)]
            [JsonConverter(typeof(StringEnumConverter))]
            public ElevationLayerType elevationLayerType;
            [JsonProperty(PropertyName = "imagery_source_type", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            [DefaultValue(ImagerySourceType.MapboxOutdoors)]
            [JsonConverter(typeof(StringEnumConverter))]
            public ImagerySourceType imagerySourceType;
#endif
        }
    }

}
