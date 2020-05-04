using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Virgis {

    public class LayersPanel : MonoBehaviour {
        public GameObject layerPanelPrefab;

        // Start is called before the first frame update
        void Start() {
            GameObject newLayerPanel;
            for (int i = 0; i < 5; i++) {
                newLayerPanel = (GameObject)Instantiate(layerPanelPrefab, transform);
                newLayerPanel.GetComponent<Image>().color = Random.ColorHSV();
            }
        }

        // Update is called once per frame
        void Update() {

        }
    }
}