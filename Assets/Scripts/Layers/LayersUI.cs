using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Virgis {

    /// <summary>
    /// LayersUI is the mediator for all components within the Layers UI GO.
    /// </summary>
    ///
    /// For desktop Scene, the Layers UI GO is used in:
    /// 1) InputMapping
    /// 2) Menus GO
    public class LayersUI : MonoBehaviour {
        public GameObject layersScrollView;
        public GameObject layerPanelPrefab;
        public GameObject menus;

        private AppState appState;
        private Dictionary<string, string> layersMap;

        // Start is called before the first frame update
        void Start() {
            appState = AppState.instance;
            layersMap = new Dictionary<string, string>();
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

            appState.project.RecordSets.ForEach(rs => {
                newLayerPanel = (GameObject) Instantiate(layerPanelPrefab, transform);
                string displayName = String.IsNullOrEmpty(rs.DisplayName) ? $"ID: {rs.Id}" : rs.DisplayName;
                layersMap.Add(rs.Id, displayName);
                newLayerPanel.GetComponentInChildren<LayerUIPanel>().layerDisplayName = displayName;
                newLayerPanel.transform.SetParent(layersScrollView.transform, false);
            });
        }

        private void OnStartEditSession() {
        }

        private void OnEndEditSession(bool saved) {
        }
    }
}