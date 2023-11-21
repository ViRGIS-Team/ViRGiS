/* MIT License

Copyright (c) 2020 - 23 Runette Software

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


using Project;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;

namespace Virgis {


    /// <summary>
    /// This script initialises the project and loads the Project and Layer data.
    /// 
    /// It is run once at Startup
    /// </summary>
    public abstract class ServerInitialize : MapInitializePrototype
    {
        public State appState;
        
        private ProjectJsonReader m_projectJsonReader;

        ///<summary>
        ///Instantiates all singletons.
        /// </summary>
        protected void Awake()
        {
            Debug.Log("Map awakens");
            if (State.instance == null) {
                Debug.Log("instantiate app state");
                Instantiate(appState);
            }
            AppState.instance.server = gameObject;
            Debug.Log($"Virgis version : {Application.version}");
            Debug.Log($"Project version: {GisProject.GetVersion()}");
        }

        protected new void Start() {
            base.Start();
            Debug.Log("Checking for Startup Project");
            if (m_loadOnStartup != null)
                Load(m_loadOnStartup);
        }


        /// 
        /// This is the initialisation script.
        /// 
        /// It loads the Project file, reads it for the layers and calls Draw to render each layer
        /// </summary>
        public override bool Load(string file) {
            return _load(file);
        }

        protected override bool _load(string file) {
            Debug.Log("Starting  to load Project File");
            // Get Project definition - return if the file cannot be read - this will lead to an empty world
            m_projectJsonReader = new ProjectJsonReader();
            try {
                m_projectJsonReader.Load(file);
            } catch (Exception e) {
                Debug.LogError($"Project File {file} is invalid :: " + e.ToString());
                return false;
            }

            if (m_projectJsonReader.payload is null) {
                Debug.LogError($"Project File {file} is empty");
                return false;
            }

            AppState.instance.project = m_projectJsonReader.GetProject();
            AppState.instance.project.path = Path.GetDirectoryName(file);

            try {
                initLayers(AppState.instance.project.RecordSets);
            } catch (Exception e) {
                Debug.LogError($"Project File {file} failed :" + e.ToString());
                return false;
            }
            OnLoad();
            //set globals
            AppState.instance.Project.Complete();
            Debug.Log("Completed load Project File");
            return true;
        }

        /// <summary>
        /// override this call in the consuming project to process the individual layers.
        /// This allows the consuming project to define the layer types
        /// </summary>
        /// <param name="thisLayer"> the layer that ws pulled from the project file</param>
        /// <returns></returns>
        public abstract VirgisLayer CreateLayer(RecordSet thisLayer);


        protected void initLayers(List<RecordSet> layers) {
            AppState.instance.tasks = new ();
            foreach (RecordSet thisLayer in layers) {
                VirgisLayer temp = null;
                Debug.Log("Loading Layer : " + thisLayer.DisplayName);
                temp = CreateLayer(thisLayer);
                State appState = State.instance;
                if (!temp.Spawn(State.instance.map.transform)) Debug.Log("reparent failed");
                IEnumerator task = temp.Init(thisLayer).AsIEnumerator();
                StartCoroutine(task);
                AppState.instance.tasks.Add(task);
            }
        }


        /// <summary>
        /// This call initiates the drawing of the virtual space and calls `Draw ` on each layer in turn.
        /// </summary>
        public new void Draw()
        {
            foreach (IVirgisLayer layer in AppState.instance.layers)
            {
                try {
                    layer.Draw();
                } catch(Exception e) {
                    Debug.LogError($"Project Layer {layer.sourceName} hasfailed to draw :" + e.ToString());
                }
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
        public override async Task<RecordSetPrototype> Save(bool all = true) {
            try {
                Debug.Log("MapInitialize.Save starts");
                if (AppState.instance.project != null) {
                    if (all) {
                        foreach (IVirgisLayer com in AppState.instance.layers) {
                            RecordSet alayer = await (com as VirgisLayer).Save() as RecordSet;
                            int index = AppState.instance.project.RecordSets.FindIndex(x => x.Id == alayer.Id);
                            AppState.instance.project.RecordSets[index] = alayer;
                        }
                    }
                    m_projectJsonReader.SetProject(AppState.instance.project);
                    await m_projectJsonReader.Save();
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


        protected override void _onEditStart(bool ignore)
        {
            CheckPoint();
        }

        /// <summary>
        /// Called when an edit session ends
        /// </summary>
        /// <param name="saved">true if stop and save, false if stop and discard</param>
        protected async override void _onEditStop(bool saved)
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
