// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventManager : MonoBehaviour
{

    public UnityEvent EditSessionEndEvent;
    public UnityEvent EditSessionStartEvent;

    // Start is called before the first frame update
    void Start()
    {
        EditSessionEndEvent = new UnityEvent();
        EditSessionStartEvent = new UnityEvent();

        EditSessionStartEvent.AddListener(StartEditSession);
        EditSessionEndEvent.AddListener(EndEditSession);
    }

    // Update is called once per frame
    void Update()
    {

    }

    // TODO: Create a separate object to manage EditSession state and configs.

    // Start EditSession
    private void StartEditSession() {
        Global.EditSession = true;
    }

    // End EditSession
    private void EndEditSession() {
        Global.EditSession = false;
    }
}
