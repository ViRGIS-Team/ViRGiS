using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Datapolygon : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EditEndAction()
    {
        DatalineCylinder perimeter = gameObject.GetComponentInChildren<DatalineCylinder>();
        Vector3[] vertices = perimeter.GetVertices();
        Vector3 center = Poly.FindCenter(vertices);
        GameObject shapeObject = gameObject.transform.Find("Polygon Shape").gameObject;
        shapeObject.Destroy();
        Poly.Draw(vertices, center, gameObject, new Material(Shader.Find("Specular")) );
    }
}
