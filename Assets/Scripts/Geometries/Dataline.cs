// copyright Runette Software Ltd, 2020. All rights reserved
// parts from  https://answers.unity.com/questions/8338/how-to-draw-a-line-using-script.html

using System.Collections.Generic;
using UnityEngine;
using System;
using GeoJSON.Net.Geometry;
using Mapbox.Unity.Map;
using Project;
using g3;
using UnityEngine.UI;

namespace Virgis
{

    /// <summary>
    /// Controls and Instance of a Line Component
    /// </summary>
    public class Dataline : VirgisComponent
    {
        public GameObject CylinderObject;


        private bool BlockMove = false; // is this line in a block-move state
        private bool Lr = false; // is this line a Linear Ring - i.e. used to define a polygon
        public List<VertexLookup> VertexTable = new List<VertexLookup>();


        /// <summary>
        /// Every frame - realign the billboard
        /// </summary>
        void Update()
        {
            if (label) label.LookAt(Global.mainCamera.transform);

        }


        public override void SetColor(Color newColor)
        {
            BroadcastMessage("SetColor", newColor, SendMessageOptions.DontRequireReceiver);
        }

        public override void VertexMove(MoveArgs data)
        {
            if (VertexTable.Contains(new VertexLookup() { Id = data.id}))
            {
                VertexLookup vdata = VertexTable.Find(item => item.Id == data.id);
                if (vdata.isVertex)
                {
                    for (int i = 0; i < gameObject.transform.childCount; i++)
                    {
                        GameObject go = gameObject.transform.GetChild(i).gameObject;
                        LineSegment goFunc = go.GetComponent<LineSegment>();
                        if (goFunc != null && goFunc.vStart == vdata.Vertex)
                        {
                            goFunc.MoveStart(data.pos);
                        }
                        else if (goFunc != null && goFunc.vEnd == vdata.Vertex)
                        {
                            goFunc.MoveEnd(data.pos);
                        }
                    }
                }
            }
        }

        // https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
        public override void MoveAxis(MoveArgs args)
        {

            if (args.translate != null) transform.Translate(args.translate, Space.World);
            args.rotate.ToAngleAxis(out float angle, out Vector3 axis);
            transform.RotateAround(args.pos, axis, angle);
            Vector3 A = transform.localPosition;
            Vector3 B = transform.parent.InverseTransformPoint(args.pos);
            Vector3 C = A - B;
            float RS = args.scale;
            Vector3 FP = B + C * RS;
            if (FP.magnitude < float.MaxValue)
            {
                transform.localScale = transform.localScale * RS;
                transform.localPosition = FP;
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform T = transform.GetChild(i);
                    if (T.GetComponent<LineSegment>() != null)
                    {
                        Vector3 local = T.localScale;
                        local /= RS;
                        local.z = T.localScale.z;
                        T.localScale = local;
                    }
                    else
                    {
                        T.localScale /= RS;
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
        /// <param name="LabelPrefab"> the prefab to used for the label</param>
        public void Draw(LineString lineIn, Dictionary<string, Unit> symbology, GameObject LinePrefab, GameObject HandlePrefab, GameObject LabelPrefab)
        {
            AbstractMap _map = Global._map;
            Vector3[] line = Tools.LS2Vect(lineIn);
            Lr = lineIn.IsLinearRing();
            DCurve3 curve = new DCurve3();
            curve.Vector3(line, Lr);
            Vector3 center = (Vector3)curve.CenterMark();

            int i = 0;
            foreach (Vector3 vertex in line)
            {
                if (!(i + 1 == line.Length && Lr))
                {
                    GameObject handle = Instantiate(HandlePrefab, vertex, Quaternion.identity);
                    VirgisComponent com = handle.GetComponent<VirgisComponent>();
                    handle.transform.parent = transform;
                    VertexTable.Add(new VertexLookup() { Id = com.id, Vertex = i, isVertex = true, Com = com });
                    com.SetColor ((Color)symbology["point"].Color);
                    handle.transform.localScale = symbology["point"].Transform.Scale;
                }
                if (i + 1 != line.Length)
                {
                    GameObject lineSegment = Instantiate(CylinderObject, vertex, Quaternion.identity);
                    lineSegment.transform.parent = transform;
                    LineSegment com = lineSegment.GetComponent<LineSegment>();
                    com.Draw(vertex, line[i + 1], i, i + 1, symbology["line"].Transform.Scale.magnitude);
                    com.SetColor((Color)symbology["line"].Color);
                    if (i + 2 == line.Length && Lr) com.vEnd = 0;
                }
                i++;
            }

            //Set the label
            if (LabelPrefab != null)
            {
                GameObject labelObject = Instantiate(LabelPrefab, center, Quaternion.identity);
                labelObject.transform.parent = transform;
                labelObject.transform.Translate(Vector3.up * symbology["line"].Transform.Scale.magnitude);
                label = labelObject.transform;
                Text labelText = labelObject.GetComponentInChildren<Text>();
                if (symbology["line"].ContainsKey("Label") && symbology["line"].Label != null && gisProperties.ContainsKey(symbology["line"].Label))
                {
                    labelText.text = (string)gisProperties[symbology["line"].Label];
                }
            }
        }

        /// <summary>
        /// called to get the verteces of the LineString
        /// </summary>
        /// <returns>Vector3[] of verteces</returns>
        public Vector3[] GetVerteces()
        {
            Vector3[] result = new Vector3[VertexTable.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = VertexTable.Find(item => item.Vertex == i).Com.transform.position;
            }
            if (Lr)
            {
                Array.Resize<Vector3>(ref result, result.Length + 1);
                result[result.Length - 1] = result[0];
            }
            return result;
        }


        public override void Selected(SelectionTypes button)
        {
            if (button == SelectionTypes.SELECTALL)
            {
                gameObject.BroadcastMessage("Selected", SelectionTypes.BROADCAST, SendMessageOptions.DontRequireReceiver);
                BlockMove = true;
            }
        }

        public override void UnSelected(SelectionTypes button)
        {
            if (button != SelectionTypes.BROADCAST)
            {
                gameObject.BroadcastMessage("UnSelected", SelectionTypes.BROADCAST, SendMessageOptions.DontRequireReceiver);
                BlockMove = false;
            }
        }

        public override void Translate(MoveArgs args)
        {
            if (!BlockMove)
            {
                gameObject.BroadcastMessage("TranslateHandle", args, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                transform.Translate(args.translate, Space.World);
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

        public override void EditEnd()
        {

        }

        public override void MoveTo(Vector3 newPos)
        {
            throw new NotImplementedException();
        }

        public override Vector3 GetClosest(Vector3 coords)
        {
            throw new NotImplementedException();
        }

        public override T GetGeometry<T>()
        {
            throw new NotImplementedException();
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
}
