using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EditModeUI : MonoBehaviour
{ 
    public Toggle EditOn;
    public Toggle EditOff;

    // Start is called before the first frame update
    void Start() {
        // obtain EventManager object
        GameObject Map = Global.Map;
        EventManager eventManager;
        do {
            eventManager = Map.GetComponent<EventManager>();
            if (eventManager == null) {
                new WaitForSeconds(.5f);
            };
        } while (eventManager == null);
        // listen to end/start event of EditSession
        eventManager.EditSessionEndEvent.AddListener(OnEditSessionEnd);
        eventManager.EditSessionStartEvent.AddListener(OnEditSessionStart);
    }

    //void Enabled() {
    //    print("EditModeUI.Enabled");
    //    EditOn.isOn = Global.EditSession;
    //}

    // Select the EditOn toggle button and invoke StartEdit event.
    public void OnEditOnClick(BaseEventData eventData) {
        print("EditModeUI.OnEditOnClick");
        EventManager eventManager = Global.Map.GetComponent<EventManager>();
        eventManager.EditSessionStartEvent.Invoke();
        EditOn.isOn = true;
    }

    // Select the EditOff toggle button and invoke EndEdit event.
    public void OnEditOffClick(BaseEventData eventData) {
        print("EditModeUI.OnEditOffClick");
        EventManager eventManager = Global.Map.GetComponent<EventManager>();
        eventManager.EditSessionEndEvent.Invoke();
        EditOff.isOn = true;
    }

    // Select EditOff toggle button when EditSession ends.
    private void OnEditSessionEnd() {
        EditOff.isOn = true;
    }

    // Select EditOn toggle button when EditSession starts.
    private void OnEditSessionStart() {
        EditOn.isOn = true;
    }
}
