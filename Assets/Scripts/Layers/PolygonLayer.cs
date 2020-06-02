// copyright Runette Software Ltd, 2020. All rights reserved
using GeoJSON.Net;
using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Newtonsoft.Json.Linq;
using Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SocialPlatforms.GameCenter;
using UnityEngine.UI;

namespace Virgis {

    /// <summary>
    /// Controls an instance of a Polygon Layer
    /// </summary>
    public class PolygonLayer : Layer<GeographyCollection, FeatureCollection> {

        // The prefab for the data points to be instantiated
        public GameObject CylinderLinePrefab; // Prefab to be used for cylindrical lines
        public GameObject CuboidLinePrefab; // prefab to be used for cuboid lines
        public GameObject SpherePrefab; // prefab to be used for Vertex handles
        public GameObject CubePrefab; // prefab to be used for Vertex handles
        public GameObject CylinderPrefab; // prefab to be used for Vertex handle
        public GameObject PolygonPrefab; // Prefab to be used for the polygons
        public GameObject LabelPrefab; // Prefab to used for the Labels
        public Material PointBaseMaterial;
        public Material LineBaseMaterial;
        public Material BodyBaseMaterial;

        private GameObject HandlePrefab;
        private GameObject LinePrefab;

        private GeoJsonReader geoJsonReader;
        private Dictionary<string, Unit> symbology;
        private Material mainMat;
        private Material selectedMat;
        private Material lineMain;
        private Material lineSelected;
        private Material bodyMain;
        private Material bodySelected;


        protected override async Task _init(GeographyCollection layer) {
            geoJsonReader = new GeoJsonReader();
            await geoJsonReader.Load(layer.Source);
            features = geoJsonReader.getFeatureCollection();
            symbology = layer.Properties.Units;

            if (symbology.ContainsKey("point") && symbology["point"].ContainsKey("Shape")) {
                Shapes shape = symbology["point"].Shape;
                switch (shape) {
                    case Shapes.Spheroid:
                        HandlePrefab = SpherePrefab;
                        break;
                    case Shapes.Cuboid:
                        HandlePrefab = CubePrefab;
                        break;
                    case Shapes.Cylinder:
                        HandlePrefab = CylinderPrefab;
                        break;
                    default:
                        HandlePrefab = SpherePrefab;
                        break;
                }
            } else {
                HandlePrefab = SpherePrefab;
            }

            if (symbology.ContainsKey("line") && symbology["line"].ContainsKey("Shape")) {
                Shapes shape = symbology["line"].Shape;
                switch (shape) {
                    case Shapes.Cuboid:
                        LinePrefab = CuboidLinePrefab;
                        break;
                    case Shapes.Cylinder:
                        LinePrefab = CylinderLinePrefab;
                        break;
                    default:
                        LinePrefab = CylinderLinePrefab;
                        break;
                }
            } else {
                LinePrefab = CylinderLinePrefab;
            }

            Color col = symbology.ContainsKey("point") ? (Color) symbology["point"].Color : Color.white;
            Color sel = symbology.ContainsKey("point") ? new Color(1 - col.r, 1 - col.g, 1 - col.b, col.a) : Color.red;
            Color line = symbology.ContainsKey("line") ? (Color) symbology["line"].Color : Color.white;
            Color lineSel = symbology.ContainsKey("line") ? new Color(1 - line.r, 1 - line.g, 1 - line.b, line.a) : Color.red;
            Color body = symbology.ContainsKey("body") ? (Color) symbology["body"].Color : Color.white;
            Color bodySel = symbology.ContainsKey("body") ? new Color(1 - body.r, 1 - body.g, 1 - body.b, body.a) : Color.red;
            mainMat = Instantiate(PointBaseMaterial);
            mainMat.SetColor("_BaseColor", col);
            selectedMat = Instantiate(PointBaseMaterial);
            selectedMat.SetColor("_BaseColor", sel);
            lineMain = Instantiate(LineBaseMaterial);
            lineMain.SetColor("_BaseColor", line);
            lineSelected = Instantiate(LineBaseMaterial);
            lineSelected.SetColor("_BaseColor", lineSel);
            bodyMain = Instantiate(BodyBaseMaterial);
            bodyMain.SetColor("_BaseColor", body);
        }

        protected override VirgisComponent _addFeature(Vector3[] geometry) {;
            return _drawFeature(geometry, Datapolygon.FindCenter(geometry));
        }

        protected override void _draw() {
            foreach (Feature feature in features.Features) {
                IDictionary<string, object> properties = feature.Properties;
                string gisId = feature.Id;


                // Get the geometry
                MultiPolygon mPols = null;
                if (feature.Geometry.Type == GeoJSONObjectType.Polygon) {
                    mPols = new MultiPolygon(new List<Polygon>() { feature.Geometry as Polygon });
                } else if (feature.Geometry.Type == GeoJSONObjectType.MultiPolygon) {
                    mPols = feature.Geometry as MultiPolygon;
                }

                foreach (Polygon mPol in mPols.Coordinates) {
                    ReadOnlyCollection<LineString> LinearRings = mPol.Coordinates;
                    LineString perimeter = LinearRings[0];
                    Vector3[] poly = perimeter.Vector3();
                    Vector3 center = Vector3.zero;
                    if (properties.ContainsKey("polyhedral") && properties["polyhedral"] != null) {
                        if (properties["polyhedral"].GetType() != typeof(Point)) {
                            JObject jobject = (JObject) properties["polyhedral"];
                            Point centerPoint = jobject.ToObject<Point>();
                            center = centerPoint.Coordinates.Vector3();
                            properties["polyhedral"] = center.ToPoint();
                        } else {
                            center = (properties["polyhedral"] as Point).Coordinates.Vector3();
                        }
                    } else {
                        center = Datapolygon.FindCenter(poly);
                        properties["polyhedral"] = center.ToPoint();
                    }
                    _drawFeature(poly, center, gisId, properties as Dictionary<string, object>);
                }
            }

        }

        protected VirgisComponent _drawFeature(Vector3[] perimeter, Vector3 center, string gisId = null, Dictionary<string, object> properties = null) {
            //Create the GameObjects
            GameObject dataPoly = Instantiate(PolygonPrefab, center, Quaternion.identity, transform);
            GameObject dataLine = Instantiate(LinePrefab, dataPoly.transform, false);
            GameObject centroid = Instantiate(HandlePrefab, dataLine.transform, false);

            // add the gis data from geoJSON
            Datapolygon p = dataPoly.GetComponent<Datapolygon>();
            Datapoint c = centroid.GetComponent<Datapoint>();
            p.gisId = gisId;
            p.gisProperties = properties;
            p.Centroid = c;
            c.SetMaterial(mainMat, selectedMat);

            if (symbology["body"].ContainsKey("Label") && (properties?.ContainsKey(symbology["body"].Label) ?? false)) {
                //Set the label
                GameObject labelObject = Instantiate(LabelPrefab, centroid.transform, false);
                labelObject.transform.Translate(centroid.transform.TransformVector(Vector3.up) * symbology["point"].Transform.Scale.magnitude, Space.Self);
                Text labelText = labelObject.GetComponentInChildren<Text>();
                labelText.text = (string) properties[symbology["body"].Label];
            }

            // Darw the LinearRing
            Dataline Lr = dataLine.GetComponent<Dataline>();
            Lr.Draw(perimeter, true, symbology, LinePrefab, HandlePrefab, null, mainMat, selectedMat, lineMain, lineSelected);

            //Draw the Polygon
            p.Draw(Lr.VertexTable, bodyMain);

            centroid.transform.localScale = symbology["point"].Transform.Scale;
            centroid.transform.localRotation = symbology["point"].Transform.Rotate;
            centroid.transform.localPosition = symbology["point"].Transform.Position;

            return p;
        }

        protected override void _checkpoint() {
        }
        protected override void _save() {
            Datapolygon[] dataFeatures = gameObject.GetComponentsInChildren<Datapolygon>();
            List<Feature> thisFeatures = new List<Feature>();
            foreach (Datapolygon dataFeature in dataFeatures) {
                Dataline perimeter = dataFeature.GetComponentInChildren<Dataline>();
                Vector3[] vertices = perimeter.GetVertexPositions();
                List<Position> positions = new List<Position>();
                foreach (Vector3 vertex in vertices) {
                    positions.Add(vertex.ToPosition() as Position);
                }
                LineString line = new LineString(positions);
                if (!line.IsLinearRing()) {
                    Debug.LogError("This Polygon is not a Linear Ring");
                    return;
                }
                List<LineString> LinearRings = new List<LineString>();
                LinearRings.Add(line);
                IDictionary<string, object> properties = dataFeature.gisProperties;
                Datapoint centroid = dataFeature.Centroid;
                properties["polyhedral"] = centroid.transform.position.ToPoint();
                thisFeatures.Add(new Feature(new Polygon(LinearRings), properties, dataFeature.gisId));
            };
            FeatureCollection FC = new FeatureCollection(thisFeatures);
            geoJsonReader.SetFeatureCollection(FC);
            geoJsonReader.Save();
            features = FC;
        }

        public override GameObject GetFeatureShape() {
            GameObject fs = Instantiate(HandlePrefab);
            Datapoint dp = fs.GetComponent<Datapoint>();
            dp.SetMaterial(mainMat, selectedMat);
            return fs;
        }

        public override void Translate(MoveArgs args) {
            changed = true;
        }

        public override void MoveAxis(MoveArgs args) {
            changed = true;
        }
    }
}
