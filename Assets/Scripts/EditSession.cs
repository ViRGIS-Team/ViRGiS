using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Virgis {

    // EndEditSessionEvent is triggered whenever user ends
    // an edit session. This event carries one boolean parameter:
    // true if the edit session is stopped and saved,
    // false if the edit session is stopped and discarded.
    [System.Serializable]
    public class EndEditSessionEvent : UnityEvent<bool> {
    }

    public class EditSession {
        public enum EditMode {
            None, SnapGrid, SnapAnchor
        }

        private bool _active;
        private UnityEvent _startEditSessionEvent;
        private EndEditSessionEvent _endEditSessionEvent;
        private EditMode _editMode;

        public EditSession() {
            _active = false;
            _startEditSessionEvent = new UnityEvent();
            _endEditSessionEvent = new EndEditSessionEvent();
            _editMode = EditMode.None;
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

        public EditMode mode {
            get => _editMode;
            set {
                _editMode = value;
            }
        }

    }
}