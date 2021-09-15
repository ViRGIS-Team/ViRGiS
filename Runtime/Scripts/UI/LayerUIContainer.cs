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

        private AppState _appState;
        private IVirgisLayer _layer;
        public Dictionary<Guid, LayerUIPanel> _layersMap;

        void Awake()
        {
            _appState = AppState.instance;
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
            get => _layer;
            set {
                _layer = value;
                // layer name to be displayed is RecordSet.DisplayName, 
                // or RecordSet.Id as fallback
                string displayName = String.IsNullOrEmpty(_layer.GetMetadata().DisplayName)
                    ? $"ID: {_layer.GetMetadata().Id}"
                    : _layer.GetMetadata().DisplayName;
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
            if (_appState.editSession.IsActive())
            {
                // in edit session, layer can be set to edit
                panelScript.editLayerToggle.interactable = true;
            }
            else
            {
                // not in edit session, layer cannot be set to edit
                panelScript.editLayerToggle.interactable = false;
            }
            _layersMap.Add(layer.GetId(), panelScript);
            (transform as RectTransform).ForceUpdateRectTransforms();
        }
        private void OnLayerPanelEditSelected(LayerUIPanel layerPanel, bool selected)
        {
            if (selected)
            {
                IVirgisLayer oldEditableLayer = _appState.editSession.editableLayer;
                _appState.editSession.editableLayer = layerPanel.layer;
                if (oldEditableLayer != null && _layersMap.ContainsKey(oldEditableLayer.GetId()))
                    _layersMap[oldEditableLayer.GetId()].editLayerToggle.isOn = false;
            }
            else
            {
                IVirgisLayer oldEditableLayer = _appState.editSession.editableLayer;
                _appState.editSession.editableLayer = null;
                if (oldEditableLayer != null)
                    _layersMap[oldEditableLayer.GetId()].editLayerToggle.isOn = false;
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
