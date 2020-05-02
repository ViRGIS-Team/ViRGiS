
// copyright Runette Software Ltd, 2020. All rights reserved﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Mapbox.Unity.Utilities;
using Mapbox.Unity.Map;
using Mapbox.Utils;

public class DataPlotter : MonoBehaviour
{
    // Name of the input file, no extension
 public string inputfile;
 // Indices for columns to be assigned
 public int columnX = 0;
 public int columnY = 1;
 public int columnZ = 2;

 public int columnW = 6;

 public int columnR = 3;
 public int columnG = 4;
 public int columnB = 5;

 // Full column names
 public string xName;
 public string yName;
 public string zName;
 public string rName;
 public string gName;
 public string bName;
 public string wName;

 // The prefab for the data points to be instantiated
 public GameObject PointPrefab;

[SerializeField]
 private AbstractMap _map;

 public float startAltitude = 50;


 // List for holding data from CSV reader
 private List<Dictionary<string, object>> pointList;

 // Use this for initialization
 void Start () {

Vector2d origin = _map.CenterLatitudeLongitude;
float originElevation = _map.QueryElevationInMetersAt(origin);
GameObject camera = GameObject.Find("Main Camera");
camera.transform.position = new Vector3(0, (originElevation + startAltitude)*_map.WorldRelativeScale,0);


 // Set pointlist to results of function Reader with argument inputfile
 pointList = CSVReader.Read(inputfile);

 // Declare list of strings, fill with keys (column names)
 List<string> columnList = new List<string>(pointList[1].Keys);

 // Assign column name from columnList to Name variables
 xName = columnList[columnX];
 yName = columnList[columnY];
 zName = columnList[columnZ];
 rName = columnList[columnR];
 gName = columnList[columnG];
 bName = columnList[columnB];
 wName = columnList[columnW];
//Loop through Pointlist

 for (var i = 0; i < pointList.Count; i++)
 {
 // Get value in poinList at ith "row", in "column" Name
 float x = Convert.ToSingle(pointList[i][xName]);
 float y = Convert.ToSingle(pointList[i][yName]);
 float z = Convert.ToSingle(pointList[i][zName]);
 float r = Convert.ToSingle(pointList[i][rName])/255f;
 float g = Convert.ToSingle(pointList[i][gName])/255f;
 float b = Convert.ToSingle(pointList[i][bName])/255f;
 float w = Convert.ToSingle(pointList[i][wName]);

 //instantiate the prefab with coordinates defined above
 GameObject dataPoint = Instantiate(PointPrefab, new Vector3(0, 0, 0), Quaternion.identity);
 dataPoint.transform.parent = gameObject.transform;

 //Set the color
 dataPoint.GetComponent<Renderer>().material.color =
 new Color(r,g,b, 1.0f);
 Vector3 scaleChange = new Vector3(w, w, w);
 dataPoint.transform.localScale = scaleChange;

 Vector2d _location = Conversions.StringToLatLon(x + "," + z);
 dataPoint.transform.localPosition = _map.GeoToWorldPosition(_location, true) + new Vector3(0, y*_map.WorldRelativeScale, 0);


 }
 }
}
