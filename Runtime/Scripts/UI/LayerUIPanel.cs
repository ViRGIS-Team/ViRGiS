using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Virgis {

    [System.Serializable]
    public class LayerPanelEditSelectedEvent : UnityEvent<LayerUIPanel, bool> {}

    public class LayerUIPanel : MonoBehaviour {
        public Toggle editLayerToggle;
        public Text layerNameText;

        private IVirgisLayer _layer;
        private LayerPanelEditSelectedEvent _editSelectedEvent;

        void Awake() {
            if (_editSelectedEvent == null)
                _editSelectedEvent = new LayerPanelEditSelectedEvent();
            if (editLayerToggle != null )
                editLayerToggle.onValueChanged.AddListener(OnEditToggleValueChange);

        }

        public IVirgisLayer layer {
            get => _layer;
            set {
                _layer = value;
                if (_layer.sourceName == null || layer.sourceName == "") {
                    layerNameText.text = _layer.featureType.ToString();
                } else {
                    layerNameText.text = _layer.sourceName;
                }
            }
        }

        public void AddEditSelectedListener(UnityAction<LayerUIPanel, bool> action) {
            if (_editSelectedEvent == null)
                _editSelectedEvent = new LayerPanelEditSelectedEvent();
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
    }
}