// from https://answers.unity.com/questions/546473/create-a-plane-from-points.html

using UnityEngine;
using System.Collections;

public class Poly
{

    static public void Draw(Vector3[] poly, Vector3 center, GameObject parent, Material mat = null)
    {
        GameObject gameobject = new GameObject("Polygon Shape");
        gameobject.transform.SetPositionAndRotation(center, Quaternion.identity);
        gameobject.transform.parent = parent.transform;

        if (poly == null || poly.Length < 3)
        {
            Debug.Log("Invalid polygon verteces");
            return;
        }

        MeshFilter mf = gameobject.GetComponent<MeshFilter>();
        if (mf == null)
        {
            mf = gameobject.AddComponent<MeshFilter>();
        };

        Mesh mesh = new Mesh();
        mf.mesh = mesh;

        Renderer rend = gameobject.GetComponent<MeshRenderer>();
        if (rend == null)
        {
            rend = gameobject.AddComponent<MeshRenderer>();
            rend.material = mat;
        };

        Vector3[] vertices = new Vector3[poly.Length + 1];
        vertices[0] = Vector3.zero;

        for (int i = 0; i < poly.Length; i++)
        {
            //poly[i].y = 0.0f;
            vertices[i + 1] = poly[i] - center;
        }

        mesh.vertices = vertices;

        int[] triangles = new int[poly.Length * 3];

        for (int i = 0; i < poly.Length - 1; i++)
        {
            triangles[i * 3] = i + 2;
            triangles[i * 3 + 1] = 0;
            triangles[i * 3 + 2] = i + 1;
        }

        triangles[(poly.Length - 1) * 3] = 1;
        triangles[(poly.Length - 1) * 3 + 1] = 0;
        triangles[(poly.Length - 1) * 3 + 2] = poly.Length;

        mesh.triangles = triangles;
        mesh.uv = BuildUVs(vertices);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

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