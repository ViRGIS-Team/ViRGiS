﻿using GeoJSON.Net.Feature;
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

        // variables for adding feature - currently used only for Line feature
        private VirgisComponent _newFeature;
        private Datapoint _firstVertex;
        private List<Datapoint> _lastVertex = new List<Datapoint>();

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
                _lastVertex.ForEach( dp =>  dp.MoveTo(args));
            }
        }

        public void LeftTriggerPressed(bool activate) {
            //Debug.Log($"LeftTriggerPressed: activate = {activate}");
            if (_waitingForSecondPress) {
                StopCoroutine(_timer);
                _waitingForSecondPress = false;
                OnTriggerDoublePress();
            } else {
                _timer = WaitForSecondTriggerPress(_markerShape.transform.position);
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

        private void OnTriggerSinglePress(Vector3 posWhenSinglePress) {
            Debug.Log("ShapeAdder OnTriggerSinglePress");
            if (_appState.editSession.IsActive()) {
                ILayer editableLayer = _appState.editSession.editableLayer;
                RecordSetDataType dataType = editableLayer.GetMetadata().DataType;
                Datapoint[] vertexes;
                switch (dataType) {
                    case RecordSetDataType.Point:
                        var _ = editableLayer.AddFeature(new Vector3[1] { posWhenSinglePress });
                        break;
                    case RecordSetDataType.Line:
                        //Debug.Log($"ShapeAdder add Vertex");
                        if (_newFeature != null) {
                            _newFeature.AddVertex(posWhenSinglePress);
                        } else {
                            _newFeature = editableLayer.AddFeature(new Vector3[2] { posWhenSinglePress, posWhenSinglePress + Vector3.one * Single.Epsilon });
                            // get the last vertex
                            vertexes = (_newFeature as Dataline).GetVertexes();
                            _firstVertex = vertexes[0];
                            _lastVertex.Add(vertexes[1]);
                        }
                        break;
                    case RecordSetDataType.Polygon:
                        if (_newFeature != null ) {
                            if (_lastVertex.Count == 1) {
                                _newFeature.transform.GetComponentInChildren<Dataline>().AddVertex(posWhenSinglePress);
                            } else {
                                _lastVertex.RemoveAt(0);
                            }
                            
                        } else {
                            Debug.Log("ShapeAdder Add Polygon Feature");
                            _newFeature = editableLayer.AddFeature(new Vector3[4] { posWhenSinglePress, posWhenSinglePress + Vector3.right * Single.Epsilon, posWhenSinglePress + Vector3.up * Single.Epsilon, posWhenSinglePress });
                            vertexes = (_newFeature as Datapolygon).GetVertexes();
                            _firstVertex = vertexes[0];
                            _lastVertex.Add(vertexes[1]);
                            _lastVertex.Add(vertexes[2]);
                            ;
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
                            // if edit mode is snap to anchor and start and end vertexes are at the same position
                            // call Dataline.MakeLinearRing()
                            if (_appState.editSession.mode == EditSession.EditMode.SnapAnchor && 
                                _firstVertex.transform.position == _lastVertex[0].transform.position) {
                                (_newFeature as Dataline).MakeLinearRing();
                            }
                            // complete adding line feature
                            _newFeature = null;
                            _lastVertex.Clear();
                        }
                        break;
                    case RecordSetDataType.Polygon:
                        if (_newFeature != null) {
                            // complete adding line feature
                            _newFeature = null;
                            _lastVertex.Clear();
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
                    featureShape.transform.parent = transform;
                    featureShape.transform.position = defaultMarkerShape.transform.position;
                    featureShape.transform.rotation = defaultMarkerShape.transform.rotation;
                    featureShape.transform.localScale = defaultMarkerShape.transform.localScale;
                    _markerShapeMap.Add(layer.GetId(), featureShape);
                    return featureShape;
                    //GameObject go = Instantiate(featureShape, defaultMarkerShape.transform.position, defaultMarkerShape.transform.rotation, transform);
                    //go.transform.localScale = defaultMarkerShape.transform.localScale;
                    //_markerShapeMap.Add(layer.GetId(), go);
                    //return go;
                }
            }
        }

        private IEnumerator WaitForSecondTriggerPress(Vector3 posWhenSinglePress) {
            //Debug.Log("ShapeAdder WaitForSecondPress starts");
            float eventTime = Time.unscaledTime + 0.5f;
            while (Time.unscaledTime < eventTime)
                yield return null;
            //yield return new WaitForSecondsRealtime(0.5f);
            //Debug.Log("ShapeAdder WaitForSecondPress ends");
            _waitingForSecondPress = false;
            OnTriggerSinglePress(posWhenSinglePress);
        }

    }
}
