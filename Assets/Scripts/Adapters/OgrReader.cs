// copyright Runette Software Ltd, 2020. All rights reserved
using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using OSGeo.OGR;
using OSGeo.OSR;

namespace Virgis
{


    public class OgrReader
    {
        private List<Layer> _layers = new List<Layer>();
        public string fileName;
        private DataSource _datasource;

        public List<Layer> GetLayers()
        {
            return _layers;
        }


        public async Task Load(string file)
        {
            fileName = file;
            try
            {
                _datasource = Ogr.Open(fileName, 1);
                if (_datasource == null)
                    throw (new FileNotFoundException());
                for (int i = 0; i < _datasource.GetLayerCount(); i++) 
                _layers.Add(_datasource.GetLayerByIndex(i));
                if (_layers.Count == 0)
                    throw (new NotSupportedException());
            }
            catch (Exception e) when (
                   e is UnauthorizedAccessException ||
                   e is DirectoryNotFoundException ||
                   e is FileNotFoundException ||
                   e is NotSupportedException
                   )
            {
                Debug.LogError("Failed to Load" + file + " : " + e.ToString());

            }
        }

        public static wkbGeometryType Flatten(Geometry geom) {
            geom.FlattenTo2D();
            wkbGeometryType type = geom.GetGeometryType();
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
            return type;
        }
    }
}