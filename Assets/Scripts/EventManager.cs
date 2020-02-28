using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventManager : MonoBehaviour
{

    public UnityEvent OnEditsessionEnd;
    // Start is called before the first frame update
    void Start()
    {
        OnEditsessionEnd = new UnityEvent();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
