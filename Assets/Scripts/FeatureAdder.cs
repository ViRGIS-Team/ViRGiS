using Project;
using SQLite4Unity3d;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Virgis {
    public class FeatureAdder : MonoBehaviour {
        public GameObject blueCubePrefab;
        public GameObject defaultMarkerShape;

        private AppState _appState;
        private GameObject _markerShape;
        private Dictionary<Guid, GameObject> _markerShapeMap;
        
        // variables for double-press timer
        private IEnumerator _timer;
        private bool _waitingForSecondPress;

        // variables for adding feature
        private VirgisComponent _newFeature;
        private Datapoint _lineVertex;

        // Start is called before the first frame update
        void Start() {
            Debug.Log("ShapeAdder starts");
            _appState = AppState.instance;
            _markerShapeMap = new Dictionary<Guid, GameObject>();
            _markerShape = defaultMarkerShape;
            _appState.editSession.AddStartEditSessionListener(OnStartEditSession);
            _appState.editSession.AddEndEditSessionListener(OnEndEditSession);
            _appState.editSession.AddEditableLayerChangedListener(OnEditableLayerChanged);
            _waitingForSecondPress = false;
            _newFeature = null;
        }

        void Update() {
            if (_appState.editSession.IsActive() && (_newFeature != null)) {
                MoveArgs args = new MoveArgs();
                args.pos = _markerShape.transform.position;
                _lineVertex.MoveTo(args);
            }
        }

        public void LeftTriggerPressed(bool activate) {
            //Debug.Log($"LeftTriggerPressed: activate = {activate}");
            if (_waitingForSecondPress) {
                StopCoroutine(_timer);
                _waitingForSecondPress = false;
                OnTriggerDoublePress();
            } else {
                _timer = WaitForSecondTriggerPress();
                StartCoroutine(_timer);
                _waitingForSecondPress = true;
            }
        }

        public void LeftTriggerReleased(bool activate) {
            //Debug.Log($"LeftTriggerReleased: activate = {activate}");
        }

        public void LeftGripPressed(bool activate) {
            Debug.Log($"LeftGripPressed: activate = {activate}");
        }

        public void LeftGripReleased(bool activate) {
            Debug.Log($"LeftGripPressed: activate = {activate}");
        }

        private void OnStartEditSession() {
            Debug.Log("ShapeAdder OnStartEditSession");
            ILayer editableLayer = _appState.editSession.editableLayer;
            _markerShape = SelectMarkerShape(editableLayer);
            _markerShape.SetActive(true);
        }

        private void OnEditableLayerChanged(ILayer newEditableLayer) {
            Debug.Log("ShapeAdder OnEditableLayerChanged");
            _markerShape.SetActive(false);
            _markerShape = SelectMarkerShape(newEditableLayer);
            _markerShape.SetActive(true);
        }

        private void OnEndEditSession(bool saved) {
            Debug.Log("ShapeAdder OnEndEditSession");
            _markerShape.SetActive(false);
        }

        private void OnTriggerSinglePress() {
            Debug.Log("ShapeAdder OnTriggerSinglePress");
            if (_appState.editSession.IsActive()) {
                ILayer editableLayer = _appState.editSession.editableLayer;
                RecordSetDataType dataType = editableLayer.GetMetadata().DataType;
                switch (dataType) {
                    case RecordSetDataType.Point:
                        Debug.Log("ShapeAdder Add Point Feature");
                        editableLayer.AddFeature(_markerShape.transform.position);
                        //GameObject newShape = Instantiate(blueCubePrefab, _markerShape.transform.position, _markerShape.transform.rotation);
                        break;
                    case RecordSetDataType.Line:
                        //Debug.Log($"ShapeAdder add Vertex");
                        if (_newFeature != null) {
                            Vector3 markerPos = _markerShape.transform.position;
                            markerPos.y += 0.01f;
                            _lineVertex = _newFeature.AddVertex(markerPos) as Datapoint;
                        } else {
                            _newFeature = editableLayer.AddFeature(_markerShape.transform.position);
                            // get the last vertex
                            Datapoint[] vertexes = (_newFeature as Dataline).GetVertexes();
                            _lineVertex = vertexes[1];
                        }
                        break;
                }
            }
        }

        private void OnTriggerDoublePress() {
            Debug.Log("ShapeAdder OnTriggerDoublePress");
            if (_appState.editSession.IsActive()) {
                ILayer editableLayer = _appState.editSession.editableLayer;
                RecordSetDataType dataType = editableLayer.GetMetadata().DataType;
                switch (dataType) {
                    case RecordSetDataType.Line:
                        if (_newFeature != null) {
                            // complete adding line feature
                            _newFeature = null;
                            _lineVertex = null;
                        }
                        break;
                }
            }
        }

        private GameObject SelectMarkerShape(ILayer layer) {
            if (_markerShapeMap.ContainsKey(layer.GetId())) {
                return _markerShapeMap[layer.GetId()];
            } else {
                GameObject featureShape = layer.GetFeatureShape();
                if (featureShape == null) {
                    return defaultMarkerShape;
                } else {
                    GameObject go = Instantiate(featureShape, defaultMarkerShape.transform.position, defaultMarkerShape.transform.rotation, transform);
                    go.transform.localScale = defaultMarkerShape.transform.localScale;
                    _markerShapeMap.Add(layer.GetId(), go);
                    return go;
                }
            }
        }

        private IEnumerator WaitForSecondTriggerPress() {
            //Debug.Log("ShapeAdder WaitForSecondPress starts");
            float eventTime = Time.unscaledTime + 0.5f;
            while (Time.unscaledTime < eventTime)
                yield return null;
            //yield return new WaitForSecondsRealtime(0.5f);
            //Debug.Log("ShapeAdder WaitForSecondPress ends");
            _waitingForSecondPress = false;
            OnTriggerSinglePress();
        }

    }
}
