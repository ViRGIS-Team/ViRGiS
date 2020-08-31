using Mapbox.Unity.Map;
using OSGeo.OSR;
using Project;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

namespace Virgis {

    // AppState is a global singleton object that stores
    // app states, such as EditSession, etc.
    //
    // Singleton pattern taken from https://learn.unity.com/tutorial/level-generation
    public class AppState : MonoBehaviour {
        public static AppState instance = null;

        private EditSession _editSession;
        private List<Component> _layers;
        private ZoomEvent _zoomChange;
        private SpatialReference _crs;
        private CoordinateTransformation _trans;

        void Awake() {
            print("AppState awakens");
            if (instance == null) {
                print("AppState instance assigned");
                instance = this;
            } else if (instance != this) {
                // there cannot be another instance
                print("AppState found another instance");
                Destroy(gameObject);
            }

            DontDestroyOnLoad(gameObject);
            _editSession = new EditSession();
            _layers = new List<Component>();
            _zoomChange = new ZoomEvent();

            try {
                GdalConfiguration.ConfigureOgr();
                GdalConfiguration.ConfigureGdal();
                GdalConfiguration.ConfiurePdal();
                GdalConfiguration.ConfigureMdal();
            } catch (Exception e) {
                Debug.LogError(e.ToString());
            }
        }

        /// <summary>
        /// Init is called after a project has been fully loaded.
        /// </summary>
        /// 
        /// Call this method everytime a new project has been loaded,
        /// e.g. New Project, Open Project
        public void Init() {
            IVirgisLayer firstLayer = (IVirgisLayer) _layers[0];
            firstLayer.SetEditable(true);
            _editSession.editableLayer = firstLayer;
        }

        public EditSession editSession {
            get => _editSession;
        }

        public AbstractMap abstractMap {
            get; set;
        }

        public GameObject map {
            get; set;
        }

        public GisProject project {
            get; set;
        }

        public  SpatialReference mapProj {
            get => _crs;
        }

        public CoordinateTransformation mapTrans {
            get => _trans;
        }

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
                //_crs.ImportFromProj4($"+proj=tmerc +datum=WGS84 +units=m +axis=enu +no-defs +lat_0={project.Origin.Coordinates.Latitude} +lon_0={project.Origin.Coordinates.Longitude}");
            }
            CoordinateTransformationOptions op = new CoordinateTransformationOptions();
            op.SetOperation("+proj=axisswap +order=1,3,2");
            _trans = new CoordinateTransformation(_crs, _crs, op);
            if (_trans == null)
                throw new NotSupportedException("transformation failed");
        }

        public List<Component> layers {
            get => _layers;
        }

        public void addLayer(Component layer) {
            _layers.Add(layer);
        }

        public void clearLayers() {
            _layers.Clear();
        }

        public Camera mainCamera {
            get; set;
        }

        public GameObject trackingSpace {
            get; set;
        }

        public bool InEditSession() {
            return _editSession.IsActive();
        }

        public void StartEditSession() {
            _editSession.Start();
        }

        public void StopSaveEditSession() {
            _editSession.StopAndSave();
        }

        public void StopDiscardEditSession() {
            _editSession.StopAndDiscard();
        }

        public void AddStartEditSessionListener(UnityAction action) {
            _editSession.AddStartEditSessionListener(action);
        }

        public void AddEndEditSessionListener(UnityAction<bool> action) {
            _editSession.AddEndEditSessionListener(action);
        }

        public void AddZoomChangeListerner(UnityAction<float> action) {
            _zoomChange.AddZoomChangeListerner(action);
        }

        public void ZoomChange(float zoom) {
            _zoomChange.Change(zoom);
        }

        public float GetScale() {
            return _zoomChange.GetScale();
        }

    }
}