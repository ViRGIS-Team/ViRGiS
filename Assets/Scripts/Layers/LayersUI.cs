using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Virgis {

    /// <summary>
    /// LayersUI is the mediator for all components within the Layers UI GO (i.e. Layers Menu).
    /// </summary>
    ///
    /// For desktop Scene, the Layers UI GO is used in:
    /// 1) InputMapping
    /// 2) Menus GO
    /// 
    /// 
    public class LayersUI : MonoBehaviour {
        public GameObject layersScrollView;
        public GameObject layerPanelPrefab;
        public GameObject menus;

        private AppState appState;
        private Dictionary<Guid, LayerUIPanel> layersMap;

        // Start is called before the first frame update
        void Start() {
            appState = AppState.instance;
            layersMap = new Dictionary<Guid, LayerUIPanel>();
            appState.editSession.AddStartEditSessionListener(OnStartEditSession);
            appState.editSession.AddEndEditSessionListener(OnEndEditSession);
            CreateLayerPanels();
        }

        public void HandleKeyInput(InputAction.CallbackContext context) {
            InputAction action = context.action;
            if (action.name == "ShowLayers") {
                bool isActive = gameObject.activeSelf;
                if (isActive) {
                    gameObject.SetActive(false);
                } else {
                    menus.SetActive(false);
                    gameObject.SetActive(true);
                }
            }
        }

        public void OnShowMenuButtonClicked() {
            gameObject.SetActive(false);
            menus.SetActive(true);
        }

        private void CreateLayerPanels() {
            GameObject newLayerPanel;

            // appState.layers are actually Layer prefabs (Component)
            appState.layers.ForEach(comp => {
                // obtain the actual Layer object
                ILayer layer = comp.GetComponentInChildren<ILayer>();
                print($"CreateLayerPanels: layer {layer.GetMetadata().Id ?? ""}, {layer.GetMetadata().DisplayName ?? ""}");
                // create a view panel for this particular layer
                newLayerPanel = (GameObject) Instantiate(layerPanelPrefab, transform);
                // obtain the panel script
                LayerUIPanel panelScript = newLayerPanel.GetComponentInChildren<LayerUIPanel>();
                // set the layer in the panel
                panelScript.layer = layer;
                // when the Layers Menu screen is first displayed,
                // edit session could already be active
                if (appState.editSession.IsActive()) {
                    // in edit session, layer can be set to edit
                    panelScript.editLayerToggle.interactable = true;
                    if (layer.IsInEditSession())
                        panelScript.editLayerToggle.isOn = true;
                } else {
                    // not in edit session, layer cannot be set to edit
                    panelScript.editLayerToggle.interactable = false;
                }
                layersMap.Add(layer.GetId(), panelScript);
                newLayerPanel.transform.SetParent(layersScrollView.transform, false);
            });
        }

        private void OnStartEditSession() {
            foreach (LayerUIPanel panel in layersMap.Values) {
                panel.editLayerToggle.interactable = true;
                if (panel.layer.IsInEditSession())
                    panel.editLayerToggle.isOn = true;
            }
        }

        private void OnEndEditSession(bool saved) {
            foreach (LayerUIPanel panel in layersMap.Values) {
                panel.editLayerToggle.interactable = false;
            }
        }
    }
}