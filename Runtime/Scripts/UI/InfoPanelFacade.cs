/* MIT License

Copyright (c) 2020 - 21 Runette Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (and subsidiary notices) shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */

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
