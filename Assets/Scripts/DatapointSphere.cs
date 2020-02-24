using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DatapointSphere : MonoBehaviour
{

     public Color color;
     public Color anticolor;
     private Renderer thisRenderer;
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
}
