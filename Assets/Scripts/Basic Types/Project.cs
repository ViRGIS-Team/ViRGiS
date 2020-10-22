

using System.Collections.Generic;
using System.Linq;
using System;
using GeoJSON.Net.Geometry;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using System.ComponentModel;
using Mapbox.Unity.Map;

namespace Project
{
    public class GisProject : TestableObject
    {
        [JsonProperty(PropertyName = "version", Required = Required.Always)]
        public string ProjectVersion;

        [JsonProperty(PropertyName = "name", Required = Required.Always)]
        public string Name;

        [JsonProperty(PropertyName = "origin", Required = Required.Always)]
        public Point Origin;

        [JsonProperty(PropertyName = "scale", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(1f)]
        public float Scale;

        [JsonProperty(PropertyName = "default_proj")]
        public string projectCrs;

        [JsonProperty(PropertyName = "grid-scale")]
        public float GridScale;

        [JsonProperty(PropertyName = "cameras", Required = Required.Always)]
        public List<Point> Cameras;

        [JsonProperty(PropertyName = "recordsets", Required = Required.Always)]
        [JsonConverter(typeof(RecordsetConverter))]
        public List<RecordSet> RecordSets;
    }

    public class RecordSet : TestableObject
    {
        [JsonProperty(PropertyName = "id", Required = Required.Always)]
        public string Id;
        [JsonProperty(PropertyName = "display-name")]
        public string DisplayName;
        [JsonProperty(PropertyName = "datatype", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public RecordSetDataType DataType;
        [JsonProperty(PropertyName = "source")]
        public string Source;  
        [JsonProperty(PropertyName = "position")]
        public Point Position;
        [JsonProperty(PropertyName = "transform")]
        public JsonTransform Transform;
        [JsonProperty(PropertyName = "properties")]
        public Dictionary<string, object> Properties;
        [JsonProperty(PropertyName = "visible", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(true)]
        public bool Visible;
        [JsonProperty(PropertyName = "proj4")]
        public string Crs;
    }

    public class JsonTransform : TestableObject
    {
        [JsonProperty(PropertyName = "translate", Required = Required.Always)]
        [JsonConverter(typeof(VectorConverter<SerializableVector3>))]
        public SerializableVector3 Position;
        [JsonProperty(PropertyName = "rotate", Required = Required.Always)]
        [JsonConverter(typeof(VectorConverter<SerializableQuaternion>))]
        public SerializableQuaternion Rotate;
        [JsonProperty(PropertyName = "scale", Required = Required.Always)]
        [JsonConverter(typeof(VectorConverter<SerializableVector3>))]
        public SerializableVector3 Scale;
    }

    public class VectorConverter<T>  : JsonConverter where T: Serializable, new()
    {
        public VectorConverter()
        {

        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(T).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return null;
                case JsonToken.StartArray:
                    JArray jarray = JArray.Load(reader);
                    IList<float> values = jarray.Select(c => (float)c).ToList();
                    T result = new T();
                    result.Update(values);
                    return result;
            }

            throw new JsonReaderException("expected null, object or array token but received " + reader.TokenType);
        }


        public override void WriteJson(JsonWriter writer, object vector, JsonSerializer serializer)
        {
            T newvector = (T)vector;
            serializer.Serialize(writer, newvector.ToArray());
        }
    }

    public class RecordsetConverter : JsonConverter
    {
        public RecordsetConverter()
        {

        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(RecordSet).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return null;
                case JsonToken.StartArray:
                    JArray jarray = JArray.Load(reader);
                    IList<JObject> sets = jarray.Select(c => (JObject)c).ToList();
                    List<RecordSet> result = new List<RecordSet>();
                    foreach (JObject set in sets) {
                        Type type = RecordSetCollectionType.Get((RecordSetDataType) Enum.Parse(typeof(RecordSetDataType), set["datatype"].ToString()));
                        switch (type) {
                            case Type _ when type == typeof(GeographyCollection):
                                result.Add(set.ToObject(typeof(GeographyCollection)) as GeographyCollection);
                                break;
                            case Type _ when type == typeof(GeologyCollection):
                                result.Add(set.ToObject(typeof(GeologyCollection)) as GeologyCollection);
                                break;
                            case Type _ when type == typeof(MapBox):
                                result.Add(set.ToObject(typeof(MapBox)) as MapBox);
                                break;
                            default:
                                result.Add(set.ToObject(typeof(RecordSet)) as RecordSet);
                                break;
                        }
                    }
                    return result;
            }

            throw new JsonReaderException("expected null, object or array token but received " + reader.TokenType);
        }


        public override void WriteJson(JsonWriter writer, object vector, JsonSerializer serializer)
        {
            serializer.Serialize(writer, vector);
        }
    }


    public enum RecordSetType
    {
        RecordSet,
        MapBox,
        GeologyCollection,
        GeographyCollection
    }

    public class MapBox : RecordSet {
        [JsonProperty(PropertyName = "properties")]
        public new MapBoxData Properties;

        public struct MapBoxData {
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
        }
    }

    public class GeographyCollection : RecordSet
    {
        [JsonProperty(PropertyName = "properties")]
        public new GeogData Properties;

        public struct GeogData {
            [JsonProperty(PropertyName = "units", Required = Required.Always)]
            public Dictionary<string, Unit> Units;
            [JsonProperty(PropertyName = "dem")]
            public string Dem;
            [JsonProperty(PropertyName = "colorinterp")]
            public Dictionary<string, object> ColorInterp;
            [JsonProperty(PropertyName = "filter")]
            public List<Dictionary<string, object>> Filter;
            [JsonProperty(PropertyName = "bbox")]
            public List<double> BBox;
            [JsonProperty(PropertyName = "source-type", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            [DefaultValue(SourceType.File)]
            public SourceType SourceType;
            [JsonProperty(PropertyName = "read-only", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            [DefaultValue(false)]
            public bool ReadOnly;
        }
    }

    public class  GeologyCollection : GeographyCollection
    {
        [JsonProperty(PropertyName = "properties")]
        public new GeoData Properties;

        public struct GeoData
        {
            [JsonProperty(PropertyName = "units", Required = Required.Always)]
            public Dictionary<string, Unit> Units;
            [JsonProperty(PropertyName = "lines")]
            public Dictionary<string, GeoTypes> Lines;
            [JsonProperty(PropertyName = "x_sect_type")]
            public string xSect;
            [JsonProperty(PropertyName = "dem")]
            public string Dem;
            [JsonProperty(PropertyName = "colorinterp")]
            public Dictionary<string, object> ColorInterp;
            [JsonProperty(PropertyName = "filter")]
            public List<Dictionary<string, object>> Filter;
            [JsonProperty(PropertyName = "bbox")]
            public List<double> BBox;
            [JsonProperty(PropertyName = "flip", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
            [DefaultValue(false)]
            public bool Flip;
        }
    }


    /// <summary>
    /// Acceptable values for Recordset Type
    /// </summary>
    public enum RecordSetDataType{
        MapBox,
        Vector,
        Raster,
        PointCloud,
        Mesh,
        Mdal,
        Record,
        XSect,
        CSV, 
        Point,
        Line,
        Polygon,
        DEM,
        Graph
    }

    /// <summary>
    /// Used as a lookup to tell which recordset definition should be used with which layer type
    /// </summary>
    public class RecordSetCollectionType {
        public static Type Get(RecordSetDataType rs) {
            switch (rs) {
                case RecordSetDataType.MapBox: return typeof(MapBox);
                case RecordSetDataType.Vector: return typeof(GeographyCollection);
                case RecordSetDataType.Raster: return typeof(GeographyCollection);
                case RecordSetDataType.PointCloud: return typeof(GeographyCollection);
                case RecordSetDataType.Mesh: return typeof(GeographyCollection);
                case RecordSetDataType.Mdal: return typeof(GeographyCollection);
                case RecordSetDataType.Record: return typeof(RecordSet);
                case RecordSetDataType.XSect: return typeof(GeologyCollection);
                case RecordSetDataType.CSV: return typeof(RecordSet);
                case RecordSetDataType.Point: return typeof(GeographyCollection);
                case RecordSetDataType.Line: return typeof(GeographyCollection);
                case RecordSetDataType.Polygon: return typeof(GeographyCollection);
                case RecordSetDataType.DEM: return typeof(GeographyCollection);
                default: return typeof(RecordSet);
            }
        }
    }

    /// <summary>
    /// Acceptable values for Geology Types
    /// </summary>
    public enum GeoTypes{
        Fault,
        Fract,
        Vein
    }

    /// <summary>
    /// Acceptable values for the Source field of a recordset
    /// </summary>
    public enum SourceType {
        File,
        WFS,
        OAPIF,
        WMS,
        WCS,
        DB,
        AWS,
        GCS,
        Azure,
        Alibaba,
        Openstack,
        TCP,
    }


    public class Unit : TestableObject
    {
        /// <summary>
        /// Color used for the unit of symbology.
        /// 
        /// Can be in either integer[0 .. 255] format or float[0..1] format
        /// </summary>
        [JsonProperty(PropertyName = "color", Required = Required.Always)]
        [JsonConverter(typeof(VectorConverter<SerializableColor>))]
        public SerializableColor Color;
        /// <summary>
        /// The shape to be used by the unit of symbology.
        /// 
        /// Must contain an instance of Shapes
        /// </summary>
        [JsonProperty(PropertyName = "shape", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public Shapes Shape;
        /// <summary>
        /// The transfor to be applied to the unit of symnbology
        /// </summary>
        [JsonProperty(PropertyName = "transform", Required = Required.Always)]
        public JsonTransform Transform;
        /// <summary>
        /// The name of a field in the metadata to be used a label for the data entity
        /// </summary>
        [JsonProperty(PropertyName = "label")]
        public string Label;
    }

    /// <summary>
    /// Acceptable value for the Shape field in Symbology
    /// </summary>
    public enum Shapes
    {
        Spheroid,
        Cuboid,
        Cylinder
    }

    /// <summary>
    /// Generic class to make an entity testabble - to allow the members to be tested for their presence
    /// </summary>
    public class TestableObject
    {
        public bool ContainsKey(string propName)
        {
            return GetType().GetMember(propName) != null;
        }
    }

}