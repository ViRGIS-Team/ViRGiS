
// copyright Runette Software Ltd, 2020. All rights reserved﻿using System.Collections;
using UnityEngine;
using System;
using Project;
using System.Threading.Tasks;
using System.Collections.Generic;
using GeoJSON.Net.Geometry;
using UnityEngine.UI;

namespace Virgis {

    public class DataPlotter : Layer<RecordSet, CSVData> {

        // The prefab for the data points to be instantiated
        public GameObject SpherePrefab;
        public GameObject CubePrefab;
        public GameObject CylinderPrefab;
        public GameObject LabelPrefab;


        private GameObject PointPrefab;
        private CSVReader csvReader;



        protected override async Task _init(RecordSet layer) {
            // Set pointlist to results of function Reader with argument inputfile
            csvReader = new CSVReader();
            await csvReader.Load(layer.Source);
            features = csvReader.Read();
        }

        protected override void _checkpoint() {
           
        }

        protected override void _draw() {

            Dictionary<string, Unit> symbology = new Dictionary<string, Unit>();
            float displacement = 1.0f;
            if (layer.Properties.Contains("Units")) {
                symbology = layer.Properties["Units"] as Dictionary<string, Unit>;
                if (symbology.ContainsKey("point") && symbology["point"].ContainsKey("Shape")) {
                    Shapes shape = symbology["point"].Shape;
                    switch (shape) {
                        case Shapes.Spheroid:
                            PointPrefab = SpherePrefab;
                            break;
                        case Shapes.Cuboid:
                            PointPrefab = CubePrefab;
                            break;
                        case Shapes.Cylinder:
                            PointPrefab = CylinderPrefab;
                            displacement = 1.5f;
                            break;
                        default:
                            PointPrefab = SpherePrefab;
                            break;
                    }
                } else {
                    PointPrefab = SpherePrefab;
                }
            }
            if (layer.Properties.Contains("lat") && features[0].ContainsKey(layer.Properties["lat"] as string) 
                && layer.Properties.Contains("lon") && features[0].ContainsKey(layer.Properties["lon"] as string)) {
                foreach (CSVRow feature in features) {
                    Position position = new Position((double)feature[layer.Properties["lat"] as string], (double) feature[layer.Properties["lon"] as string]);


                    //instantiate the prefab with coordinates defined above
                    GameObject dataPoint = Instantiate(PointPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                    dataPoint.transform.parent = gameObject.transform;

                    // add the gis data from geoJSON
                    Datapoint com = dataPoint.GetComponent<Datapoint>();
                    com.gisProperties = feature;

                    //Set the symbology
                    if (symbology.ContainsKey("point")) {
                        dataPoint.SendMessage("SetColor", (Color) symbology["point"].Color);
                        dataPoint.transform.localScale = symbology["point"].Transform.Scale;
                        dataPoint.transform.localRotation = symbology["point"].Transform.Rotate;
                        dataPoint.transform.localPosition = symbology["point"].Transform.Position;
                        dataPoint.transform.position = position.Vector3();
                    }

                    //Set the label
                    GameObject labelObject = Instantiate(LabelPrefab, Vector3.zero, Quaternion.identity);
                    labelObject.transform.parent = dataPoint.transform;
                    labelObject.transform.localPosition = Vector3.up * displacement;
                    Text labelText = labelObject.GetComponentInChildren<Text>();

                    if (symbology.ContainsKey("point") && symbology["point"].ContainsKey("Label") && symbology["point"].Label != null && feature.ContainsKey(symbology["point"].Label)) {
                        labelText.text = (string) feature[symbology["point"].Label];
                    }
                }
            }
        }

        protected override VirgisComponent _addFeature(Vector3[] geometry) {
            throw new NotImplementedException();
        }

        protected override void _save() {
            throw new NotImplementedException();
        }

        public override void MoveAxis(MoveArgs args) {
            
        }

        public override void Translate(MoveArgs args) {
            
        }

        public override GameObject GetFeatureShape() {
            return PointPrefab;
        }

    }
}
