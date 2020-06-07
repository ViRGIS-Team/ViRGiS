using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Virgis {

    [System.Serializable]
    public class LayerPanelEditSelectedEvent : UnityEvent<LayerUIPanel, bool> {}

    public class LayerUIPanel : MonoBehaviour {
        public Toggle editLayerToggle;
        public Toggle viewLayerToggle;

        private IVirgisLayer _layer;
        private LayerPanelEditSelectedEvent _editSelectedEvent;

        void Awake() {
            _editSelectedEvent = new LayerPanelEditSelectedEvent();
            editLayerToggle.onValueChanged.AddListener(OnEditToggleValueChange);
            viewLayerToggle.onValueChanged.AddListener(OnViewToggleValueChange);
        }

        public IVirgisLayer layer {
            get => _layer;
            set {
                _layer = value;
                // layer name to be displayed is RecordSet.DisplayName, 
                // or RecordSet.Id as fallback
                string displayName = String.IsNullOrEmpty(_layer.GetMetadata().DisplayName) 
                    ? $"ID: {_layer.GetMetadata().Id}" 
                    : _layer.GetMetadata().DisplayName;
                viewLayerToggle.GetComponentInChildren<Text>().text = displayName;
            }
        }

        public void AddEditSelectedListener(UnityAction<LayerUIPanel, bool> action) {
            _editSelectedEvent.AddListener(action);
        }

        private void OnEditToggleValueChange(bool enabled) {
            if (enabled && !_layer.IsEditable()) {
                // if the layer is already editable, don't invoke
                _editSelectedEvent.Invoke(this, true);
            } else if (!enabled && _layer.IsEditable()) {
                _editSelectedEvent.Invoke(this, false);
            }
        }

        private void OnViewToggleValueChange(bool visible) {
            if (visible) {
                viewLayerToggle.GetComponentInChildren<Text>().color = new Color32(0, 0, 245, 255);
                _layer.SetVisible(true);
            } else {
                viewLayerToggle.GetComponentInChildren<Text>().color = new Color32(100, 100, 100, 255);
                _layer.SetVisible(false);
            }
        }
    }
}