// copyright Runette Software Ltd, 2020. All rights reserved
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Project;
using System;
using OSGeo.OGR;

namespace Virgis
{


    public class GeoJsonReader
    {
        private Layer _payload;
        public string fileName;
        private DataSource _datasource;

        public Layer getFeatureCollection()
        {
            return _payload;
        }

        public async Task Load(string file)
        {
            fileName = file;
            try
            {
                _datasource = Ogr.Open(fileName, 1);
                if (_datasource == null)
                    throw (new FileNotFoundException());
                _payload = _datasource.GetLayerByIndex(0);
                string crsWkt;
                _payload.GetSpatialRef().ExportToPrettyWkt(out crsWkt, 0);
                Debug.Log(crsWkt);
                if (_payload == null)
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



        public async Task Save()
        {
            
        }

        public void SetFeatureCollection(Layer contents)
        {
           
        }

    }
}
