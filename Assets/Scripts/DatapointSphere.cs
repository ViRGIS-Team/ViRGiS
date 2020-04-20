
// copyright Runette Software Ltd, 2020. All rights reserved﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Controls an instance of a data pointor handle
/// </summary>
public class DatapointSphere : MonoBehaviour, IVirgisComponent
{

    public Color color; // color of the marker
    public Color anticolor; // color of the market when selected
    public string gisId; // ID of this market in the geoJSON
    public IDictionary<string, object> gisProperties; //  geoJSON properties of this marker

    private int id; // internal ID for this marker - used when it is part of a larger structure
    private Renderer thisRenderer; // convenience link to the rendere for this marker
    private Transform label; //  Go of the label or billboard


    void Start()
    {
        thisRenderer = GetComponent<Renderer>();
        if (color != null)
        {
            thisRenderer.material.SetColor("_BaseColor", color);
        }
        if (transform.childCount > 0) label = transform.GetChild(0);
    }

    /// <summary>
    /// Every frame - realign the billboard
    /// </summary>
    void Update()
    {
        if (label) label.LookAt(Global.mainCamera.transform);

    }

    /// <summary>
    ///  On selected - change color and send message up the entity tree
    /// </summary>
    /// <param name="button">SelecetionType Identifies the user action type that led to selection</param>
    public void Selected(SelectionTypes button)
    {
         thisRenderer.material.SetColor("_BaseColor", anticolor);
        if (button != SelectionTypes.BROADCAST)
        {
            gameObject.transform.parent.gameObject.SendMessageUpwards("Selected", button, SendMessageOptions.DontRequireReceiver);
        }
    }

    /// <summary>
    /// On unselected - change color and send message up the entity tree
    /// </summary>
    /// <param name="button">SelecetionType Identifies the user action type that led to selection</param>
    public void UnSelected(SelectionTypes button)
    {
        thisRenderer.material.SetColor("_BaseColor", color);
        if (button != SelectionTypes.BROADCAST)
        {
            gameObject.transform.parent.gameObject.SendMessageUpwards("UnSelected", button, SendMessageOptions.DontRequireReceiver);
        }

    }

    /// <summary>
    /// Set the color for the marker
    /// </summary>
    /// <param name="newColor"> Color</param>
    public void SetColor(Color newColor)
    {
        color = newColor;
        anticolor = Color.white - newColor;
        anticolor.a = color.a;
        Renderer thisRenderer = GetComponent<Renderer>();
        if (thisRenderer)
        {
            thisRenderer.material.SetColor("_BaseColor", color);
        }
    }

    /// <summary>
    /// Sent by the UI to request this marker to move.
    /// </summary>
    /// <param name="newPos">Vector3 Worldspace Location to move to </param>
    public void MoveTo(Vector3 newPos)
    {
        MoveArgs args = new MoveArgs();
        args.translate = newPos - transform.position;
        args.oldPos = transform.position;
        gameObject.transform.position = newPos;
        args.id = id;
        args.pos = transform.position;
        SendMessageUpwards("VertexMove", args, SendMessageOptions.DontRequireReceiver);
        SendMessageUpwards("Translate", args, SendMessageOptions.DontRequireReceiver);
    }

    /// <summary>
    ///  Sent by the parent entity to request this market to move as part of an entity move
    /// </summary>
    /// <param name="argsin">MoveArgs</param>
    void TranslateHandle(MoveArgs argsin)
    {
        if (argsin.id != id)
        {
            MoveArgs argsout = new MoveArgs();
            argsout.oldPos = transform.position;
            gameObject.transform.position = transform.position + argsin.translate; ;
            argsout.id = id;
            argsout.pos = transform.position;
            SendMessageUpwards("VertexMove", argsout, SendMessageOptions.DontRequireReceiver);
        }
    }

    /// <summary>
    /// Set the Id of the marker
    /// </summary>
    /// <param name="value">ID</param>
    public void SetId(int value)
    {
        id = value;
    }

    /// <summary>
    /// Callled on an ExitEditSession event
    /// </summary>
    public void EditEnd()
    {

    }
}
