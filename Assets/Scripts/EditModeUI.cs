using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EditModeUI : MonoBehaviour
{ 
    public Toggle EditOn;
 
    // Start is called before the first frame update
    void Enabled()
    {
        EditOn.isOn = Global.EditSession;
    }

    public void OnChange(bool thisEvent) 
    {
        if(thisEvent)
        {
            Global.EditSession = true;
        } else
        {
            if (Global.EditSession)
            {
                Global.EditSession = false;
                EventManager eventManager = Global.Map.GetComponent<EventManager>();
                eventManager.OnEditsessionEnd.Invoke();
            }
        }
    }
}
