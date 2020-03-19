// copyright Runette Software Ltd, 2020. All rights reserved
// parts from https://answers.unity.com/questions/546473/create-a-plane-from-points.html

using System.Collections.Generic;
using UnityEngine;
using GeoJSON.Net.Geometry;


public class Datapolygon : MonoBehaviour
{

    private bool BlockMove = false;
    public string gisId;
    public IDictionary<string, object> gisProperties;
    private GameObject shape;
    public DatapointSphere centroid;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Selected(int button)
    {
        if (button == 1)
        {
            gameObject.BroadcastMessage("Selected", 100, SendMessageOptions.DontRequireReceiver);
            BlockMove = true;
            DatalineCylinder com = gameObject.GetComponentInChildren<DatalineCylinder>();
            com.Selected(1);
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

    public void VertexMove(MoveArgs data)
    {
        if (!BlockMove)
        {
            ShapeMoveVertex(data);
        }
    }

    public void Translate(MoveArgs data)
    {
        if (BlockMove)
        {
            GameObject shape = gameObject.transform.Find("Polygon Shape").gameObject;
            shape.transform.Translate(data.translate);
            if (data.id < 0)
            {
                DatalineCylinder com = gameObject.GetComponentInChildren<DatalineCylinder>();
                com.Translate(data);
            }
        }
    }

    public void SetColor(Color newCol)
    {
        shape.GetComponent<Renderer>().material.SetColor("_BaseColor", newCol);
    }

    public GameObject Draw(Vector3[] poly, Material mat = null)
    {

        shape = new GameObject("Polygon Shape");
        shape.transform.parent = gameObject.transform;
        shape.transform.localPosition = Vector3.zero;


        if (poly == null || poly.Length < 3)
        {
            throw new System.ArgumentException("Invalid polygon vertices"); ;
        }
        Vector3 center = Vector3.zero;
        if (gisProperties.ContainsKey("polyhedral"))
        {
            Point centerPosition = gisProperties["polyhedral"] as Point;
            center = centerPosition.Coordinates.Vector3();
        }
        else
        {
            center = FindCenter(poly);
            gisProperties["polyhedral"] = new Point(Tools.Vect2Ipos(center));
        }

        MeshFilter mf = shape.AddComponent<MeshFilter>();

        mf.mesh = MakeMesh(poly, center);

        Renderer rend = shape.GetComponent<MeshRenderer>();
        if (rend == null)
        {
            rend = shape.AddComponent<MeshRenderer>();
            rend.material = mat;
        };

        return gameObject;

    }

    public GameObject RefreshMesh(Vector3[] poly, Vector3 center)
    {
        Mesh mesh = shape.GetComponent<MeshFilter>().mesh;
        mesh.Clear();
        Vector3[] vertices = Vertices(poly, center);
        mesh.vertices = vertices;
        mesh.triangles = Triangles(poly.Length);
        mesh.uv = BuildUVs(vertices);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return gameObject;
    }

    public void ShapeMoveVertex(MoveArgs data)
    {
        Mesh mesh = shape.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        vertices[data.id + 1] = vertices[data.id + 1] + data.translate;
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    public Vector3[] Vertices(Vector3[] poly, Vector3 center)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[poly.Length];
        vertices[0] = Vector3.zero;

        for (int i = 0; i < poly.Length - 1; i++)
        {
            //poly[i].y = 0.0f;
            vertices[i + 1] = poly[i] - center;
        }

        return vertices;
    }

    public static int[] Triangles(int length)
    {

        int[] triangles = new int[length * 3];

        for (int i = 0; i < length - 1; i++)
        {
            triangles[i * 3] = i + 2;
            triangles[i * 3 + 1] = 0;
            triangles[i * 3 + 2] = i + 1;
        }

        triangles[(length - 1) * 3] = 1;
        triangles[(length - 1) * 3 + 1] = 0;
        triangles[(length - 1) * 3 + 2] = length;

        return triangles;
    }

    public Mesh MakeMesh(Vector3[] poly, Vector3 center)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = Vertices(poly, center);
        mesh.vertices = vertices;
        mesh.triangles = Triangles(poly.Length - 1);
        mesh.uv = BuildUVs(vertices);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }

    public static Vector3 FindCenter(Vector3[] poly)
    {
        Vector3 center = Vector3.zero;
        foreach (Vector3 v3 in poly)
        {
            center += v3;
        }
        return center / poly.Length;
    }

    static Vector2[] BuildUVs(Vector3[] vertices)
    {

        float xMin = Mathf.Infinity;
        float zMin = Mathf.Infinity;
        //float yMin = Mathf.Infinity;
        //float yMax = -Mathf.Infinity;
        float xMax = -Mathf.Infinity;
        float zMax = -Mathf.Infinity;

        foreach (Vector3 v3 in vertices)
        {
            if (v3.x < xMin)
                xMin = v3.x;
            if (v3.z < zMin)
                zMin = v3.y;
            if (v3.x > xMax)
                xMax = v3.x;
            if (v3.z > zMax)
                zMax = v3.y;
        }

        float xRange = xMax - xMin;
        float zRange = zMax - zMin;
        //float yRange = yMax - yMin;

        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            uvs[i].x = (vertices[i].x - xMin) / xRange;
            uvs[i].y = (vertices[i].z - zMin) / zRange;
            //uvs[i].y = (vertices[i].z - zMin) / zRange;

        }
        return uvs;
    }
}

