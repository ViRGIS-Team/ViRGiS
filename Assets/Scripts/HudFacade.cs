using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Virgis;

public class HudFacade : MonoBehaviour
{
    public Text HudLeftText;
    public Text HudRightText;
    
    // Start is called before the first frame update
    void Start()
    {
        AppState.instance.AddEndEditSessionListener(OnEditSessionEnd);
        AppState.instance.AddStartEditSessionListener(OnEditSessionStart);
        AppState.instance.AddZoomChangeListerner(OnZoomChanged);
        HudLeftText.text = $"1 : {AppState.instance.GetScale()}";
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
