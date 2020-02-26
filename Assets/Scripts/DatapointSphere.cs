using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DatapointSphere : MonoBehaviour
{

     public Color color;
     public Color anticolor;
     private Renderer thisRenderer;

     private int id;
    // Start is called before the first frame update
    void Start()
    {
         thisRenderer = GetComponent<Renderer>();
         if (color != null) {
             thisRenderer.material.color = color;
         }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

     void Selected(bool selected) {
        if (selected) {
            thisRenderer.material.color = anticolor;
        } else {
            thisRenderer.material.color = color;
        }
    }

    void SetColor (Color newColor) {
        color = newColor;
        anticolor = Color.white - newColor;
        if (thisRenderer != null) {
            thisRenderer.material.color = color;
        }
    }

    void MoveTo (Vector3 newPos) {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.MovePosition(newPos);
        MoveArgs args = new MoveArgs();
        args.id = id;
        args.pos = newPos;
        SendMessageUpwards("VertexMove", args, SendMessageOptions.DontRequireReceiver);
    }

    public void SetId(int value) {
        id = value;
    }

    public void EditEnd()
    {
        SendMessageUpwards("EditEndAction", SendMessageOptions.DontRequireReceiver);
    }
}
