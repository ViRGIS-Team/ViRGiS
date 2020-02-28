// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CylinderLine : MonoBehaviour
{
    public int id;
    private Vector3 start;
    private Vector3 end;
    private float diameter;

    public void Draw(Vector3 from, Vector3 to, float dia)
    {
        start = from;
        end = to;
        diameter = dia;
        _draw();

    }

    public void SetId(int newId) {
        id = newId;
    }

    public void MoveStart(Vector3 newStart) {
        start = newStart;
        _draw();
    }

    public void MoveEnd (Vector3 newEnd) {
        end = newEnd;
        _draw();
    }

    private void _draw()
    {
        gameObject.transform.position = start;
        gameObject.transform.LookAt(end);
        float length = Vector3.Distance(start, end) / 2.0f;
        gameObject.transform.localScale = new Vector3(diameter, diameter, length);
    }
}
