// copyright Runette Software Ltd, 2020. All rights reserved
using GeoJSON.Net.Geometry;

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
    public abstract class MapInitialize : VirgisLayer
    {

        public AppState appState;

        //File reader for Project file
        private ProjectJsonReader projectJsonReader;


        ///<summary>
        ///Instantiates all singletons.
        /// </summary>
        protected void Awake()
        {
            Debug.Log("Map awakens");
            Debug.Log($"Virgis version : {Application.version}");
            Debug.Log($"Project version: {GisProject.GetVersion()}");
            if (AppState.instance == null)
            {
                Debug.Log("instantiate app state");
                appState = Instantiate(appState);
            }
            appState.editSession.StartEvent.Subscribe(_onStartEditSession);
            appState.editSession.EndEvent.Subscribe(_onExitEditSession);

            //set globals
            appState.map = gameObject;
        }

        protected void Start() {
            
        }

        protected void Update() {
            
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

            try {
                foreach (RecordSet thisLayer in appState.project.RecordSets) {
                    await initLayer(thisLayer);
                };
            } catch {
                Debug.LogError($"Project File {file} failed");
                return false;
            }

            //set globals
            appState.Project.Complete();
            return true;
        }

        /// <summary>
        /// override this call in the consuming project to process the individual layers.
        /// This allows the consuming project to define the layer types
        /// </summary>
        /// <param name="thisLayer"> the lkayer that ws pulled from the project file</param>
        /// <returns></returns>
        public abstract Task<VirgisLayer> createLayer(RecordSet thisLayer);


        private async Task initLayer(RecordSet thisLayer) {
            VirgisLayer temp = null;
                try {
                    temp =  await createLayer(thisLayer);                   
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


        /// <summary>
        /// This cll initiates the drawing of the bvirtual spce and calls `Draw ` on each layer in turn.
        /// </summary>
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

        protected override void _checkpoint()
        {
        }

        /// <summary>
        /// this call initiates the saving of the whole project and calls `Save` on each layer in turn
        /// </summary>
        /// <param name="all"></param>
        /// <returns></returns>
        public async Task<RecordSet> Save(bool all = true) {
            try {
                Debug.Log("MapInitialize.Save starts");
                if (appState.project != null) {
                    if (all) {
                        foreach (IVirgisLayer com in appState.layers) {
                            RecordSet alayer = await com.Save();
                            int index = appState.project.RecordSets.FindIndex(x => x.Id == alayer.Id);
                            appState.project.RecordSets[index] = alayer;
                        }
                    }
                    appState.project.Scale = appState.Zoom.Get();
                    appState.project.Cameras = new List<Point>() { appState.mainCamera.transform.position.ToPoint() };
                    projectJsonReader.SetProject(appState.project);
                    await projectJsonReader.Save();
                }
                return default;
            } catch (Exception e) {
                Debug.Log("Save failed : " + e.ToString());
                return default;
            }
        }

        protected override Task _save()
        {
            throw new System.NotImplementedException();
        }


        protected void _onStartEditSession(bool ignore)
        {
            CheckPoint();
        }

        /// <summary>
        /// Called when an edit session ends
        /// </summary>
        /// <param name="saved">true if stop and save, false if stop and discard</param>
        protected async void _onExitEditSession(bool saved)
        {
            if (!saved) {
                Draw();
            }
            await Save(saved);
    }

        public override GameObject GetFeatureShape()
        {
            return null;
        }

    }
}
