using UnityEngine;
using UnityEngine.UI;
using System;


namespace Virgis {

    public class InfoPanelFacade : MonoBehaviour {
        public bool rightInfoPanel;
        public bool leftInfoPanel;
        public Text textBox;

        private AppState appState;
        private string lastText = "";


        // Start is called before the first frame update
        void Start() {
            appState = AppState.instance;
            if (leftInfoPanel) {
                appState.Info.Event.Subscribe(UpdateText);
                appState.ButtonStatus.Event.Subscribe(ButtonChange);
        }
  }

        private void UpdateText( string text) {
            gameObject.SetActive (text != "" ||  appState.ButtonStatus.isLhTrigger);
            lastText = text;
            if (! appState.ButtonStatus.isLhTrigger || text != "") textBox.text = text;
        }

        private void ButtonChange(ButtonStatus status) {
            if (!status.activate && status.SelectionType == SelectionType.INFO) {
                gameObject.SetActive(false);
                textBox.text = lastText;
            }
        }
        
    }
}
