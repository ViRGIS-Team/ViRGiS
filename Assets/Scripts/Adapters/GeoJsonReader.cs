// copyright Runette Software Ltd, 2020. All rights reserved
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Project;
using System;
using OSGeo.OGR;
using OSGeo.OSR;

namespace Virgis
{


    public class GeoJsonReader
    {
        private Layer _payload;
        public string fileName;
        private DataSource _datasource;
        public SpatialReference CRS;

        public Layer getFeatureCollection()
        {
            return _payload;
        }

        public void setFeatureCollection(Layer data) {
            _payload = data;
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
                CRS = _payload.GetSpatialRef();
                CRS.ExportToPrettyWkt(out crsWkt, 0);
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



        public void Save()
        {
            Driver driver = Ogr.GetDriverByName("GeoJSON");
            if (driver is null) {
                Debug.LogError("Failed to save :Incorrect Driver Name");
                throw (new NotSupportedException());
            }
            DataSource ds = driver.CopyDataSource(_datasource, fileName, null);
            if (ds is null) {
                Debug.LogError("Failed to save :Creation failed");
                throw (new NotSupportedException());
            }
            string name = _datasource.GetLayerByIndex(0).GetName();
            ds.DeleteLayer(0);
            Layer layer = ds.CopyLayer(_payload,name , null );
            ds.SyncToDisk();
            ds.FlushCache();
        }

        public void SetFeatureCollection(Layer contents)
        {
           
        }

    }
}
