using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{

    public GameObject menu;

    // Start is called before the first frame update
    void Start()
    {
        print("start camera manager");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) {
            bool isActive = menu.activeSelf;
            menu.SetActive(!isActive);
        }
    }
}
