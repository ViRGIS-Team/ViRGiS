using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.ObjectModel;
using System;
using GeoJSON.Net.Geometry;
using Mapbox.Unity.Map;

public class DatalineCylinder : MonoBehaviour
{
    public Color color;
     public Color anticolor;
     private Renderer thisRenderer;

    public GameObject CylinderObject;

    // Start is called before the first frame update
    void Start()
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

    void VertexMove(MoveArgs data) {
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            GameObject go = gameObject.transform.GetChild(i).gameObject;
            CylinderLine goFunc = go.GetComponent<CylinderLine>();
            if (goFunc != null && goFunc.id == data.id)
            {
                goFunc.MoveStart(data.pos);
            } else if (goFunc != null && goFunc.id == data.id - 1)
            {
                goFunc.MoveEnd(data.pos);
            }
        }
    } 

    public void Draw(LineString lineIn, Color color, float width, GameObject LinePrefab, GameObject HandlePrefab, AbstractMap _map)
    {
        Vector3[] line = Tools.LS2Vect(lineIn, _map);

        int i = 0;
        foreach (Vector3 vertex in line)
        {
            GameObject handle = Instantiate(HandlePrefab, vertex, Quaternion.identity);
            handle.transform.parent = gameObject.transform;
            handle.SendMessage("SetId", i);
            if (i + 1 != line.Length)
            {
                GameObject lineSegment = Instantiate(CylinderObject, vertex, Quaternion.identity);
                lineSegment.transform.parent = gameObject.transform;
                lineSegment.GetComponent<CylinderLine>().SetId(i);
                lineSegment.GetComponent<CylinderLine>().Draw(vertex, line[i + 1], 0.5f);
            }
            i++;
        }
        GameObject labelObject = new GameObject();
        labelObject.transform.parent = gameObject.transform;
        labelObject.transform.localPosition = new Vector3(0, 0, 0);
        labelObject.AddComponent(typeof(TextMesh));
        gameObject.transform.parent = gameObject.transform;

    }

    public Vector3[] GetVertices()
    {
        DatapointSphere[] data = gameObject.GetComponentsInChildren<DatapointSphere>();
        Vector3[] result = new Vector3[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            DatapointSphere datum = data[i];
            result[i] = datum.position;
        }
        return result;

    }

    /* static public Gradient ColorGrad(Color color1)
    {
        float alpha = 1.0f;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color1, 0.5f) },
            new GradientAlphaKey[] { new GradientAlphaKey(alpha, 0.5f) }
        );
        return gradient;
    } */
}
