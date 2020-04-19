// copyright Runette Software Ltd, 2020. All rights reserved
// parts from  https://answers.unity.com/questions/8338/how-to-draw-a-line-using-script.html

using System.Collections.Generic;
using UnityEngine;
using System;
using GeoJSON.Net.Geometry;
using Mapbox.Unity.Map;
using Project;

/// <summary>
/// Controls and Instance of a Line Component
/// </summary>
public class DatalineCylinder : MonoBehaviour, IVirgisComponent
{
    public Color color; // color for the line
    public Color anticolor; // color for the vertces when selected
    private bool BlockMove = false; // is this line in a block-move state
    private bool Lr = false; // is this line a Linear Ring - i.e. used to define a polygon

    public GameObject CylinderObject; 

    public string gisId; // the ID for this line from the geoJSON
    public IDictionary<string, object> gisProperties; // the properties for this 

    /// <summary>
    /// Sets the Color of the line
    /// </summary>
    /// <param name="newColor"></param>
    public void SetColor(Color newColor)
    {
        BroadcastMessage("SetColor", newColor, SendMessageOptions.DontRequireReceiver);
    }

    /// <summary>
    /// Called when a child Vertex moves to the point in the MoveArgs - which is in World Coordinates
    /// </summary>
    /// <param name="data">MOveArgs</param>
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

    /// <summary>
    /// Called to draw the line
    /// </summary>
    /// <param name="lineIn"> A LineString</param>
    /// <param name="symbology">The symbo,logy to be applied to the loine</param>
    /// <param name="LinePrefab"> The prefab to be used for the line</param>
    /// <param name="HandlePrefab"> The prefab to be used for the handle</param>
    public void Draw(LineString lineIn, Unit symbology, GameObject LinePrefab, GameObject HandlePrefab)
    {
        AbstractMap _map = Global._map;
        Vector3[] line = Tools.LS2Vect(lineIn);
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

    /// <summary>
    /// called to get the verteces of the LineString
    /// </summary>
    /// <returns>Vector3[] of verteces</returns>
    public Vector3[] GetVerteces()
    {
        DatapointSphere[] data = GetHandles();
        Vector3[] result = new Vector3[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            DatapointSphere datum = data[i];
            result[i] = datum.transform.position;
        }
        if(Lr)
        {
            Array.Resize<Vector3>(ref result, result.Length + 1);
            result[result.Length - 1] = result[0];
        }
        return result;
    }

    /// <summary>
    /// called to get the handle ViRGIS Components for the Line
    /// </summary>
    /// <returns> DatapointSphere[]</returns>
    public DatapointSphere[] GetHandles()
    {
        return gameObject.GetComponentsInChildren<DatapointSphere>();
    }

    /// <summary>
    /// Called when a child component is selected
    /// </summary>
    /// <param name="button"> SelectionTypes </param>
    public void Selected(SelectionTypes button)
    {
        if (button == SelectionTypes.SELECTALL)
        {
            gameObject.BroadcastMessage("Selected", SelectionTypes.BROADCAST, SendMessageOptions.DontRequireReceiver);
            BlockMove = true;
        }
    }

    /// <summary>
    /// Called when a child component is unselected
    /// </summary>
    /// <param name="button"> SelectionTypes</param>
    public void UnSelected(SelectionTypes button)
    {
        if (button != SelectionTypes.BROADCAST)
        {
            gameObject.BroadcastMessage("UnSelected", SelectionTypes.BROADCAST, SendMessageOptions.DontRequireReceiver);
            BlockMove = false;
        }
    }

    /// <summary>
    /// Called when a child component is translated by User action
    /// </summary>
    /// <param name="args">MoveArgs</param>
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
        foreach (Vector3 vertex in GetVerteces())
        {
            result += "{vertex.x} {vertex.y} {vertex.z},";
        }
        result.TrimEnd(',');
        result += ")";
        return result;
    }

    /// <summary>
    /// Callled on an ExitEditSession event
    /// </summary>
    public void EditEnd()
    {

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
