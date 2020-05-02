using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuFacade : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Visible(bool thisEvent) 
    {
        gameObject.SetActive(thisEvent);
        Debug.Log(thisEvent);
    }

    public void HandleKeyInput(InputAction.CallbackContext context) {
        InputAction action = context.action;
        if (action.name == "ShowMenu") {
            bool isActive = gameObject.activeSelf;
            gameObject.SetActive(!isActive);
        }
    }

}
