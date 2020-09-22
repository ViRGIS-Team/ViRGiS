using UnityEngine;
using UnityEngine.UI;
using Virgis;
using System;

public class HudFacade : MonoBehaviour
{
    public Text HudLeftText;
    public Text HudRightText;
    public Text HudCentreText;
    
    // Start is called before the first frame update
    void Start()
    {
        AppState appState = AppState.instance;
        appState.editSession.StartEvent.Subscribe(OnEditSessionEnd);
        appState.editSession.EndEvent.Subscribe(OnEditSessionStart);
        appState.Zoom.Event.Subscribe(OnZoomChanged);
        appState.Orientation.Event.Subscribe(onOrientation);

    }

    public void onOrientation(Vector3 current) {
        current.y = 0;
        double angle = Math.Floor(Vector3.SignedAngle(AppState.instance.map.transform.forward, current, AppState.instance.map.transform.up)/5)*5;
        if (angle < 0)
            angle = 360 + angle;
        HudCentreText.text = angle.ToString();
    }

    public void OnEditSessionStart(bool ignore) {
        HudRightText.text = "Editing";
    }

    public void OnEditSessionEnd(bool saved) {
        HudRightText.text = "";
    }

    public void OnZoomChanged(float scale) {
        HudLeftText.text = $"1 : {Mathf.Round(scale *10) / 10}";
    }
}
