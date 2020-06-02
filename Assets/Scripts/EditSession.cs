using UnityEngine.Events;

namespace Virgis {

    // EndEditSessionEvent is triggered whenever user ends
    // an edit session. This event carries one boolean parameter:
    // true if the edit session is stopped and saved,
    // false if the edit session is stopped and discarded.
    [System.Serializable]
    public class EndEditSessionEvent : UnityEvent<bool> {
    }

    /// <summary>
    /// EditableLayerChangedEvent is triggered during an edit session,
    /// where the editable layer has been changed to another layer.
    /// </summary>
    /// 
    /// This event carries the new editable layer.
    [System.Serializable]
    public class EditableLayerChangedEvent : UnityEvent<ILayer> {
    }

    public class EditSession {
        public enum EditMode {
            None, SnapGrid, SnapAnchor
        }

        private bool _active;
        private ILayer _editableLayer;
        private UnityEvent _startEditSessionEvent;
        private EndEditSessionEvent _endEditSessionEvent;
        private EditableLayerChangedEvent _editableLayerChangedEvent;
        private EditMode _editMode;

        public EditSession() {
            _active = false;
            _startEditSessionEvent = new UnityEvent();
            _endEditSessionEvent = new EndEditSessionEvent();
            _editableLayerChangedEvent = new EditableLayerChangedEvent();
            _editMode = EditMode.None;
        }

        public ILayer editableLayer {
            get => _editableLayer;
            set {
                value?.SetEditable(true);
                _editableLayer?.SetEditable(false);
                _editableLayer = value;
                if (_active) _editableLayerChangedEvent.Invoke(_editableLayer);
            }
        }

        public bool IsActive() {
            return _active;
        }

        public void Start() {
            if (!_active) {
                _startEditSessionEvent.Invoke();
                _active = true;
            }
        }

        public void StopAndSave() {
            if (_active) {
                _active = false;
                _endEditSessionEvent.Invoke(true);
            }
        }

        public void StopAndDiscard() {
            if (_active) {
                _active = false;
                _endEditSessionEvent.Invoke(false);
            }
        }

        public void AddStartEditSessionListener(UnityAction action) {
            _startEditSessionEvent.AddListener(action);
        }

        public void AddEndEditSessionListener(UnityAction<bool> action) {
            _endEditSessionEvent.AddListener(action);
        }

        public void AddEditableLayerChangedListener(UnityAction<ILayer> action) {
            _editableLayerChangedEvent.AddListener(action);
        }

        public EditMode mode {
            get => _editMode;
            set {
                _editMode = value;
            }
        }

    }
}