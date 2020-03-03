
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using GeoJSON.Net.Geometry;
using GeoJSON.Net.Feature;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;



namespace Project
{
    public class GisProject
    {
        [JsonProperty(PropertyName = "name", Required = Required.Always)]
        public string Name;

        [JsonProperty(PropertyName = "origin", Required = Required.Always)]
        public Point Origin;

        [JsonProperty(PropertyName = "mapscale", Required = Required.Always)]
        public int MapScale;

        [JsonProperty(PropertyName = "cameras", Required = Required.Always)]
        public List<Point> Cameras;

        [JsonProperty(PropertyName = "recordsets", Required = Required.Always)]
        public List<RecordSet> RecordSets;

        public Point Camera
        {
            get { return Cameras[0]; }
        }
    }

    public class RecordSet
    {
        [JsonProperty(PropertyName = "type", Required = Required.Always)]
        public string Type;
        [JsonProperty(PropertyName = "datatype", Required = Required.Always)]
        public RecordSetDataType DataType;
        [JsonProperty(PropertyName = "source")]
        public string Source;
        [JsonProperty(PropertyName = "features")]
        public FeatureCollection Features;
        [JsonProperty(PropertyName = "position")]
        public Point Position;
        [JsonProperty(PropertyName = "transform")]
        public JsonTransform Transform;
    }

    public class JsonTransform
    {
        [JsonProperty(PropertyName = "translate", Required = Required.Always)]
        [JsonConverter(typeof(VectorConverter))]
        public SerializableVector3 Position;
        [JsonProperty(PropertyName = "rotate", Required = Required.Always)]
        [JsonConverter(typeof(QuaternionConverter))]
        public SerializableQuaternion Rotate;
        [JsonProperty(PropertyName = "scale", Required = Required.Always)]
        [JsonConverter(typeof(VectorConverter))]
        public SerializableVector3 Scale;
    }

    public class VectorConverter : JsonConverter
    {
        public VectorConverter()
        {
            
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(Vector3).IsAssignableFrom(objectType);
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
                    SerializableVector3 result = new SerializableVector3(values);
                    return result;
            }

            throw new JsonReaderException("expected null, object or array token but received " + reader.TokenType);
        }


        public override void WriteJson(JsonWriter writer, object vector, JsonSerializer serializer)
        {
            serializer.Serialize(writer, vector);
        }
    }

    public class QuaternionConverter : JsonConverter
    {
        public QuaternionConverter()
        {
            
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(Quaternion).IsAssignableFrom(objectType);
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
                    SerializableQuaternion result = new SerializableQuaternion(values);
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
        GeologyCollective,
        GeographyCollective
    }

    public class GeographyCollection : RecordSet
    {

    }

    public class GeologyCollection : GeographyCollection
    {

    }

    public enum RecordSetDataType
    {
        Point,
        Line,
        Polygon,
        PointCloud,
        Mesh,
        Record,
    }    
    
}