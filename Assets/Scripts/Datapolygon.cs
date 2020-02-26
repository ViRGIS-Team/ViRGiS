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
        LineRenderer lr = gameObject.GetComponentInChildren<LineRenderer>();
        Vector3[] vertices = new Vector3[lr.positionCount];
        lr.GetPositions(vertices);
        Vector3 center = Poly.FindCenter(vertices);
        GameObject shapeObject = gameObject.GetComponentInChildren<Datapolygon>().gameObject;
        // Material mat = shapeObject.GetComponent<MeshRenderer>().material;
        //shapeObject.Destroy();
        Poly.Draw(vertices, center, gameObject, new Material(Shader.Find("Specular")) );
    }
}
