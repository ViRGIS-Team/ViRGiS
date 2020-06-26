// copyright Runette Software Ltd, 2020. All rights reserved
using GeoJSON.Net.Geometry;
using Mapbox.Unity.Map;
using Project;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System;


namespace Virgis
{


    /// <summary>
    /// This script initialises the project and loads the Project and Layer data.
    /// 
    /// It is run once at Startup
    /// </summary>
    public class MapInitialize : VirgisLayer
    {
        // Refernce to the Main Camera GameObject
        public GameObject MainCamera;
        public AbstractMap MapBoxLayer;

        //References to the Prefabs to be used for Layers
        public GameObject PointLayer;
        public GameObject LineLayer;
        public GameObject PolygonLayer;
        public GameObject PointCloud;
        public GameObject MeshLayer;
        public GameObject XsectLayer;
        public GameObject CsvLayer;
        public AppState appState;

        // Path to the Project File
        public string inputfile;

        //File reader for Project and GeoJSON file
        private ProjectJsonReader projectJsonReader;


        ///<summary>
        ///Instantiates all singletons.
        /// </summary>
        void Awake()
        {
            print("Map awakens");
            if (AppState.instance == null)
            {
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
        async void Start()
        {
            // Fetch Project definition - return if the file cannot be read - this will lead to an empty world
            projectJsonReader = new ProjectJsonReader();
            await projectJsonReader.Load(inputfile);
            if (projectJsonReader.payload is null)
                return;
            appState.project = projectJsonReader.GetProject();

            //set globals
            appState.initProj();
            appState.map = gameObject;
            appState.ZoomChange(appState.project.Scale);
            appState.mainCamera = MainCamera;
            MainCamera.transform.position = appState.project.Cameras[0].ToVector3();

            await Init(null);
        }

        async new Task<VirgisLayer> Init(RecordSet notImportant)
        {
            VirgisLayer temp = null;
            foreach (RecordSet thisLayer in appState.project.RecordSets)
            {
                try {
                    switch (thisLayer.DataType) {
                        case RecordSetDataType.MapBox:
                            MapBoxLayer.UseWorldScale();
                            MapBoxLayer.Initialize(appState.project.Origin.Coordinates.Vector2d(), Convert.ToInt32(thisLayer.Properties["mapscale"]));
                            appState.abstractMap = MapBoxLayer;
                            temp = MapBoxLayer.GetComponent<ContainerLayer>();
                            temp.SetMetadata(thisLayer);
                            temp.changed = false;
                            break;
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
                        case RecordSetDataType.XSect:
                            temp = await Instantiate(XsectLayer, Vector3.zero, Quaternion.identity).GetComponent<XsectLayer>().Init(thisLayer as GeologyCollection);
                            break;
                        case RecordSetDataType.CSV:
                            temp = await Instantiate(CsvLayer, Vector3.zero, Quaternion.identity).GetComponent<DataPlotter>().Init(thisLayer as RecordSet);
                            break;
                        default:
                            Debug.LogError(thisLayer.Type.ToString() + " is not known.");
                            break;
                    }
                    temp.transform.parent = transform;
                    appState.addLayer(temp);
                    temp.gameObject.SetActive(thisLayer.Visible);
                    temp.Draw();
                } catch (Exception e) {
                    Debug.LogError(e.ToString() ?? "Unknown Error");
                }
            }
            appState.Init();
            return this;
        }

        public new void Add(MoveArgs args)
        {
            throw new System.NotImplementedException();
        }

        protected override VirgisFeature _addFeature(Vector3[] geometry)
        {
            throw new System.NotImplementedException();
        }

        new void Draw()
        {
            foreach (IVirgisLayer layer in appState.layers)
            {
                layer.Draw();
            }
        }

        protected override void _draw()
        {
            throw new System.NotImplementedException();
        }

        public override async void ExitEditSession(bool saved) {
            if (!saved) {
                Draw();
            }
            await Save();
        }

        protected override void _checkpoint()
        {
        }

        public new async Task<RecordSet> Save()
        {
            // TODO: wrap this in try/catch block
            Debug.Log("MapInitialize.Save starts");
            foreach (IVirgisLayer com in appState.layers)
            {
                RecordSet alayer = await com.Save();
                int index = appState.project.RecordSets.FindIndex(x => x.Id == alayer.Id);
                appState.project.RecordSets[index] = alayer;
            }
            appState.project.Scale = appState.GetScale();
            appState.project.Cameras = new List<Point>() { MainCamera.transform.position.ToPoint() };
            projectJsonReader.SetProject(appState.project);
            await projectJsonReader.Save();
                        // TODO: should return the root layer in v2
            return null;
        }

        protected override Task _save()
        {
            throw new System.NotImplementedException();
        }

        public override void Translate(MoveArgs args)
        {

        }

        public override void MoveAxis(MoveArgs args)
        {

        }

        public override void StartEditSession()
        {
            CheckPoint();
        }

        protected void _onStartEditSession()
        {
            BroadcastMessage("StartEditSession", SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>
        /// Called when an edit session ends
        /// </summary>
        /// <param name="saved">true if stop and save, false if stop and discard</param>
        protected void _onExitEditSession(bool saved)
        {
            BroadcastMessage("ExitEditSession", saved, SendMessageOptions.DontRequireReceiver);
        }

        public override GameObject GetFeatureShape()
        {
            return null;
        }

    }
}
