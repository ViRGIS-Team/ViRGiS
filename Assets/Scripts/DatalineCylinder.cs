// copyright Runette Software Ltd, 2020. All rights reserved
// parts from  https://answers.unity.com/questions/8338/how-to-draw-a-line-using-script.html

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.ObjectModel;
using System;
using GeoJSON.Net.Geometry;
using Mapbox.Unity.Map;
using Project;


public class DatalineCylinder : MonoBehaviour, IVirgisComponent
{
    public Color color;
    public Color anticolor;
    private Renderer thisRenderer;
    private bool BlockMove = false;
    private bool Lr = false;

    public GameObject CylinderObject;

    public string gisId;
    public IDictionary<string, object> gisProperties;

    // Start is called before the first frame update
    void Start()
    {
    }

    public void SetColor(Color newColor)
    {
        color = newColor;
        anticolor = Color.white - newColor;
        if (thisRenderer != null)
        {
            thisRenderer.material.SetColor("_BaseColor", color);
        }
    }

    public void VertexMove(MoveArgs data)
    {
        if (data.id >= 0)
        {
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                GameObject go = gameObject.transform.GetChild(i).gameObject;
                CylinderLine goFunc = go.GetComponent<CylinderLine>();
                if (goFunc != null && goFunc.vStart == data.id)
                {
                    goFunc.MoveStart(data.pos);
                }
                else if (goFunc != null && goFunc.vEnd == data.id)
                {
                    goFunc.MoveEnd(data.pos);
                }
            }
        }
    }

    public void Draw(LineString lineIn, Unit symbology, GameObject LinePrefab, GameObject HandlePrefab, AbstractMap _map)
    {

        Vector3[] line = Tools.LS2Vect(lineIn, _map);
        Lr = lineIn.IsLinearRing();

        int i = 0;
        foreach (Vector3 vertex in line)
        {
            if (!(i + 1 == line.Length && Lr))
            {
                GameObject handle = Instantiate(HandlePrefab, vertex, Quaternion.identity);
                handle.transform.parent = gameObject.transform;
                handle.SendMessage("SetId", i);
                handle.SendMessage("SetColor", (Color)symbology.Color);
                handle.transform.localScale = symbology.Transform.Scale;
            }
            if (i + 1 != line.Length)
            {
                GameObject lineSegment = Instantiate(CylinderObject, vertex, Quaternion.identity);
                lineSegment.transform.parent = gameObject.transform;
                CylinderLine com = lineSegment.GetComponent<CylinderLine>();
                com.SetId(i);
                com.Draw(vertex, line[i + 1], i, i+1, 0.5f);
                com.SetColor((Color)symbology.Color);
                if (i + 2 == line.Length && Lr) com.vEnd = 0;
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
        DatapointSphere[] data = GetHandles();
        Vector3[] result = new Vector3[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            DatapointSphere datum = data[i];
            result[i] = datum.position;
        }
        if(Lr)
        {
            Array.Resize<Vector3>(ref result, result.Length + 1);
            result[result.Length - 1] = result[0];
        }
        return result;
    }

    public DatapointSphere[] GetHandles()
    {
        return gameObject.GetComponentsInChildren<DatapointSphere>();
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
        if (button != 100)
        {
            gameObject.BroadcastMessage("UnSelected", 100, SendMessageOptions.DontRequireReceiver);
            BlockMove = false;
        }
    }

    public void Translate(MoveArgs args)
    {
        if (BlockMove)
        {
            gameObject.BroadcastMessage("TranslateHandle", args, SendMessageOptions.DontRequireReceiver);
        }
    }

    public string GetWkt()
    {
        string result = "LINESTRING Z";
        result += GetWktCoords();
        return result;
    }

    public string GetWktCoords()
    {

        string result = "(";
        foreach (Vector3 vertex in GetVertices())
        {
            result += "{vertex.x} {vertex.y} {vertex.z},";
        }
        result.TrimEnd(',');
        result += ")";
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
