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
    public class AppState : MonoBehaviour {
        public static AppState instance = null;

        private EditSession _editSession;
        private List<VirgisLayer> _layers;
        private SpatialReference _crs;
        private CoordinateTransformation _trans;
        private IDisposable initsub;
        public Vector3 lastHitPosition;
        public SpatialReference projectCrs;
        public List<Coroutine> tasks;
        public int editScale;
        public int currentView;
        public bool guiActive {
            get {
                return lhguiActive || rhguiActive;
            }
        }
        public bool lhguiActive = false;
        public bool rhguiActive = false;
        public OrientEvent Orientation {
            get;
            private set;
        }

        public InfoEvent Info {
            get;
            private set;
        }

        public ZoomEvent Zoom {
            get;
            private set;
        }

        public ButtonStatus ButtonStatus{
            get;
            private set;
        }

        /// <summary>
        /// Use this to get the project change event
        /// </summary>
        public ProjectChange Project {
            get;
            private set;
        }

        /// <summary>
        /// Event that is triggered when a layer is added
        /// </summary>
        public LayerChange LayerUpdate {
            get;
            private set;
        }

        /// <summary>
        /// UniRx Subject that is triggered when a new configuration is loaded.
        /// </summary>
        public Subject<bool> ConfigEvent = new Subject<bool>();

        protected void Start() {
            
        }

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
            _layers = new List<VirgisLayer>();
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
        /// Init is called after a project has been fully loaded.
        /// </summary>
        /// 
        /// Call this method everytime a new project has been loaded,
        /// e.g. New Project, Open Project
        public virtual void Init() { }

        public EditSession editSession {
            get => _editSession;
        }


        public GameObject map {
            get; set;
        }

        /// <summary>
        /// Use this to change or get the project
        /// </summary>
        public GisProject project {
            get {
                return Project.Get();
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
            op.SetBallparkAllowed(false);
            return new CoordinateTransformation(sr, mapProj, op);
        }

        public CoordinateTransformation projectOutTransformer(SpatialReference sr) {
            CoordinateTransformationOptions op = new CoordinateTransformationOptions();
            op.SetBallparkAllowed(false);
            return new CoordinateTransformation(mapProj, sr, op);
        }

        public List<VirgisLayer> layers {
            get => _layers;
        }

        public void addLayer(VirgisLayer layer) {
            _layers.Add(layer);
            if (_layers.Count == 1)
                _editSession.editableLayer = (IVirgisLayer) _layers[0];
            if (_layers.Count == 2 && _layers[0].GetMetadata().DataType == RecordSetDataType.MapBox)
                _editSession.editableLayer = (IVirgisLayer) _layers[1];
            LayerUpdate.AddLayer(layer);
        }

        public void clearLayers() {
            _layers.Clear();
        }

        public Camera mainCamera {
            get; set;
        }

        public Transform trackingSpace {
            get; set;
        }

        public bool InEditSession() {
            return _editSession.IsActive();
        }

        public void StartEditSession() {
            _editSession.Start();
            editScale = 5;
        }

        public void StopSaveEditSession() {
            _editSession.StopAndSave();
        }

        public void StopDiscardEditSession() {
            _editSession.StopAndDiscard();
        }

        /// <summary>
        /// Courtesy function to allow the creation of logic to set configuration items
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public virtual void SetConfig(string key, object value) {
            //Do Nothing
        }

        /// <summary>
        /// Courtesy Function to allow the retrieval of Configuration items
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual object GetConfig(string key) {
            return default;
        }
    }
}