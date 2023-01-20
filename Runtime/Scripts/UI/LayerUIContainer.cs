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
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace Virgis
{
    public class LayerUIContainer : MonoBehaviour
    {

        public GameObject subLayerPanel;
        public GameObject subLayerBox;
        public List<GameObject> subPanels = new List<GameObject>();
        public Text layerNameText;
        public Toggle viewLayerToggle;

        private AppState m_appState;
        private IVirgisLayer m_layer;
        public Dictionary<Guid, LayerUIPanel> m_layersMap;

        void Awake()
        {
            m_appState = AppState.instance;
            if (viewLayerToggle != null)
                viewLayerToggle.onValueChanged.AddListener(OnViewToggleValueChange);
        }

        public void expand(bool thisEvent) 
        {
            subLayerBox.SetActive(thisEvent);
            RectTransform trans = transform as RectTransform;
            if (thisEvent) {
                
                trans.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 40 + 40 * subPanels.Count);
            } else {
                trans.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 40);
                //foreach (GameObject panel in subPanels)
                //    Destroy(panel);
                //subPanels.Clear();
            }
            trans.ForceUpdateRectTransforms();
        }

        public IVirgisLayer layer {
            get => m_layer;
            set {
                m_layer = value;
                // layer name to be displayed is RecordSet.DisplayName, 
                // or RecordSet.Id as fallback
                string displayName = String.IsNullOrEmpty(m_layer.GetMetadata().DisplayName)
                    ? $"ID: {m_layer.GetMetadata().Id}"
                    : m_layer.GetMetadata().DisplayName;
                layerNameText.text = displayName;
                if (layer.isContainer) {
                    foreach (VirgisLayer subLayer in layer.subLayers) {
                        AddLayer(subLayer);
                    }
                } else {
                    AddLayer(layer);
                }
            }
        }

        public void AddLayer(IVirgisLayer layer) 
        {
            GameObject panel = Instantiate(subLayerPanel, subLayerBox.transform);
            subPanels.Add(panel);
            LayerUIPanel panelScript = panel.GetComponentInChildren<LayerUIPanel>();
            // set the layer in the panel
            panelScript.layer = layer;
            // listens to panel's edit selected event
            panelScript.AddEditSelectedListener(OnLayerPanelEditSelected);
            if (layer.IsEditable()) panelScript.editLayerToggle.isOn = true;
            // when the Layers Menu screen is first displayed,
            // edit session could already be active
            if (AppState.instance.editSession.IsActive())
            {
                // in edit session, layer can be set to edit
                panelScript.editLayerToggle.interactable = true;
            }
            else
            {
                // not in edit session, layer cannot be set to edit
                panelScript.editLayerToggle.interactable = false;
            }
            m_layersMap.Add(layer.GetId(), panelScript);
            (transform as RectTransform).ForceUpdateRectTransforms();
        }
        private void OnLayerPanelEditSelected(LayerUIPanel layerPanel, bool selected)
        {
            if (selected)
            {
                IVirgisLayer oldEditableLayer = m_appState.editSession.editableLayer;
                m_appState.editSession.editableLayer = layerPanel.layer;
                if (oldEditableLayer != null && m_layersMap.ContainsKey(oldEditableLayer.GetId()))
                    m_layersMap[oldEditableLayer.GetId()].editLayerToggle.isOn = false;
            }
            else
            {
                IVirgisLayer oldEditableLayer = m_appState.editSession.editableLayer;
                m_appState.editSession.editableLayer = null;
                if (oldEditableLayer != null)
                    m_layersMap[oldEditableLayer.GetId()].editLayerToggle.isOn = false;
            }
        }

        private void OnViewToggleValueChange(bool visible) {
            if (visible) {
                viewLayerToggle.GetComponentInChildren<Text>().color = new Color32(0, 0, 245, 255);
                m_layer.SetVisible(true);
            } else {
                viewLayerToggle.GetComponentInChildren<Text>().color = new Color32(100, 100, 100, 255);
                m_layer.SetVisible(false);
            }
        }
    }
}
