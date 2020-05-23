using UnityEngine;

namespace Virgis {
    public class ShapeAdder : MonoBehaviour {
        public GameObject cubePrefab;
        public GameObject theCube;

        private AppState _appState;

        // Start is called before the first frame update
        void Start() {
            Debug.Log("ShapeAdder starts");
            _appState = AppState.instance;
            _appState.editSession.AddStartEditSessionListener(OnStartEditSession);
            _appState.editSession.AddEndEditSessionListener(OnEndEditSession);
        }

        public void LeftTriggerPressed(bool activate) {
            Debug.Log($"LeftTriggerPressed: activate = {activate}");
            if (_appState.editSession.IsActive()) {
                ILayer editableLayer = _appState.editableLayer;
                MoveArgs args = new MoveArgs();
                args.pos = theCube.transform.position;
                args.rotate = theCube.transform.rotation;
                editableLayer.AddFeature(args);
            }
            //Vector3 pos = theCube.transform.position;
            //Quaternion rot = theCube.transform.rotation;
            //GameObject newCube = Instantiate(cubePrefab, pos, rot);
        }

        public void LeftTriggerReleased(bool activate) {
            Debug.Log($"LeftTriggerReleased: activate = {activate}");
        }

        private void OnStartEditSession() {
            Debug.Log("ShapeAdder OnStartEditSession");
            theCube.SetActive(true);
        }

        private void OnEndEditSession(bool saved) {
            Debug.Log("ShapeAdder OnEndEditSession");
            theCube.SetActive(false);
        }

    }
}
