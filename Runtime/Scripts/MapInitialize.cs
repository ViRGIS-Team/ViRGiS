// copyright Runette Software Ltd, 2020. All rights reserved
using GeoJSON.Net.Geometry;
using Project;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System;
using UniRx;

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
        private IDisposable startsub;
        private IDisposable stopsub;


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
            startsub = appState.editSession.StartEvent.Subscribe(_onStartEditSession);
            stopsub = appState.editSession.EndEvent.Subscribe(_onExitEditSession);

            //set globals
            appState.map = gameObject;
        }

        protected void Start() {
            
        }

        protected void Update() {
            
        }

        protected void OnDestroy() {
            startsub.Dispose();
            stopsub.Dispose();
        }

        protected override Task _init() {
            throw new NotImplementedException();
        }


        /// 
        /// This is the initialisation script.
        /// 
        /// It loads the Project file, reads it for the layers and calls Draw to render each layer
        /// </summary>
        public bool Load(string file) {
            Debug.Log("Starting  to load Project File");
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
                   initLayers(appState.project.RecordSets);
            } catch {
                Debug.LogError($"Project File {file} failed");
                return false;
            }

            //set globals
            appState.Project.Complete();
            Debug.Log("Completed load Project File");
            return true;
        }

        /// <summary>
        /// override this call in the consuming project to process the individual layers.
        /// This allows the consuming project to define the layer types
        /// </summary>
        /// <param name="thisLayer"> the lkayer that ws pulled from the project file</param>
        /// <returns></returns>
        public abstract VirgisLayer createLayer(RecordSet thisLayer);


        private void initLayers(List<RecordSet> layers) {
            foreach (RecordSet thisLayer in layers) {
                VirgisLayer temp = null;
                Debug.Log("Loading Layer : " + thisLayer.DisplayName);
                temp = createLayer(thisLayer);
                StartCoroutine(temp.Init(thisLayer).AsIEnumerator());
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

        protected override Task _draw()
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
