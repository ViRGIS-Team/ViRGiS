using Project;
using System.Collections.Generic;
using UnityEngine;

namespace Virgis {
    public class ShapeAdder : MonoBehaviour {
        public GameObject spherePrefab;
        public GameObject cylinderLinePrefab;
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
                RecordSetDataType dataType = editableLayer.GetMetadata().DataType;
                switch (dataType) {
                    case RecordSetDataType.Point:
                        MoveArgs args = new MoveArgs();
                        args.pos = theCube.transform.position;
                        args.rotate = theCube.transform.rotation;
                        editableLayer.AddFeature(args);
                        break;
                    case RecordSetDataType.Line:
                        Debug.Log($"ShapeAdder add Vertex");
                        (editableLayer as LineLayer).AddVertex(theCube.transform.position);
                        break;
                }
            }
        }

        public void LeftTriggerReleased(bool activate) {
            Debug.Log($"LeftTriggerReleased: activate = {activate}");
        }

        public void LeftGripPressed(bool activate) {
            Debug.Log($"LeftGripPressed: activate = {activate}");
            if (_appState.editSession.IsActive()) {
                ILayer editableLayer = _appState.editableLayer;
                RecordSetDataType dataType = editableLayer.GetMetadata().DataType;
                switch (dataType) {
                    case RecordSetDataType.Line:
                        Debug.Log($"ShapeAdder add Vertex");
                        MoveArgs args = new MoveArgs();
                        args.pos = theCube.transform.position;
                        editableLayer.AddFeature(args);
                        break;
                }
            }
        }

        public void LeftGripReleased(bool activate) {
            Debug.Log($"LeftGripPressed: activate = {activate}");
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
