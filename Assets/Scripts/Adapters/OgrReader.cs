// copyright Runette Software Ltd, 2020. All rights reserved
using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using OSGeo.OGR;
using OSGeo.GDAL;

namespace Virgis
{


    public class OgrReader
    {
        private List<Layer> _layers = new List<Layer>();
        public string fileName;
        private DataSource _datasource;
        private int _update;

        public List<Layer> GetLayers()
        {
            return _layers;
        }


        public async Task Load(string file, int update) {
            fileName = file;
            _update = update;
            Load();
        }

        private void Load() {
            try
            {
                _datasource = Ogr.Open(fileName, _update);
                if (_datasource == null)
                    throw (new FileNotFoundException());
                for (int i = 0; i < _datasource.GetLayerCount(); i++) 
                _layers.Add(_datasource.GetLayerByIndex(i));
                if (_layers.Count == 0)
                    throw (new NotSupportedException());
            }
            catch (Exception e) 
            {
                Debug.LogError("Failed to Load" + fileName + " : " + e.ToString());
            }
        }

        public async Task LoadWfs(string url, int update) {
            fileName = "WFS:" + url;
            _update = update;
            Load();
        }

        public static void Flatten(ref wkbGeometryType type) {
            if (type != wkbGeometryType.wkbUnknown && type != wkbGeometryType.wkbNone) {
                Geometry geom = new Geometry(type);
                geom.FlattenTo2D();
                type = geom.GetGeometryType();
                switch (type) {
                    case wkbGeometryType.wkbMultiLineString:
                        type = wkbGeometryType.wkbLineString;
                        break;
                    case wkbGeometryType.wkbMultiPoint:
                        type = wkbGeometryType.wkbPoint;
                        break;
                    case wkbGeometryType.wkbMultiPolygon:
                        type = wkbGeometryType.wkbPolygon;
                        break;
                }
            }
            return;
        }
    }
}