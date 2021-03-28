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
