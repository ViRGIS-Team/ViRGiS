// copyright Runette Software Ltd, 2020. All rights reserved

using Project;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using g3;
using OSGeo.OGR;
using GeoJSON.Net.CoordinateReferenceSystem;

namespace Virgis
{

    /// <summary>
    /// Controls an instance of a Polygon Layer
    /// </summary>
    public class PolygonLayer : VirgisLayer<GeographyCollection, Layer>
    {

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


        protected override async Task _init(GeographyCollection layer)
        {
            geoJsonReader = new GeoJsonReader();
            await geoJsonReader.Load(layer.Source);
            features = geoJsonReader.getFeatureCollection();
            symbology = layer.Properties.Units;

            if (symbology.ContainsKey("point") && symbology["point"].ContainsKey("Shape"))
            {
                Shapes shape = symbology["point"].Shape;
                switch (shape)
                {
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
            }
            else
            {
                HandlePrefab = SpherePrefab;
            }

            if (symbology.ContainsKey("line") && symbology["line"].ContainsKey("Shape"))
            {
                Shapes shape = symbology["line"].Shape;
                switch (shape)
                {
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
            }
            else
            {
                LinePrefab = CylinderLinePrefab;
            }

            Color col = symbology.ContainsKey("point") ? (Color)symbology["point"].Color : Color.white;
            Color sel = symbology.ContainsKey("point") ? new Color(1 - col.r, 1 - col.g, 1 - col.b, col.a) : Color.red;
            Color line = symbology.ContainsKey("line") ? (Color)symbology["line"].Color : Color.white;
            Color lineSel = symbology.ContainsKey("line") ? new Color(1 - line.r, 1 - line.g, 1 - line.b, line.a) : Color.red;
            Color body = symbology.ContainsKey("body") ? (Color)symbology["body"].Color : Color.white;
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

        protected override VirgisFeature _addFeature(Vector3[] line)
        {
            Geometry geom = new Geometry(wkbGeometryType.wkbLinearRing);
            geom.AssignSpatialReference(AppState.instance.mapProj);
            geom.Vector3(line);
            return _drawFeature(geom, new Feature(new FeatureDefn(null)));
        }

        protected override void _draw()
        {
            long FeatureCount = features.GetFeatureCount(1);
            features.ResetReading();
            for (int i = 0; i < FeatureCount; i++)
            {
                Feature feature = features.GetNextFeature();
                if (feature == null)
                    continue;
                Geometry poly = feature.GetGeometryRef();
                if (poly == null)
                    continue;
                if (poly.GetGeometryType() == wkbGeometryType.wkbPolygon || poly.GetGeometryType() == wkbGeometryType.wkbPolygon25D || poly.GetGeometryType() == wkbGeometryType.wkbPolygonM || poly.GetGeometryType() == wkbGeometryType.wkbPolygonZM) {
                    Geometry line = poly.GetGeometryRef(0);
                    if (line.GetGeometryType() == wkbGeometryType.wkbLinearRing || line.GetGeometryType() == wkbGeometryType.wkbLineString25D) {
                        _drawFeature(line, feature);
                    }
                }
            }

        }

        protected VirgisFeature _drawFeature(Geometry polygon,  Feature feature = null)
        {

            LineString perimeter = (LinearRings as ReadOnlyCollection<LineString>)[0];
            Vector3[] poly = perimeter.Vector3();
            DCurve3 curve = new DCurve3();
            curve.Vector3(poly, true);
            Vector3 center = (Vector3) curve.Center();
            //Create the GameObjects
            GameObject dataPoly = Instantiate(PolygonPrefab, center, Quaternion.identity, transform);



            // add the gis data from geoJSON
            Datapolygon p = dataPoly.GetComponent<Datapolygon>();

            if (feature != null)
                p.feature = feature;


   



            if (symbology["body"].ContainsKey("Label") && symbology["body"].Label != null && (feature?.ContainsKey(symbology["body"].Label) ?? false))
            {
                //Set the label
                GameObject labelObject = Instantiate(LabelPrefab, dataPoly.transform, false);
                labelObject.transform.Translate(dataPoly.transform.TransformVector(Vector3.up) * symbology["point"].Transform.Scale.magnitude, Space.Self);
                Text labelText = labelObject.GetComponentInChildren<Text>();
                labelText.text = (string)feature.Get(symbology["body"].Label);
            }


            List<Dataline> polygon = new List<Dataline>();
            // Darw the LinearRing
            foreach (LineString LinearRing in LinearRings) {
                Vector3[] lr = LinearRing.Vector3();
                GameObject dataLine = Instantiate(LinePrefab, dataPoly.transform, false);
                Dataline com = dataLine.GetComponent<Dataline>();
                com.Draw(lr, true, symbology, LinePrefab, HandlePrefab, null, mainMat, selectedMat, lineMain, lineSelected);
                polygon.Add(com);
            }

            //Draw the Polygon
            p.Draw(polygon, bodyMain);

            return p;
        }

        protected override void _checkpoint()
        {
        }
        protected override Task _save()
        {
            Datapolygon[] dataFeatures = gameObject.GetComponentsInChildren<Datapolygon>();
            foreach (Datapolygon dataFeature in dataFeatures)
            {
                Feature feature = dataFeature.feature;
                Geometry geom = new Geometry(wkbGeometryType.wkbPolygon);
                Geometry lr = new Geometry(wkbGeometryType.wkbLinearRing);
                geom.AssignSpatialReference(AppState.instance.mapProj);
                Dataline perimeter = dataFeature.GetComponentInChildren<Dataline>();
                lr.Vector3(dataFeature.GetVertexPositions());
                lr.CloseRings();
                geom.AddGeometryDirectly(lr);
                geom.TransformTo(geoJsonReader.CRS);
                feature.SetGeometryDirectly(geom);
                features.SetFeature(feature);
                Dataline[] polygon = dataFeature.GetComponentsInChildren<Dataline>();
                List<LineString> LinearRings = new List<LineString>();
                foreach (Dataline perimeter in polygon) {
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
                    LinearRings.Add(line);
                }
                Dictionary<string, object> properties = dataFeature.gisProperties as Dictionary<string, object> ?? new Dictionary<string, object>();
                thisFeatures.Add(new Feature(new Polygon(LinearRings), properties, dataFeature.gisId));
            };
            features.SyncToDisk();
            return Task.CompletedTask;
        }


        public override GameObject GetFeatureShape()
        {
            GameObject fs = Instantiate(HandlePrefab);
            Datapoint dp = fs.GetComponent<Datapoint>();
            dp.SetMaterial(mainMat, selectedMat);
            return fs;
        }

        public override void Translate(MoveArgs args)
        {
            changed = true;
        }

        public override void MoveAxis(MoveArgs args)
        {
            changed = true;
        }
    }
}
