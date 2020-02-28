// copyright Runette Software Ltd, 2020. All rights reserved
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Datapolygon : MonoBehaviour
{

    private bool BlockMove = false;
    public string gisId;
    public IDictionary<string, object> gisProperties;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Selected(int button)
    {
        if (button == 1)
        {
            gameObject.BroadcastMessage("Selected", 100, SendMessageOptions.DontRequireReceiver);
            BlockMove = true;
        }
    }

    public void UnSelected(int button)
    {
        DatalineCylinder perimeter = gameObject.GetComponentInChildren<DatalineCylinder>();
        Vector3[] vertices = perimeter.GetVertices();
        Vector3 center = Poly.FindCenter(vertices);
        GameObject shapeObject = gameObject.transform.Find("Polygon Shape").gameObject;
        shapeObject.Destroy();
        Poly.Draw(vertices, center, gameObject, new Material(Shader.Find("Specular")) );
    }
}
