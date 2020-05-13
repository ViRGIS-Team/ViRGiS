using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Virgis {

    public class LayerUIPanel : MonoBehaviour {
        public Toggle editLayerToggle;
        public Toggle viewLayerToggle;

        private ILayer _layer;

        // Start is called before the first frame update
        void Start() {

        }

        // Update is called once per frame
        void Update() {

        }

        public ILayer layer {
            get => _layer;
            set {
                _layer = value;
                // layer name to be displayed is RecordSet.DisplayName, 
                // or RecordSet.Id as fallback
                string displayName = String.IsNullOrEmpty(_layer.GetMetadata().DisplayName) 
                    ? $"ID: {_layer.GetMetadata().Id}" 
                    : _layer.GetMetadata().DisplayName;
                viewLayerToggle.GetComponentInChildren<Text>().text = displayName;
            }
        }
    }
}