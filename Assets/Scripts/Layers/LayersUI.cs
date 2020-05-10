using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Virgis {

    // LayersUI is the mediator for all components within the Layers UI GO.
    // For desktop Scene, the Layers UI GO is used in:
    // 1) InputMapping
    // 2) Menus GO
    public class LayersUI : MonoBehaviour {
        public GameObject layersScrollView;
        public GameObject layerPanelPrefab;
        public GameObject menus;

        // Start is called before the first frame update
        void Start() {
            GameObject newLayerPanel;
            for (int i = 0; i < 10; i++) {
                newLayerPanel = (GameObject)Instantiate(layerPanelPrefab, transform);
                newLayerPanel.GetComponentInChildren<LayerUIPanel>().layerDisplayName = $"Layer {i+1}";
                newLayerPanel.transform.SetParent(layersScrollView.transform, false);
            }
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

    }
}