using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Virgis {

    public class LayersUI : MonoBehaviour {
        public GameObject layersScrollView;
        public GameObject layerPanelPrefab;

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
                gameObject.SetActive(!isActive);
            }
        }

    }
}