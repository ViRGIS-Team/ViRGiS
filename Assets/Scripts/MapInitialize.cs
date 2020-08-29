// copyright Runette Software Ltd, 2020. All rights reserved
using GeoJSON.Net.Geometry;
using Mapbox.Unity.Map;
using Project;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System;
using System.Linq;

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
        public GameObject MapBoxLayer;

        //References to the Prefabs to be used for Layers
        public GameObject VectorLayer;
        public GameObject RasterLayer;
        public GameObject PointCloud;
        public GameObject MeshLayer;
        public GameObject MDALLayer;
        public GameObject XsectLayer;
        public GameObject CsvLayer;
        public GameObject DemLayer;
        public AppState appState;


        //File reader for Project file
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

            //set globals
            appState.map = gameObject;
            appState.mainCamera = MainCamera;
        }

        /// 
        /// This is the initialisation script.
        /// 
        /// It loads the Project file, reads it for the layers and calls Draw to render each layer
        /// </summary>


        public async Task<bool> Load(string file) {
  
            // Get Project definition - return if the file cannot be read - this will lead to an empty world
            projectJsonReader = new ProjectJsonReader();
            try {
                projectJsonReader.Load(file);
            } catch (Exception e) {
                Debug.LogError($"Project File {file} is invalid :: " + e.ToString());
                return false;
            }

            if (projectJsonReader.payload is null) {
                Debug.LogError($"Project File {file} is empty");
                return false;
            }
            appState.project = projectJsonReader.GetProject();

            //set globals
            appState.initProj();
            appState.ZoomChange(appState.project.Scale);
            MainCamera.transform.position = appState.project.Cameras[0].ToVector3();

            try {
                foreach (RecordSet thisLayer in appState.project.RecordSets) {
                    await initLayer(thisLayer);
                };
            } catch {
                Debug.LogError($"Project File {file} failed");
                return false;
            }
            appState.Init();
            return true;
        }

        private async Task initLayer(RecordSet thisLayer) {
            VirgisLayer temp = null;
                try {
                    switch (thisLayer.DataType) {
                        case RecordSetDataType.MapBox:
                            MapBox.MapBoxData props = (thisLayer as MapBox).Properties;
                            VirgisAbstractMap mbLayer = Instantiate(MapBoxLayer, transform).GetComponent<VirgisAbstractMap>();
                            mbLayer.UseWorldScale();
                            mbLayer.SetProperties(props.imagerySourceType, props.elevationLayerType, props.elevationSourceType, props.MapSize);
                            mbLayer.Initialize(appState.project.Origin.Coordinates.Vector2d(), props.MapScale);
                            Debug.Log(" Mapbox Options : " + mbLayer.Options.ToString());
                            appState.abstractMap = mbLayer;
                            temp = mbLayer.GetComponent<ContainerLayer>();
                            temp.SetMetadata(thisLayer);
                            temp.changed = false;
                            break;
                        case RecordSetDataType.Vector:
                            temp = await Instantiate(VectorLayer, transform).GetComponent<OgrLayer>().Init(thisLayer as GeographyCollection);
                            break;
                        case RecordSetDataType.Raster:
                            temp = await Instantiate(RasterLayer, transform).GetComponent<RasterLayer>().Init(thisLayer as GeographyCollection);
                            break;
                        case RecordSetDataType.PointCloud:
                            temp = await Instantiate(PointCloud,transform).GetComponent<PointCloudLayer>().Init(thisLayer as GeographyCollection);
                            break;
                        case RecordSetDataType.Mesh:
                            temp = await Instantiate(MeshLayer, transform).GetComponent<MeshLayer>().Init(thisLayer as GeographyCollection);
                            break;
                        case RecordSetDataType.Mdal:
                            temp = await Instantiate(MDALLayer, transform).GetComponent<MdalLayer>().Init(thisLayer as GeographyCollection);
                            break;
                        case RecordSetDataType.XSect:
                            temp = await Instantiate(XsectLayer, transform).GetComponent<XsectLayer>().Init(thisLayer as GeologyCollection);
                            break;
                        case RecordSetDataType.CSV:
                            temp = await Instantiate(CsvLayer,transform).GetComponent<DataPlotter>().Init(thisLayer as RecordSet);
                            break;
                        case RecordSetDataType.DEM:
                            temp = await Instantiate(DemLayer, transform).GetComponent<DemLayer>().Init(thisLayer as GeographyCollection);
                            break;
                        default:
                            Debug.LogError(thisLayer.Type.ToString() + " is not known.");
                            break;
                    }
                    Stack<VirgisLayer> tempLayers = new Stack<VirgisLayer>();
                    tempLayers.Push(temp);
                    while (tempLayers.Count > 0) {
                        VirgisLayer l = tempLayers.Pop();
                        int children = l.transform.childCount;
                        if (l._layer.DataType == RecordSetDataType.MapBox)
                            children = 0;
                        if (children == 0) {
                            appState.addLayer(l);
                            l.gameObject.SetActive(thisLayer.Visible);
                            l.Draw();
                            continue;
                        }
                        for (int i = 0; i < children; i++) {
                            tempLayers.Push(l.transform.GetChild(i).GetComponent<VirgisLayer>());
                        } 
                    }
                } catch (Exception e) {
                    Debug.LogError(e.ToString() ?? "Unknown Error");
                }
        }

        public void Add(MoveArgs args)
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

        public async Task<RecordSet> Save(bool all = true)
        {
            // TODO: wrap this in try/catch block
            Debug.Log("MapInitialize.Save starts");

            if (all) {
                foreach (IVirgisLayer com in appState.layers) {
                    RecordSet alayer = await com.Save();
                    int index = appState.project.RecordSets.FindIndex(x => x.Id == alayer.Id);
                    appState.project.RecordSets[index] = alayer;
                }
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
