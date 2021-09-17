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

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Virgis {

    [System.Serializable]
    public class LayerPanelEditSelectedEvent : UnityEvent<LayerUIPanel, bool> {}

    public class LayerUIPanel : MonoBehaviour {
        public Toggle editLayerToggle;
        public Text layerNameText;

        private IVirgisLayer m_layer;
        private LayerPanelEditSelectedEvent m_editSelectedEvent;

        void Awake() {
            if (m_editSelectedEvent == null)
                m_editSelectedEvent = new LayerPanelEditSelectedEvent();
            if (editLayerToggle != null )
                editLayerToggle.onValueChanged.AddListener(OnEditToggleValueChange);

        }

        public IVirgisLayer layer {
            get => m_layer;
            set {
                m_layer = value;
                if (layerNameText != null) {
                    if (m_layer.sourceName == null || layer.sourceName == "") {
                        layerNameText.text = m_layer.featureType.ToString();
                    } else {
                        layerNameText.text = m_layer.sourceName;
                    }
                }
            }
        }

        public void AddEditSelectedListener(UnityAction<LayerUIPanel, bool> action) {
            if (m_editSelectedEvent == null)
                m_editSelectedEvent = new LayerPanelEditSelectedEvent();
            m_editSelectedEvent.AddListener(action);
        }

        private void OnEditToggleValueChange(bool enabled) {
            if (enabled && !m_layer.IsEditable()) {
                // if the layer is already editable, don't invoke
                m_editSelectedEvent.Invoke(this, true);
            } else if (!enabled && m_layer.IsEditable()) {
                m_editSelectedEvent.Invoke(this, false);
            }
        }
    }
}