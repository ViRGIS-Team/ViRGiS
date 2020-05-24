using Project;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Virgis {
    public class ShapeAdder : MonoBehaviour {
        public GameObject blueCubePrefab;
        public GameObject defaultMarkerShape;

        private GameObject _markerShape;
        private AppState _appState;
        private Dictionary<Guid, GameObject> _markerShapeMap;

        // Start is called before the first frame update
        void Start() {
            Debug.Log("ShapeAdder starts");
            _appState = AppState.instance;
            _markerShapeMap = new Dictionary<Guid, GameObject>();
            _markerShape = defaultMarkerShape;
            _appState.editSession.AddStartEditSessionListener(OnStartEditSession);
            _appState.editSession.AddEndEditSessionListener(OnEndEditSession);
            _appState.editSession.AddEditableLayerChangedListener(OnEditableLayerChanged);
        }

        public void LeftTriggerPressed(bool activate) {
            Debug.Log($"LeftTriggerPressed: activate = {activate}");
            //if (_appState.editSession.IsActive()) {
            //    ILayer editableLayer = _appState.editableLayer;
            //    RecordSetDataType dataType = editableLayer.GetMetadata().DataType;
            //    switch (dataType) {
            //        case RecordSetDataType.Point:
            //            MoveArgs args = new MoveArgs();
            //            args.pos = theCube.transform.position;
            //            args.rotate = theCube.transform.rotation;
            //            editableLayer.AddFeature(args);
            //            break;
            //        case RecordSetDataType.Line:
            //            Debug.Log($"ShapeAdder add Vertex");
            //            (editableLayer as LineLayer).AddVertex(theCube.transform.position);
            //            break;
            //    }
            //}
        }

        public void LeftTriggerReleased(bool activate) {
            Debug.Log($"LeftTriggerReleased: activate = {activate}");
        }

        public void LeftGripPressed(bool activate) {
            Debug.Log($"LeftGripPressed: activate = {activate}");
            //if (_appState.editSession.IsActive()) {
            //    ILayer editableLayer = _appState.editableLayer;
            //    RecordSetDataType dataType = editableLayer.GetMetadata().DataType;
            //    switch (dataType) {
            //        case RecordSetDataType.Line:
            //            Debug.Log($"ShapeAdder add Vertex");
            //            MoveArgs args = new MoveArgs();
            //            args.pos = theCube.transform.position;
            //            editableLayer.AddFeature(args);
            //            break;
            //    }
            //}
        }

        public void LeftGripReleased(bool activate) {
            Debug.Log($"LeftGripPressed: activate = {activate}");
        }

        private void OnStartEditSession() {
            Debug.Log("ShapeAdder OnStartEditSession");
            ILayer editableLayer = _appState.editSession.editableLayer;
            _markerShape = selectMarkerShape(editableLayer);
            _markerShape.SetActive(true);
        }

        private void OnEditableLayerChanged(ILayer newEditableLayer) {
            Debug.Log("ShapeAdder OnEditableLayerChanged");
            _markerShape.SetActive(false);
            _markerShape = selectMarkerShape(newEditableLayer);
            _markerShape.SetActive(true);
        }

        private void OnEndEditSession(bool saved) {
            Debug.Log("ShapeAdder OnEndEditSession");
            _markerShape.SetActive(false);
        }

        private GameObject selectMarkerShape(ILayer layer) {
            if (_markerShapeMap.ContainsKey(layer.GetId())) {
                return _markerShapeMap[layer.GetId()];
            } else {
                GameObject featureShape = layer.GetFeatureShape();
                if (featureShape == null) {
                    return defaultMarkerShape;
                } else {
                    GameObject go = Instantiate(featureShape, defaultMarkerShape.transform.position, defaultMarkerShape.transform.rotation, transform);
                    go.transform.localScale = defaultMarkerShape.transform.localScale;
                    return go;
                }
            }
        }

    }
}
