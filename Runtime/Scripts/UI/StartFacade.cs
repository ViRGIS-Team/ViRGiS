/* MIT License

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
using UnityEngine;
using Project;



namespace Virgis {

    public class StartFacade : MonoBehaviour {

        public GameObject fileListPanelPrefab;
        public GameObject fileScrollView;
        public string projectDirectory;
        public string searchPattern;
        
        private AppState _appState;



        // Start is called before the first frame update
        void Start() {
            _appState = AppState.instance;
            CreateFilePanels();
        }

        // Update is called once per frame
        void Update() {

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
            if (AppState.instance.layers != null)  foreach (VirgisLayer layer in AppState.instance.layers) {
                  Destroy(layer.gameObject);
            }
            AppState.instance.clearLayers();
            Debug.Log("File selected :" + file);
            gameObject.SetActive(false);
            if (! AppState.instance.map.GetComponent<MapInitialize>().Load(file)) {
                gameObject.SetActive(true);
            }
        } 

    }
}
