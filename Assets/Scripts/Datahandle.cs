using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Datahandle : MonoBehaviour
{

    public Color color;
    public Color anticolor;
    public Vector3 position;

    public int id;
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
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Selected(int button)
    {
        thisRenderer.material.SetColor("_BaseColor", anticolor);
        if (button != 100)
        {
            gameObject.transform.parent.gameObject.SendMessageUpwards("Selected", button, SendMessageOptions.DontRequireReceiver);
        }
    }

    void UnSelected(int button)
    {
        thisRenderer.material.SetColor("_BaseColor", color);
        if (button != 100)
        {
            gameObject.transform.parent.gameObject.SendMessageUpwards("UnSelected", button, SendMessageOptions.DontRequireReceiver);
        }

    }

    void SetColor(Color newColor)
    {
        color = newColor;
        anticolor = Color.white - newColor;
        Renderer thisRenderer = GetComponent<Renderer>();
        if (thisRenderer)
        {
            thisRenderer.material.SetColor("_BaseColor", color);
        }
    }

    void MoveTo(Vector3 newPos)
    {
        MoveArgs args = new MoveArgs();
        args.rotate = Quaternion.identity;
        if (id == 0)
        {
            args.translate = newPos - gameObject.transform.position;
        } else
        {
            Vector3 newDir = newPos - gameObject.transform.parent.position;
            args.scale =newDir.magnitude/(gameObject.transform.position - gameObject.transform.parent.position).magnitude;
            args.rotate = Quaternion.FromToRotation(gameObject.transform.parent.right, newDir);
        }
        args.id = id;
        SendMessageUpwards("Translate", args, SendMessageOptions.DontRequireReceiver);
    }


    public void SetId(int value)
    {
        id = value;
    }

    public void EditEnd()
    {

    }
}
