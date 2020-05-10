using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Virgis {

    public class LayerUIPanel : MonoBehaviour {
        public Toggle editLayerToggle;
        public Toggle viewLayerToggle;

        private string _layerDisplayName;

        // Start is called before the first frame update
        void Start() {

        }

        // Update is called once per frame
        void Update() {

        }

        public string layerDisplayName {
            get => _layerDisplayName;
            set {
                _layerDisplayName = value;
                viewLayerToggle.GetComponentInChildren<Text>().text = value;
            }
        }
    }
}