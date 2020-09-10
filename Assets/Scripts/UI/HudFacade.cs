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
        AppState.instance.AddEndEditSessionListener(OnEditSessionEnd);
        AppState.instance.AddStartEditSessionListener(OnEditSessionStart);
        AppState.instance.AddZoomChangeListerner(OnZoomChanged);
        HudLeftText.text = $"1 : {AppState.instance.GetScale()}";
    }

    private void Update() {
        Vector3 current = AppState.instance.Orientation;
        current.y = 0;
        double angle = Math.Ceiling(Vector3.SignedAngle(AppState.instance.map.transform.forward, current, AppState.instance.map.transform.up));
        if (angle < 0) angle = 360 + angle;
        HudCentreText.text = angle.ToString();
    }

    public void OnEditSessionStart() {
        HudRightText.text = "Editing";
    }

    public void OnEditSessionEnd(bool saved) {
        HudRightText.text = "";
    }

    public void OnZoomChanged(float scale) {
        HudLeftText.text = $"1 : {Mathf.Round(scale *10) / 10}";
    }
}
