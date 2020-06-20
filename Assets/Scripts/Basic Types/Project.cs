
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using GeoJSON.Net.Geometry;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using System.ComponentModel;

namespace Project
{
    public class GisProject : TestableObject
    {
        [JsonProperty(PropertyName = "name", Required = Required.Always)]
        public string Name;

        [JsonProperty(PropertyName = "origin", Required = Required.Always)]
        public Point Origin;

        [JsonProperty(PropertyName = "scale", Required = Required.Always)]
        public float Scale;

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
        [JsonProperty(PropertyName = "type", Required = Required.Always)]
        public string Type;
        [JsonProperty(PropertyName = "datatype", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public RecordSetDataType DataType;
        [JsonProperty(PropertyName = "source")]
        public string Source;
        [JsonProperty(PropertyName = "features")]
        public FeatureCollection Features;
        [JsonProperty(PropertyName = "position")]
        public Point Position;
        [JsonProperty(PropertyName = "transform")]
        public JsonTransform Transform;
        [JsonProperty(PropertyName = "properties")]
        public Dictionary<string, object> Properties;
        [JsonProperty(PropertyName = "visible", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        [DefaultValue(true)]
        public bool Visible;
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
                    foreach (JObject set in sets)
                    {

                        if (set["type"].ToString() == RecordSetType.GeographyCollection.ToString()) {
                            result.Add(set.ToObject(typeof(GeographyCollection)) as GeographyCollection);
                        } else if (set["type"].ToString() == RecordSetType.GeologyCollection.ToString())
                        {
                            result.Add(set.ToObject(typeof(GeologyCollection)) as GeologyCollection);
                        } else {
                            result.Add(set.ToObject(typeof(RecordSet)) as RecordSet);
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
        GeologyCollection,
        GeographyCollection
    }

    public class GeographyCollection : RecordSet
    {
        [JsonProperty(PropertyName = "properties")]
        public new GeogData Properties;

        public struct GeogData
        {
            [JsonProperty(PropertyName = "units", Required = Required.Always)]
            public Dictionary<string, Unit> Units;
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
        }

    }

    public enum RecordSetDataType
    {
        MapBox,
        Point,
        Line,
        Polygon,
        PointCloud,
        Mesh,
        Record,
        XSect,
        Tab,
        CSV
    }

    public enum GeoTypes
    {
        Fault,
        Fract,
        Vein
    }


    public class Unit : TestableObject
    {
        [JsonProperty(PropertyName = "color", Required = Required.Always)]
        [JsonConverter(typeof(VectorConverter<SerializableColor>))]
        public SerializableColor Color;
        [JsonProperty(PropertyName = "shape", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public Shapes Shape; 
        [JsonProperty(PropertyName = "transform", Required = Required.Always)]
        public JsonTransform Transform;
        [JsonProperty(PropertyName = "label")]
        public string Label;
    }

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