
// copyright Runette Software Ltd, 2020. All rights reserved﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DatapointSphere : MonoBehaviour, IVirgisComponent
{

    public Color color;
    public Color anticolor;
    public Vector3 position;
    public Transform viewer;

    public string gisId;
    public IDictionary<string, object> gisProperties;

    private int id;
    private Renderer thisRenderer;
    // Start is called before the first frame update
    void Start()
    {
        thisRenderer = GetComponent<Renderer>();
        if (color != null)
        {
            thisRenderer.material.SetColor("_BaseColor", color);
        }

        position = gameObject.transform.position;
        viewer = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.LookAt(viewer);
    }

    public void Selected(int button)
    {
        thisRenderer.material.SetColor("_BaseColor", anticolor);
        if (button != 100)
        {
            gameObject.transform.parent.gameObject.SendMessageUpwards("Selected", button, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void UnSelected(int button)
    {
        thisRenderer.material.SetColor("_BaseColor", color);
        if (button != 100)
        {
            gameObject.transform.parent.gameObject.SendMessageUpwards("UnSelected", button, SendMessageOptions.DontRequireReceiver);
        }

    }

    public void SetColor(Color newColor)
    {
        color = newColor;
        anticolor = Color.white - newColor;
        Renderer thisRenderer = GetComponent<Renderer>();
        if (thisRenderer)
        {
            thisRenderer.material.SetColor("_BaseColor", color);
        }
    }

    public void MoveTo(Vector3 newPos)
    {
        MoveArgs args = new MoveArgs();
        args.translate = newPos - position;
        args.oldPos = position;
        position = newPos;
        gameObject.transform.position = position;
        args.id = id;
        args.pos = position;
        SendMessageUpwards("VertexMove", args, SendMessageOptions.DontRequireReceiver);
        SendMessageUpwards("Translate", args, SendMessageOptions.DontRequireReceiver);
    }

    void TranslateHandle(MoveArgs argsin)
    {
        if (argsin.id != id)
        {
            MoveArgs argsout = new MoveArgs();
            Vector3 newPos = position + argsin.translate;
            argsout.oldPos = position;
            position = newPos;
            gameObject.transform.position = position;
            argsout.id = id;
            argsout.pos = position;
            SendMessageUpwards("VertexMove", argsout, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void SetId(int value)
    {
        id = value;
    }

    public void EditEnd()
    {

    }
}
