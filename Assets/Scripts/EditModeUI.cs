using UnityEngine;
using UnityEngine.UI;

namespace Virgis {
    public class EditModeUI : MonoBehaviour {
        public Toggle snapGridToggle;
        public Toggle snapAnchorToggle;

        private AppState _appState;

        // Start is called before the first frame update
        void Start() {
            _appState = AppState.instance;

            // There is a bug in Unity where if you set OnValueChange event
            // in Unity inspector, the method will be called with the same
            // boolean value (either true/false depending on what you set)
            // regardless of the actual Toggle value.
            // The workaround is to set onValueChange listener in the script.
            snapGridToggle.onValueChanged.AddListener(OnSnapGridToggleValueChanged);
            snapAnchorToggle.onValueChanged.AddListener(OnSnapAnchorToggleValueChanged);
        }

        // Select the EditOn toggle button and invoke StartEdit event.
        public void OnSnapGridToggleValueChanged(bool enabled) {
            if (enabled) {
                if (snapAnchorToggle.isOn) {
                    snapAnchorToggle.isOn = false;
                }
                _appState.editSession.mode = EditSession.EditMode.SnapGrid;
                print($"Edit mode = {_appState.editSession.mode}");
            } else {
                if (!snapAnchorToggle.isOn) {
                    _appState.editSession.mode = EditSession.EditMode.None;
                    print($"Edit mode = {_appState.editSession.mode}");
                }
            }
        }

        // Select the EditOff toggle button and invoke EndEdit event.
        public void OnSnapAnchorToggleValueChanged(bool enabled) {
            if (enabled) {
                if (snapGridToggle.isOn) {
                    snapGridToggle.isOn = false;
                }
                _appState.editSession.mode = EditSession.EditMode.SnapAnchor;
                print($"Edit mode = {_appState.editSession.mode}");
            } else {
                if (!snapGridToggle.isOn) {
                    _appState.editSession.mode = EditSession.EditMode.None;
                    print($"Edit mode = {_appState.editSession.mode}");
                }
            }
        }
    }
}