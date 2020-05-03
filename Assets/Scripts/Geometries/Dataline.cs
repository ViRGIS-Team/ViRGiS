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
using System.Linq;

namespace Virgis
{

    /// <summary>
    /// Controls and Instance of a Line Component
    /// </summary>
    public class Dataline : MonoBehaviour, IVirgisComponent
    {
        public Color color; // color for the line
        public Color anticolor; // color for the vertces when selected
        public GameObject CylinderObject;
        public string gisId; // the ID for this line from the geoJSON
        public IDictionary<string, object> gisProperties; // the properties for this entity


        private bool BlockMove = false; // is this line in a block-move state
        private bool Lr = false; // is this line a Linear Ring - i.e. used to define a polygon
        private Transform label; //  Go of the label or billboard

        /// <summary>
        /// Every frame - realign the billboard
        /// </summary>
        void Update()
        {
            if (label) label.LookAt(Global.mainCamera.transform);

        }

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
                    LineSegment goFunc = go.GetComponent<LineSegment>();
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
        /// received when a Move Axis request is made by the user
        /// </summary>
        /// <param name="delta"> Vector representing this channge to the transform</param>
        /// https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
        public void MoveAxis(MoveArgs args)
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
                    handle.transform.parent = transform;
                    handle.SendMessage("SetId", i);
                    handle.SendMessage("SetColor", (Color)symbology["point"].Color);
                    handle.transform.localScale = symbology["point"].Transform.Scale;
                }
                if (i + 1 != line.Length)
                {
                    GameObject lineSegment = Instantiate(CylinderObject, vertex, Quaternion.identity);
                    lineSegment.transform.parent = transform;
                    LineSegment com = lineSegment.GetComponent<LineSegment>();
                    com.SetId(i);
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
            Datapoint[] data = GetHandles();
            Vector3[] result = new Vector3[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                Datapoint datum = data[i];
                result[i] = datum.transform.position;
            }
            if (Lr)
            {
                Array.Resize<Vector3>(ref result, result.Length + 1);
                result[result.Length - 1] = result[0];
            }
            return result;
        }

        /// <summary>
        /// called to get the handle ViRGIS Components for the Line
        /// </summary>
        /// <returns> Datapoint[]</returns>
        public Datapoint[] GetHandles()
        {
            return gameObject.GetComponentsInChildren<Datapoint>().Where(item => item.id >= 0).ToArray();

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
}
