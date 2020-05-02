using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// AppState is a global singleton object that stores
// app states, such as EditSession, etc.
//
// Singleton pattern taken from https://learn.unity.com/tutorial/level-generation
public class AppState : MonoBehaviour
{
    public static AppState instance = null;

    private bool _editSession;

    private UnityEvent _startEditSessionEvent;
    private UnityEvent _endEditSessionEvent;

    void Awake() {
        print("AppState awakens");
        if (instance == null) {
            print("AppState instance assigned");
            instance = this;
        } else if (instance != this) {
            // there cannot be another instance
            print("AppState found another instance");
            Destroy(gameObject);
        }

        DontDestroyOnLoad(gameObject);
        InitApp();
    }

    public bool InEditSession() {
        return _editSession;
    }

    public void StartEditSession() {
        if (!this._editSession) {
            _editSession = true;
            _startEditSessionEvent.Invoke();
        }
    }

    public void StopSaveEditSession() {
        if (this._editSession) {
            _editSession = false;
            _endEditSessionEvent.Invoke();
            // save edits
            print("Saving changes");
            // throw exception if save failed
        }
    }

    public void StopDiscardEditSession() {
        if (this._editSession) {
            _editSession = false;
            _endEditSessionEvent.Invoke();
            // discard edits
            print("Discarding changes");
        }
    }

    public void AddStartEditSessionListener(UnityAction action) {
        _startEditSessionEvent.AddListener(action);
    }

    public void AddEndEditSessionListener(UnityAction action) {
        _endEditSessionEvent.AddListener(action);
    }

    private void InitApp() {
        _editSession = false;
        _startEditSessionEvent = new UnityEvent();
        _endEditSessionEvent = new UnityEvent();
    }

}
