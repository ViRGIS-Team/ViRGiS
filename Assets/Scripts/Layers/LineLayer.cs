// copyright Runette Software Ltd, 2020. All rights reserved

using g3;
using OSGeo.OGR;
using OSGeo.OSR;
using Project;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Virgis
{

    /// <summary>
    /// The parent entity for a instance of a Line Layer - that holds one MultiLineString FeatureCollection
    /// </summary>
    public class LineLayer : VirgisLayer<GeographyCollection, Layer>
    {
        // The prefab for the data points to be instantiated
        public GameObject CylinderLinePrefab; // Prefab to be used for cylindrical lines
        public GameObject CuboidLinePrefab; // prefab to be used for cuboid lines
        public GameObject SpherePrefab; // prefab to be used for Vertex handles
        public GameObject CubePrefab; // prefab to be used for Vertex handles
        public GameObject CylinderPrefab; // prefab to be used for Vertex handle
        public GameObject LabelPrefab;
        public Material PointBaseMaterial;
        public Material LineBaseMaterial;

        // used to read the GeoJSON file for this layer
        private GeoJsonReader geoJsonReader;

        private GameObject HandlePrefab;
        private GameObject LinePrefab;
        private Dictionary<string, Unit> symbology;
        private Material mainMat;
        private Material selectedMat;
        private Material lineMain;
        private Material lineSelected;

        //private List<GameObject> _tempGOs = new List<GameObject>();
        //private List<GameObject> _vertices = new List<GameObject>();

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
            mainMat = Instantiate(PointBaseMaterial);
            mainMat.SetColor("_BaseColor", col);
            selectedMat = Instantiate(PointBaseMaterial);
            selectedMat.SetColor("_BaseColor", sel);
            lineMain = Instantiate(LineBaseMaterial);
            lineMain.SetColor("_BaseColor", line);
            lineSelected = Instantiate(LineBaseMaterial);
            lineSelected.SetColor("_BaseColor", lineSel);
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry)
        {
            return _drawFeature(new DCurve3().Vector3(geometry, false));
        }

        protected override void _draw()
        {
            long FeatureCount = features.GetFeatureCount(1);
            for (int i = 0; i < FeatureCount; i++) {
                Feature feature = features.GetFeature(i);
                Geometry line = feature.GetGeometryRef();
                if (line.GetGeometryType() == wkbGeometryType.wkbLineString ||
                    line.GetGeometryType() == wkbGeometryType.wkbLineString25D ||
                    line.GetGeometryType() == wkbGeometryType.wkbLineStringM ||
                    line.GetGeometryType() == wkbGeometryType.wkbLineStringZM
                ) {
                    _drawFeature(new DCurve3().FromGeometry(line));
                } else if 
                    (line.GetGeometryType() == wkbGeometryType.wkbMultiLineString ||
                    line.GetGeometryType() == wkbGeometryType.wkbMultiLineString25D ||
                    line.GetGeometryType() == wkbGeometryType.wkbMultiLineStringM ||
                    line.GetGeometryType() == wkbGeometryType.wkbMultiLineStringZM
                 ) {
                    int n = line.GetGeometryCount();
                    for (int j = 0; j < n; j++) {
                        Geometry Line2 = line.GetGeometryRef(j);
                        string Type = Line2.GetGeometryType().ToString();
                        _drawFeature(new DCurve3().FromGeometry(Line2));
                    }
                }
            }
        }


        /// <summary>
        /// Draws a single feature based on world scale coordinates
        /// </summary>
        /// <param name="line"> Vector3[] coordinates</param>
        /// <param name="feature">Featire (optinal)</param>
        protected VirgisFeature _drawFeature(DCurve3 line, Feature feature = null)
        {
            GameObject dataLine = Instantiate(LinePrefab, transform, false);

            //set the gisProject properties
            Dataline com = dataLine.GetComponent<Dataline>();
            if (feature != null)
                com.feature = feature;

            //Draw the line
            com.Draw(line, symbology, LinePrefab, HandlePrefab, LabelPrefab, mainMat, selectedMat, lineMain, lineSelected);

            return com;
        }

        protected override void _checkpoint()
        {
        }

        protected override Task _save()
        {
            //Dataline[] dataFeatures = gameObject.GetComponentsInChildren<Dataline>();
            //List<Feature> thisFeatures = new List<Feature>();
            //foreach (Dataline dataFeature in dataFeatures)
            //{
            //    Vector3[] vertices = dataFeature.GetVertexPositions();
            //    List<Position> positions = new List<Position>();
            //    foreach (Vector3 vertex in vertices)
            //    {
            //        positions.Add(vertex.ToPosition() as Position);
            //    }
            //    List<LineString> lines = new List<LineString>();
            //    lines.Add(new LineString(positions));
            //    thisFeatures.Add(new Feature(new MultiLineString(lines), dataFeature.gisProperties, dataFeature.gisId));
            //};
            //FeatureCollection FC = new FeatureCollection(thisFeatures);
            //geoJsonReader.SetFeatureCollection(FC);
            //geoJsonReader.Save();
            //features = FC;
            return Task.CompletedTask;
        }

        public override GameObject GetFeatureShape()
        {
            GameObject fs = Instantiate(HandlePrefab);
            Datapoint com = fs.GetComponent<Datapoint>();
            com.SetMaterial(mainMat, selectedMat);
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

        /* public override VirgisComponent GetClosest(Vector3 coords)
         {
             throw new System.NotImplementedException();
         }*/

    }
}
