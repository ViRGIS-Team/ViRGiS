﻿/* MIT License

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

using System.IO;
using System.Collections.Generic;
using System;
using UnityEngine;
using UniRx;
using Project;

namespace Virgis {

    public class StartFacade : MonoBehaviour {

        public GameObject fileListPanelPrefab;
        public GameObject fileScrollView;
        public string projectDirectory;
        public string searchPattern;
        
        private AppState m_appState;
        private List<IDisposable> subs = new List<IDisposable>();

        // Start is called before the first frame update
        void Start() {
            m_appState = AppState.instance;
            CreateFilePanels();
            subs.Add(m_appState.Project.Event.Subscribe(OnProjectLoad));
            if (m_appState.Project.Get() != null)
                OnProjectLoad(m_appState.Project.Get());
        }

        private void OnDestroy() {
            subs.ForEach(sub => sub.Dispose());
        }

        private void OnProjectLoad(GisProject proj) {
            gameObject.SetActive(false);
        }

        private void CreateFilePanels() {
            GameObject newFilePanel;

            // get the file list
            foreach (string file in Directory.GetFiles(projectDirectory, searchPattern, SearchOption.AllDirectories)) {

                //Create this filelist panel
                newFilePanel = (GameObject) Instantiate(fileListPanelPrefab, fileScrollView.transform );

                // obtain the panel script
                FileListPanel panelScript = newFilePanel.GetComponentInChildren<FileListPanel>();

                // set the filein the panel
                panelScript.File = file;

                panelScript.addFileSelectedListerner(onFileSelected);

            };
        }

        public void onFileSelected(string file) {
            if (m_appState.layers != null)  foreach (VirgisLayer layer in m_appState.layers) {
                  Destroy(layer.gameObject);
            }
            m_appState.clearLayers();
            Debug.Log("File selected :" + file);
            gameObject.SetActive(false);
            if (! m_appState.map.GetComponent<MapInitialize>().Load(file)) {
                gameObject.SetActive(true);
            }
        } 
    }
}
