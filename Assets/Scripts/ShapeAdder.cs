using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShapeAdder : MonoBehaviour
{
    public GameObject cubePrefab;
    public GameObject theCube;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LeftTriggerPressed(bool activate) {
        print($"LeftTriggerPressed: activate = {activate}");
        Vector3 pos = theCube.transform.position;
        Quaternion rot = theCube.transform.rotation;
        GameObject newCube = Instantiate(cubePrefab, pos, rot);
    }

    public void LeftTriggerReleased(bool activate) {
        print($"LeftTriggerReleased: activate = {activate}");
    }

}
