/* MIT License

Copyright (c) 2020 - 21 Runette Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (and subsidiary notices) shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */

using OSGeo.OSR;
using Gdal = OSGeo.GDAL.Gdal;
using Project;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UniRx;
using OSGeo;
using Mdal;
using Pdal;


namespace Virgis {

    // AppState is a global singleton object that stores
    // app states, such as EditSession, etc.
    //
    // Singleton pattern taken from https://learn.unity.com/tutorial/level-generation
    public class AppState : State {

        public new static AppState instance {
            get {
                return State.instance as AppState;
            }
            private set {
                State.instance = value;
            }
        }

        public GameObject server;

        private SpatialReference _crs;
        private CoordinateTransformation _trans;
        private IDisposable initsub;

        public SpatialReference projectCrs;

        protected void Awake() {
            Debug.Log("AppState awakens");
            if (instance == null) {
                Debug.Log("AppState instance assigned");
                instance = this;
            } else if (instance != this) {
                // there cannot be another instance
                Debug.Log("AppState found another instance");
                Destroy(gameObject);
            }
            DontDestroyOnLoad(gameObject);
            _editSession = new EditSession();
            _layers = new List<IVirgisLayer>();
            Zoom = new ZoomEvent();
            Project = new ProjectChange();
            Info = new InfoEvent();
            ButtonStatus = new ButtonStatus();
            Orientation = new OrientEvent();
            LayerUpdate = new LayerChange();

            initsub = Project.Event.Subscribe(proj => Init());
            try {
                GdalConfiguration.ConfigureOgr();
            } catch (Exception e) {
                Debug.LogError(e.ToString());
            };
            try {
                GdalConfiguration.ConfigureGdal();
            } catch (Exception e) {
                Debug.LogError(e.ToString());
            };
            try {
                PdalConfiguration.ConfigurePdal();
            } catch (Exception e) {
                Debug.LogError(e.ToString());
            };
            try {
                Debug.Log($" MDAL Version : {MdalConfiguration.ConfigureMdal()}");
            } catch (Exception e) {
                Debug.LogError(e.ToString());
            }
            Gdal.SetConfigOption("CURL_CA_BUNDLE", Path.Combine(Application.streamingAssetsPath, "gdal", "cacert.pem"));
        }

        private void OnDestroy() {
            initsub.Dispose();
        }

        /// <summary>
        /// Use this to change or get the project
        /// </summary>
        public new GisProject project {
            get {
                return Project.Get() as GisProject;
            } 
            set {
                Project.Set(value);
                initProj();
            }
        }

        public  SpatialReference mapProj {
            get => _crs;
        }

        public CoordinateTransformation  mapTrans {
            get => _trans;
        }

        /// <summary>
        /// Tasks to be run after a project is loaded
        /// </summary>
        public void initProj() {
            if (project != null) {
                _crs = new SpatialReference($@"PROJCRS[""virgis"",
                    BASEGEOGCRS[""WGS 84"",
                    DATUM[""World Geodetic System 1984"", ELLIPSOID[""WGS 84"", 6378137, 298.257223563, LENGTHUNIT[""metre"", 1]], ID[""EPSG"", 6326]], PRIMEM[""Greenwich"", 0, ANGLEUNIT[""degree"", 0.0174532925199433], ID[""EPSG"", 8901]]],
                    CONVERSION[
                        ""unknown"", METHOD[""Transverse Mercator"", ID[""EPSG"", 9807]],
                        PARAMETER
                        [""Latitude of natural origin"", {project.Origin.Coordinates.Latitude}, ANGLEUNIT[""degree"", 0.0174532925199433], ID[""EPSG"", 8801]],
                        PARAMETER
                        [""Longitude of natural origin"", {project.Origin.Coordinates.Longitude}, ANGLEUNIT[""degree"", 0.0174532925199433], ID[""EPSG"", 8802]],
                        PARAMETER[""Scale factor at natural origin"", 1, SCALEUNIT[""unity"", 1], ID[""EPSG"", 8805]],
                        PARAMETER[""False easting"", 0, LENGTHUNIT[""metre"", 1], ID[""EPSG"", 8806]],
                        PARAMETER[""False northing"", 0, LENGTHUNIT[""metre"", 1], ID[""EPSG"", 8807]]
                        ],
                        CS[Cartesian, 2],
                        AXIS[""(E)"", east, ORDER[1], LENGTHUNIT[""metre"", 1, ID[""EPSG"", 9001]]],
                        AXIS[""(N)"", north, ORDER[2], LENGTHUNIT[""metre"", 1, ID[""EPSG"", 9001]]]]");
                            }
            CoordinateTransformationOptions op = new CoordinateTransformationOptions();
            op.SetOperation("+proj=axisswap +order=1,3,2");
            _trans = new CoordinateTransformation(_crs, _crs, op);
            if (_trans == null)
                throw new NotSupportedException("transformation failed");
            projectCrs = new SpatialReference(null);
            if (project.projectCrs != null) {
                projectCrs.SetWellKnownGeogCS(project.projectCrs);
            } else {
                projectCrs.SetWellKnownGeogCS("EPSG:4979");
            }
            string wkt;
            projectCrs.ExportToWkt(out wkt, null);
            Debug.Log("Project Crs : " + wkt);
        }

        public CoordinateTransformation projectTransformer(SpatialReference sr) {
            CoordinateTransformationOptions op = new CoordinateTransformationOptions();
            op.SetBallparkAllowed(true);
            return new CoordinateTransformation(sr, mapProj, op);
        }

        public CoordinateTransformation projectOutTransformer(SpatialReference sr) {
            CoordinateTransformationOptions op = new CoordinateTransformationOptions();
            op.SetBallparkAllowed(false);
            return new CoordinateTransformation(mapProj, sr, op);
        }



        public override bool LoadProject(string path) {
            return server.GetComponent<IVirgisLayer>()?.Load(path) ?? false;
        }
    }
}