
// copyright Runette Software Ltd, 2020. All rights reserved﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DatapointSphere : MonoBehaviour
{

    public Color color;
    public Color anticolor;
    private Renderer thisRenderer;
    public Vector3 position;
    public Transform viewer;

    public string gisId;
    public IDictionary<string, object> gisProperties;

    private int id;
    // Start is called before the first frame update
    void Start()
    {
        thisRenderer = GetComponent<Renderer>();
        if (color != null)
        {
            thisRenderer.material.color = color;
        }

        position = gameObject.transform.position;
        viewer = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.LookAt(viewer);
    }

    void Selected(int button)
    {
        thisRenderer.material.color = anticolor;
        if (button != 100)
        {
            gameObject.transform.parent.gameObject.SendMessageUpwards("Selected", button, SendMessageOptions.DontRequireReceiver);
        }
    }

    void UnSelected(int button)
    {
        thisRenderer.material.color = color;
        if (button != 100)
        {
            gameObject.transform.parent.gameObject.SendMessageUpwards("UnSelected", button, SendMessageOptions.DontRequireReceiver);
        }

    }

    void SetColor(Color newColor)
    {
        color = newColor;
        anticolor = Color.white - newColor;
        if (thisRenderer)
        {
            thisRenderer.material.color = color;
        }
    }

    void MoveTo(Vector3 newPos)
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
