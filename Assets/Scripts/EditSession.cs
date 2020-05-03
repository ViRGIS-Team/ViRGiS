using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EditSession
{
    public enum EditMode {
        SnapGrid, SnapAnchor
    }

    private bool _active;
    private UnityEvent _startEditSessionEvent;
    private UnityEvent _endEditSessionEvent;
    private EditMode _editMode;

    public EditSession() {
        _active = false;
        _startEditSessionEvent = new UnityEvent();
        _endEditSessionEvent = new UnityEvent();
        _editMode = EditMode.SnapGrid;
    }

    public bool IsActive() {
        return _active;
    }

    public void Start() {
        if (!_active) {
            _active = true;
            _startEditSessionEvent.Invoke();
        }
    }

    public void StopAndSave() {
        if (_active) {
            _active = false;
            _endEditSessionEvent.Invoke();
            // save edits
            // throw exception if save failed
        }
    }

    public void StopAndDiscard() {
        if (_active) {
            _active = false;
            _endEditSessionEvent.Invoke();
            // discard edits
        }
    }

    public void AddStartEditSessionListener(UnityAction action) {
        _startEditSessionEvent.AddListener(action);
    }

    public void AddEndEditSessionListener(UnityAction action) {
        _endEditSessionEvent.AddListener(action);
    }

    public EditMode mode {
        get => _editMode;
        set {
            _editMode = value;
        }
    }

}
