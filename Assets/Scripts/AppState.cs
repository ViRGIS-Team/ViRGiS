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

        public void initProj() {
            if (project != null) {
                try {
                    GdalConfiguration.ConfigureOgr();
                    GdalConfiguration.ConfigureGdal();
                } catch (Exception e) {
                    Debug.LogError(e.ToString());
                }
                _crs = new SpatialReference(null);
                _crs.ImportFromProj4($"+proj=tmerc +datum=WGS84 +units=m +axis=enu +no-defs +lat_0={project.Origin.Coordinates.Latitude} +lon_0={project.Origin.Coordinates.Longitude}");
            }
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

        public GameObject mainCamera {
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