using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EditModeUI : MonoBehaviour
{ 
    public Toggle EditOn;
    public Toggle EditOff;

    private AppState _appState;

    // Start is called before the first frame update
    void Start() {
        _appState = AppState.instance;
        _appState.AddStartEditSessionListener(OnEditSessionStart);
        _appState.AddEndEditSessionListener(OnEditSessionEnd);

        // There is a bug in Unity where if you set OnValueChange event
        // in Unity inspector, the method will be called with the same
        // boolean value (either true/false depending on what you set)
        // regardless of the actual Toggle value.
        // The workaround is to set onValueChange listener in the script.
        EditOn.onValueChanged.AddListener(OnEditOnClick);
        EditOff.onValueChanged.AddListener(OnEditOffClick);
    }

    // Select the EditOn toggle button and invoke StartEdit event.
    public void OnEditOnClick(bool enabled) {
        print($"EditModeUI.OnEditOnClick: enabled = {enabled}, EditOn = {EditOn.isOn}, EditOff = {EditOff.isOn}");
        // To avoid calling StartEditSession twice, 
        // we check if EditSession is still false
        // (meaning StartEditSession has not been called)
        if (enabled && !_appState.InEditSession()) _appState.StartEditSession();
    }

    // Select the EditOff toggle button and invoke EndEdit event.
    public void OnEditOffClick(bool enabled) {
        print($"EditModeUI.OnEditOffClick: enabled = {enabled}, EditOn = {EditOn.isOn}, EditOff = {EditOff.isOn}");
        // To avoid calling EndEditSession twice, 
        // we check if EditSession is still true
        // (meaning EndEditSession has not been called)
        if (enabled && _appState.InEditSession()) _appState.EndEditSession();
    }

    // Select EditOff toggle button when EditSession ends.
    // This method is triggered when:
    // 1) EndEdit action is triggered
    // 2) EditOff toggle button is clicked
    private void OnEditSessionEnd() {
        // This will trigger the onValueChange event of EditOff toggle
        EditOff.isOn = true;
    }

    // Select EditOn toggle button when EditSession starts.
    // This method is triggered when:
    // 1) StartEdit action is triggered
    // 2) EditOn toggle button is clicked
    private void OnEditSessionStart() {
        // This will trigger the onValueChange event of EditOn toggle
        EditOn.isOn = true;
    }
}
