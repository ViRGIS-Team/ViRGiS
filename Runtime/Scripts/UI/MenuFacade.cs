using UnityEngine;
using UnityEngine.UI;
using System;

namespace Virgis {

    /// <summary>
    /// MenuFacade is the mediator for all components within the Menus GO.
    /// </summary>
    /// 
    /// For desktop Scene, the Menus GO is used in:
    /// 1) InputMapping
    /// 2) Layers UI GO
    public class MenuFacade : MonoBehaviour {

        public Button startEditButton;
        public Button stopSaveEditButton;
        public Button stopDiscardEditButton;
        public Button fileButton;
        public Button quitButton;
        public GameObject layersUI;
        public GameObject startMenu;

        private AppState _appState;
        private IDisposable startsub;
        private IDisposable stopsub;

        // Start is called before the first frame update
        void Start() {
            _appState = AppState.instance;
            if (_appState.editSession.IsActive()) {
                startEditButton.interactable = false;
                stopSaveEditButton.interactable = true;
                stopDiscardEditButton.interactable = true;
            } else {
                startEditButton.interactable = true;
                stopSaveEditButton.interactable = false;
                stopDiscardEditButton.interactable = false;
            }

            startsub = _appState.editSession.StartEvent.Subscribe(OnEditSessionStart);
            stopsub = _appState.editSession.EndEvent.Subscribe(OnEditSessionEnd);
        }

        private void OnDestroy() {
            startsub.Dispose();
            stopsub.Dispose();
        }

        public void Visible(bool thisEvent) {
            bool isActive = gameObject.activeSelf;
            if (isActive) {
                gameObject.SetActive(false);
            } else {
                gameObject.SetActive(true);
            }
        }

        public void OnShowLayersButtonClicked() {
            gameObject.SetActive(false);
            layersUI.SetActive(true);
        }

        public void OnStartEditButtonClicked() {
            _appState.StartEditSession();
        }

        public void OnStopSaveEditButtonClicked() {
            _appState.StopSaveEditSession();
        }

        public void OnStopDiscardEditButtonClicked() {
            _appState.StopDiscardEditSession();
        }

        public void onFileClicked() {
            startMenu.SetActive(true);
        }

        // Changes the state of menu buttons when edit session starts.
        // 1) Disable Start Edit button
        // 2) Enable both Stop Edit buttons
        //
        // This method is triggered when:
        // 1) StartEdit action is triggered
        // 2) Start Edit button is clicked
        private void OnEditSessionStart(bool ignore) {
            startEditButton.interactable = false;
            stopSaveEditButton.interactable = true;
            stopDiscardEditButton.interactable = true;
            fileButton.interactable = false;
            quitButton.interactable = false;
        }

        // Changes the state of menu buttons when edit session ends.
        // 1) Enable Start Edit button
        // 2) Disable both Stop Edit buttons
        //
        // This method is triggered when:
        // 1) EndEdit action is triggered
        // 2) One of the Stop Edit buttons is clicked
        private void OnEditSessionEnd(bool saved) {
            startEditButton.interactable = true;
            stopSaveEditButton.interactable = false;
            stopDiscardEditButton.interactable = false;
            fileButton.interactable = true;
            quitButton.interactable = true;
        }

    }
}