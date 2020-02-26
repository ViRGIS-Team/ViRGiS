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
    // Start is called before the first frame update
    void Start()
    {
         thisRenderer = GetComponent<Renderer>();
         if (color != null) {
             thisRenderer.material.color = color;
         }
    }

    // Update is called once per frame


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
        LineRenderer lr = gameObject.GetComponent<LineRenderer>();
        lr.SetPosition(data.id, data.pos);
    }

    public void Draw(LineString lineIn, Color color, float width, GameObject LinePrefab, GameObject HandlePrefab, AbstractMap _map)
    {
        ReadOnlyCollection<IPosition> vertices = lineIn.Coordinates;
        Vector3[] line = new Vector3[vertices.Count];
        float y = 1.0f;
        for (int j = 0; j < vertices.Count; j++)
        {
            line[j] = Tools.Ipos2Vect(vertices[j], y, _map);
        };
        //instantiate the prefab with coordinates defined above
        LineRenderer lr = gameObject.AddComponent<LineRenderer>();
        //lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.material.color = color;
        lr.positionCount = line.Length;
        int i = 0;
        foreach (Vector3 vertex in line)
        {
            lr.SetPosition(i, vertex);
            GameObject handle = Instantiate(HandlePrefab, vertex, Quaternion.identity);
            handle.transform.parent = gameObject.transform;
            handle.SendMessage("SetId", i);
            i++;
        }
        GameObject labelObject = new GameObject();
        labelObject.transform.parent = gameObject.transform;
        labelObject.transform.localPosition = new Vector3(0, 0, 0);
        labelObject.AddComponent(typeof(TextMesh));
        gameObject.transform.parent = gameObject.transform;
        //lr.colorGradient = ColorGrad(Color.red);
        //lr.widthCurve = WidthCurv(width);
        //lr.widthMultiplier = width;
    }

    static public Gradient ColorGrad(Color color1)
    {
        float alpha = 1.0f;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color1, 0.5f) },
            new GradientAlphaKey[] { new GradientAlphaKey(alpha, 0.5f) }
        );
        return gradient;
    }

    static public AnimationCurve WidthCurv(float width)
    {
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(1.0f, 0.5f);
        return curve;
    }
}
