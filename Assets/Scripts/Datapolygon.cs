// copyright Runette Software Ltd, 2020. All rights reserved
// parts from https://answers.unity.com/questions/546473/create-a-plane-from-points.html

using System.Collections.Generic;
using UnityEngine;
using GeoJSON.Net.Geometry;

/// <summary>
/// Controls an instance of a Polygon ViRGIS component
/// </summary>
public class Datapolygon : MonoBehaviour, IVirgisComponent
{

    private bool BlockMove = false; // Is this component in a block move state
    public string gisId; // Feature ID from the geoJSON 
    public IDictionary<string, object> gisProperties; // featire properties from the geoJSON
    private GameObject shape; // gameObject to be used for the shape
    public DatapointSphere centroid; // Polyhedral center vertex

    /// <summary>
    /// Called hwne a child component is selected
    /// </summary>
    /// <param name="button"></param>
    public void Selected(SelectionTypes button)
    {
        if (button == SelectionTypes.SELECTALL)
        {
            gameObject.BroadcastMessage("Selected", SelectionTypes.BROADCAST, SendMessageOptions.DontRequireReceiver);
            BlockMove = true;
            DatalineCylinder com = gameObject.GetComponentInChildren<DatalineCylinder>();
            com.Selected(SelectionTypes.SELECTALL);
        }
    }

    /// <summary>
    /// Called when a child component is unselected
    /// </summary>
    /// <param name="button"></param>
    public void UnSelected(SelectionTypes button)
    {
        if (button != SelectionTypes.BROADCAST)
        {
            gameObject.BroadcastMessage("UnSelected", SelectionTypes.BROADCAST, SendMessageOptions.DontRequireReceiver);
            BlockMove = false;
        }
    }

    /// <summary>
    /// Called when a child Vertex is asked to move by the user
    /// </summary>
    /// <param name="data">MoveArgs</param>
    public void VertexMove(MoveArgs data)
    {
        if (!BlockMove)
        {
            ShapeMoveVertex(data);
        }
    }

    /// <summary>
    /// called when a child component is asked to move by the user
    /// </summary>
    /// <param name="data"> MoveArgs</param>
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

    /// <summary>
    /// Change the Color of the component
    /// </summary>
    /// <param name="newCol"></param>
    public void SetColor(Color newCol)
    {
        shape.GetComponent<Renderer>().material.SetColor("_BaseColor", newCol);
    }

    /// <summary>
    /// Callled on an ExitEditSession event
    /// </summary>
    public void EditEnd()
    {

    }

    /// <summary>
    /// Called to draw the Polygon based upon the 
    /// </summary>
    /// <param name="perimeter">LineString defining the perimter of the polygon</param>
    /// <param name="mat"> Material to be used</param>
    /// <returns></returns>
    public GameObject Draw(LineString perimeter, Material mat = null)
    {
        Vector3[] poly = Tools.LS2Vect(perimeter);
        shape = new GameObject("Polygon Shape");
        shape.transform.parent = gameObject.transform;
        
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

        shape.transform.position = center;

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

    /*/// <summary>
    /// refresh the polygon mesh after a vertex move
    /// </summary>
    /// <param name="poly"></param>
    /// <param name="center"></param>
    /// <returns></returns>
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
    }*/

    /// <summary>
    /// Move a vertex of the polygon and recreate the mesh
    /// </summary>
    /// <param name="data">MoveArgs</param>
    public void ShapeMoveVertex(MoveArgs data)
    {
        Mesh mesh = shape.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        vertices[data.id + 1] = vertices[data.id + 1] + transform.InverseTransformVector(data.translate);
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }


    /// <summary>
    /// Calculate the verteces of the polygon from the LineSString
    /// </summary>
    /// <param name="poly">Vector3[] LineString in Worlspace coordinates</param>
    /// <param name="center">Vector3 centroid in Worldspace coordinates</param>
    /// <returns></returns>
    public Vector3[] Vertices(Vector3[] poly, Vector3 center)
    {
        Vector3[] vertices = new Vector3[poly.Length];
        vertices[0] = shape.transform.InverseTransformPoint(center);

        for (int i = 0; i < poly.Length - 1; i++)
        {
            //poly[i].y = 0.0f;
            vertices[i + 1] = shape.transform.InverseTransformPoint(poly[i]);
        }

        return vertices;
    }

    /// <summary>
    /// calculate the Triangles for a Polyhrderon with length verteces
    /// </summary>
    /// <param name="length">number of verteces not including the centroid</param>
    /// <returns></returns>
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

