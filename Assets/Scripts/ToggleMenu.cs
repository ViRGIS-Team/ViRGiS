using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleMenu : MonoBehaviour
{
    public GameObject HideMenu;
    public GameObject ShowMenu;
    public Button ToggleShowBtn;
    public Button ToggleHideBtn;
    public bool MenuStatus;

    void Start()
    {
        HideMenu.SetActive(true);
        ShowMenu.SetActive(false);
    }

    void MenuShowing()
    {
        HideMenu.SetActive(false);
        ShowMenu.SetActive(true);
    }

    void MenuHidden()
    {
        HideMenu.SetActive(true);
        ShowMenu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        Button btnShow = ToggleShowBtn.GetComponent<Button>();
        Button btnHide = ToggleHideBtn.GetComponent<Button>();
        btnShow.onClick.AddListener(MenuShowing);
        btnHide.onClick.AddListener(MenuHidden);
    }
}
