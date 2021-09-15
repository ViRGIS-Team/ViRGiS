using UnityEngine;
using UnityEngine.UI;
using UniRx;


namespace Virgis {

    public class InfoPanelFacade : MonoBehaviour {
        public bool rightInfoPanel;
        public bool leftInfoPanel;
        public Text textBox;

        private AppState m_appState;
        private string m_lastText = "";
        private bool m_active = true;


        // Start is called before the first frame update
        void Start() {
            m_appState = AppState.instance;
            if (leftInfoPanel) {
                m_appState.Info.Event.Subscribe(UpdateText);
                m_appState.ButtonStatus.Event.Subscribe(ButtonChange);
            }
            gameObject.SetActive(false);
        }

        private void UpdateText( string text) {
            if (m_active) {
                gameObject.SetActive(text != "" || (m_appState.ButtonStatus.isLhTrigger && !m_appState.editSession.IsActive()));
                m_lastText = text;
                if (!m_appState.ButtonStatus.isLhTrigger || text != "")
                    textBox.text = text;
            }
        }

        private void ButtonChange(ButtonStatus status) {
            if (m_active && !status.activate && status.SelectionType == SelectionType.INFO && ! m_appState.editSession.IsActive()) {
                gameObject.SetActive(false);
                textBox.text = m_lastText;
            }
        }

        public void SetActive(bool status) {
            m_active = status;
            if (!status)
                gameObject.SetActive(false);
        }
        
    }
}
