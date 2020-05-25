// copyright Runette Software Ltd, 2020. All rights reserved
using GeoJSON.Net.Geometry;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using Project;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;


namespace Virgis {


    /// <summary>
    /// This script initialises the project and loads the Project and Layer data.
    /// 
    /// It is run once at Startup
    /// </summary>
    public class MapInitialize : Layer<RecordSet, FeatureObject> {
        // Refernce to the Main Camera GameObject
        public GameObject MainCamera;

        //References to the Prefabs to be used for Layers
        public GameObject PointLayer;
        public GameObject LineLayer;
        public GameObject PolygonLayer;
        public GameObject PointCloud;
        public GameObject MeshLayer;
        public GameObject CsvLayer;
        public AppState appState;

        // Path to the Project File
        public string inputfile;

        //File reader for Project and GeoJSON file
        private GeoJsonReader geoJsonReader;


        ///<summary>
        ///Instantiates all singletons.
        /// </summary>
        void Awake() {
            print("Map awakens");
            if (AppState.instance == null) {
                print("instantiate app state");
                appState = Instantiate(appState);
            }
            appState.AddStartEditSessionListener(_onStartEditSession);
            appState.AddEndEditSessionListener(_onExitEditSession);
        }

        /// 
        /// This is the initialisation script.
        /// 
        /// It loads the Project file, reads it for the layers and calls Draw to render each layer
        /// </summary>
        async void Start() {
            // Fetch Project definition - return if the file cannot be read - this will lead to an empty world
            geoJsonReader = new GeoJsonReader();
            await geoJsonReader.Load(inputfile);
            if (geoJsonReader.payload is null)
                return;
            appState.project = geoJsonReader.GetProject();

            //initialize space
            Vector2d origin = appState.project.Origin.Coordinates.Vector2d();
            GameObject Map = gameObject;
            AbstractMap _map = Map.GetComponent<AbstractMap>();
            _map.UseWorldScale();
            _map.Initialize(origin, appState.project.MapScale);


            //set globals
            appState.abstractMap = _map;
            appState.map = Map;
            appState.ZoomChange(appState.project.Scale);
            appState.mainCamera = MainCamera;
            MainCamera.transform.position = appState.project.Camera.Coordinates.Vector3();

            await Init(null);
            Draw();
        }

        async new Task<Layer<RecordSet, FeatureObject>> Init(RecordSet layer) {
            Component temp = null;
            foreach (RecordSet thisLayer in appState.project.RecordSets) {
                switch (thisLayer.DataType) {
                    case RecordSetDataType.Point:
                        temp = await Instantiate(PointLayer, Vector3.zero, Quaternion.identity).GetComponent<PointLayer>().Init(thisLayer as GeographyCollection);
                        break;
                    case RecordSetDataType.Line:
                        temp = await Instantiate(LineLayer, Vector3.zero, Quaternion.identity).GetComponent<LineLayer>().Init(thisLayer as GeographyCollection);
                        break;
                    case RecordSetDataType.Polygon:
                        temp = await Instantiate(PolygonLayer, Vector3.zero, Quaternion.identity).GetComponent<PolygonLayer>().Init(thisLayer as GeographyCollection);
                        break;
                    case RecordSetDataType.PointCloud:
                        temp = await Instantiate(PointCloud, Vector3.zero, Quaternion.identity).GetComponent<PointCloudLayer>().Init(thisLayer as GeographyCollection);
                        break;
                    case RecordSetDataType.Mesh:
                        temp = await Instantiate(MeshLayer, Vector3.zero, Quaternion.identity).GetComponent<MeshLayer>().Init(thisLayer as GeographyCollection);
                        break;
                    case RecordSetDataType.CSV:
                        temp = await Instantiate(CsvLayer, Vector3.zero, Quaternion.identity).GetComponent<DataPlotter>().Init(thisLayer as RecordSet);
                        break;
                }
                Debug.Log("Loaded : " + thisLayer.ToString() + " : " + thisLayer.Id);
                temp.transform.parent = transform;
                appState.addLayer(temp);
            }
            appState.Init();
            return this;
        }

        protected override Task _init(RecordSet layer) {
            throw new System.NotImplementedException();
        }

        public new void Add(MoveArgs args) {
            throw new System.NotImplementedException();
        }

        protected override VirgisComponent _addFeature(Vector3 position) {
            throw new System.NotImplementedException();
        }

        new void Draw() {
            foreach (ILayer layer in appState.layers) {
                layer.Draw();
            }
        }

        protected override void _draw() {
            throw new System.NotImplementedException();
        }



        public override void ExitEditSession(bool saved) {
            if (saved) {
                Save();
            } else {
                Draw();
            }
        }

        protected override void _checkpoint() {
        }

        public new void Save() {
            foreach (ILayer com in appState.layers) {
                RecordSet layer = com.Save();
                int index = appState.project.RecordSets.FindIndex(x => x.Id == layer.Id);
                appState.project.RecordSets[index] = layer;
            }
            appState.project.Scale = appState.GetScale();
            appState.project.Cameras =  new List<Point>() { MainCamera.transform.position.ToPoint() };
            geoJsonReader.SetProject(appState.project);
            geoJsonReader.Save();
        }

        protected override void _save() {
            throw new System.NotImplementedException();
        }

        public override void Translate(MoveArgs args) {

        }

        public override void MoveAxis(MoveArgs args) {

        }

        public override void StartEditSession() {
            CheckPoint();
        }

        protected void _onStartEditSession() {
            BroadcastMessage("StartEditSession", SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>
        /// Called when an edit session ends
        /// </summary>
        /// <param name="saved">true if stop and save, false if stop and discard</param>
        protected void _onExitEditSession(bool saved) {
            BroadcastMessage("ExitEditSession", saved, SendMessageOptions.DontRequireReceiver);
        }

        public override GameObject GetFeatureShape() {
            return null;
        }

    }
}
