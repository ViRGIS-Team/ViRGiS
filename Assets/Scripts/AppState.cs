using Mapbox.Unity.Map;
using Project;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Virgis {

    // AppState is a global singleton object that stores
    // app states, such as EditSession, etc.
    //
    // Singleton pattern taken from https://learn.unity.com/tutorial/level-generation
    public class AppState : MonoBehaviour {
        public static AppState instance = null;

        private EditSession _editSession;
        private List<Component> _layers;
        private ILayer _editableLayer;

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
        }

        /// <summary>
        /// Init is called after a project has been fully loaded.
        /// </summary>
        public void Init() {
            ILayer firstLayer = (ILayer) _layers[0];
            firstLayer.SetEditable(true);
            _editableLayer = firstLayer;
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

        public List<Component> layers {
            get => _layers;
        }

        public void addLayer(Component layer) {
            _layers.Add(layer);
        }

        public void clearLayers() {
            _layers.Clear();
        }

        public ILayer editableLayer {
            get => _editableLayer;
            set {
                value?.SetEditable(true);
                _editableLayer?.SetEditable(false);
                _editableLayer = value;
            }
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

    }
}