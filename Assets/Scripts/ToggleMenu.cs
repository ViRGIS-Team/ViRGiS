using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleMenu : MonoBehaviour
{
    public GameObject Menu;
    public bool MenuStatus;
    private Button mainButton;

    void Start()
    {
        Menu.SetActive(false);
        mainButton = gameObject.GetComponent<Button>();
    }

    void MenuShow()
    {
        Menu.SetActive(false);
    }

    void MenuHide()
    {
        Menu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
