/* MIT License

Copyright (c) 2020 - 21 Runette Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice (and subsidiary notices) shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */

// parts from  https://answers.unity.com/questions/8338/how-to-draw-a-line-using-script.html

using System.Collections.Generic;
using UnityEngine;
using System;
using Project;
using g3;
using UnityEngine.UI;
using System.Linq;
using OSGeo.OGR;

namespace Virgis
{

    /// <summary>
    /// Controls and Instance of a Line Component
    /// </summary>
    public class Dataline : VirgisFeature
    {
        public GameObject CylinderObject;


        private bool m_Lr = false; // is this line a Linear Ring - i.e. used to define a polygon
        public List<VertexLookup> VertexTable = new List<VertexLookup>();
        private Dictionary<string, Unit> m_symbology;
        private GameObject m_handlePrefab;
        private Material m_lineMain;
        private Material m_lineSelected;
        public DCurve3 Curve = new DCurve3();



        /// <summary>
        /// Every frame - realign the billboard
        /// </summary>
        public void Update()
        {
            if (label) label.LookAt(AppState.instance.mainCamera.transform);
        }


        public override void VertexMove(MoveArgs data)
        {
            if (VertexTable.Contains(new VertexLookup() { Id = data.id})) {
                VertexLookup vdata = VertexTable.Find(item => item.Id == data.id);
                foreach (VertexLookup vLookup in VertexTable) {
                    if (vLookup.Line && vLookup.Line.vStart == vdata.Vertex)
                        vLookup.Line.MoveStart(data.pos);
                    if (vLookup.Line && vLookup.Line.vEnd == vdata.Vertex)
                        vLookup.Line.MoveEnd(data.pos);
                }
                Curve.Vector3(GetVertexPositions(), m_Lr);
                if (label) label.position = _labelPosition();
            }
            Curve.Vector3(GetVertexPositions(), m_Lr);
        }

        public override void MoveAxis(MoveArgs args) {

            base.MoveAxis(args);
        }


        /// <summary>
        /// This is called by the parent to action the move
        /// </summary>
        /// <param name="args"></param>
        // https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
        public void MoveAxisAction(MoveArgs args)
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
        /// <param name="handlePrefab"> The prefab to be used for the handle</param>
        /// <param name="labelPrefab"> the prefab to used for the label</param>
        public void Draw(Geometry geom, Dictionary<string, Unit> symbology,  GameObject handlePrefab, GameObject labelPrefab, Material mainMat, Material selectedMat, Material lineMain, Material lineSelected, bool isring = false)
        {
            m_symbology = symbology;
            m_handlePrefab = handlePrefab;
            this.mainMat = mainMat;
            this.selectedMat = selectedMat;
            m_lineMain = lineMain;
            m_lineSelected = lineSelected;
            m_Lr = geom.IsRing();
            if (isring)
                m_Lr = true;
            Vector3[] line = geom.TransformWorld();
            Curve.FromGeometry(geom);

            string type = geom.GetGeometryType().ToString();
            bool IsRing = geom.IsRing();


            int i = 0;
            foreach (Vector3 vertex in line)
            {
                if (!(i + 1 == line.Length && m_Lr))
                {
                    _createVertex(vertex, i);
                }
                if (i + 1 != line.Length)
                {
                    _createSegment(vertex, line[i + 1],i , (i + 2 == line.Length && m_Lr));
                }
                i++;
            }
            Curve.Vector3(GetVertexPositions(), m_Lr);

            //Set the label
            if (labelPrefab != null)
            {
                if (symbology["line"].ContainsKey("Label") && symbology["line"].Label != null && (feature?.ContainsKey(symbology["line"].Label) ?? false))
                   {
                    GameObject labelObject = Instantiate(labelPrefab, _labelPosition(), Quaternion.identity, transform);
                    label = labelObject.transform;
                    Text labelText = labelObject.GetComponentInChildren<Text>();
                    labelText.text = (string)feature.Get(symbology["line"].Label);
                }
            }
        }

        /// <summary>
        /// Make the Line into a Linear Ring by setting the Lr flag and creating a LineSegment form the last vertex to the first.
        /// If the last vertex is in the same (exact) position as the first vertex, the last vertex is deleted.
        /// </summary>
        public void MakeLinearRing() {
            // Make the Line inot a Linear ring
            if (!m_Lr) {
                VertexLookup First = VertexTable.Find(item => item.Vertex == 0);
                VertexLookup Last = VertexTable.Find(item => item.Vertex == VertexTable.Count - 1);
                if (First.Com.transform.position == Last.Com.transform.position) {
                    Destroy(Last.Com.gameObject);
                    VertexTable.Remove(Last);
                    Last = VertexTable.Find(item => item.Vertex == Last.Vertex - 1);
                    Last.Line.MoveEnd(First.Com.transform.position);
                    Last.Line.vEnd = 0;
                } else {
                    VertexTable.Last().Line = _createSegment(VertexTable.Last().Com.transform.position, VertexTable.First().Com.transform.position, VertexTable.Count -1, true);
                }

                m_Lr = true;
            }
        }

        /// <summary>
        /// called to get the verteces of the LineString
        /// </summary>
        /// <returns>Vector3[] of verteces</returns>
        public Vector3[] GetVertexPositions()
        {
            List<Vector3> result = new List<Vector3>();
            for (int i = 0; i < VertexTable.Count ; i++) {
                    result.Add(VertexTable.Find(item => item.isVertex && item.Vertex == i).Com.transform.position);
                }
            return result.ToArray();
        }

        public Datapoint[] GetVertexes() {
            Datapoint[] result = new Datapoint[VertexTable.Count];
            for (int i = 0; i < result.Length; i++) {
                result[i] = VertexTable.Find(item => item.isVertex && item.Vertex == i).Com as Datapoint;
            }
            return result;
        }


        public override void Selected(SelectionType button)
        {
            if (button == SelectionType.SELECTALL)
            {
                gameObject.BroadcastMessage("Selected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
                m_blockMove = true;
            }
        }

        public override void UnSelected(SelectionType button)
        {
            if (button != SelectionType.BROADCAST)
            {
                gameObject.BroadcastMessage("UnSelected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
                m_blockMove = false;
            }
        }

        public override void Translate(MoveArgs args)
        {
            if (!m_blockMove)
            {
                gameObject.BroadcastMessage("TranslateHandle", args, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                args.id = GetId();
                transform.parent.SendMessage("Translate", args, SendMessageOptions.DontRequireReceiver);
            }
        }

        public override void MoveTo(MoveArgs args)
        {
            throw new NotImplementedException();
        }

        public override VirgisFeature AddVertex(Vector3 position) {
            int seg = Curve.NearestSegment(position);
            LineSegment segment = VertexTable.Find(item => item.Vertex == seg).Line;
            return AddVertex(segment, position);
        }

        /// <summary>
        /// Add a vertx to the Line when you know the segment to add the vertex to
        /// </summary>
        /// <param name="segment"> Linesegement to add the vertex to </param>
        /// <param name="position"> Vertex Position in Wordl Space coordinates</param>
        /// <returns></returns>
        public VirgisFeature AddVertex(LineSegment segment, Vector3 position) {
            int start = segment.vStart;
            int next = segment.vEnd;
            VertexTable.ForEach(item => {
                if (item.Vertex > start) {
                    item.Vertex++;
                    if (item.Line != null) {
                        item.Line.vStart++;
                        if (item.Line.vEnd != 0) {
                            item.Line.vEnd++;
                        }
                    }
                }
                if (m_Lr && item.isVertex && item.Line.vStart == start) {
                    item.Line.vEnd = start + 1;
                }
                if (m_Lr && item.isVertex && item.Line.vEnd > VertexTable.Count)
                    item.Line.vEnd = 0;
            });
            start++;
            int end = next;
            if (end != 0)
                end++;
            segment.MoveEnd(position);
            Datapoint vertex = _createVertex(position, start);
            _createSegment(position, VertexTable.Find(item => item.Vertex == end).Com.transform.position, start, end == 0);
            transform.parent.SendMessage("AddVertex", position, SendMessageOptions.DontRequireReceiver);
            vertex.UnSelected(SelectionType.SELECT);
            Curve.Vector3(GetVertexPositions(), m_Lr);
            return vertex;
        }

        public override void RemoveVertex(VirgisFeature vertex) {
            if (m_blockMove) {
                Destroy(gameObject);
            } else {
                VertexLookup vLookup = VertexTable.Find(item => item.Com == vertex);
                if (vLookup.isVertex) {
                    int thisVertex = vLookup.Vertex;
                    if (vLookup.Line != null) {
                        Destroy(vLookup.Line.gameObject);
                    } else {
                        Destroy(VertexTable.Find(item => item.Vertex == vLookup.Vertex - 1).Line.gameObject);
                    }
                    Destroy(vLookup.Com.gameObject);
                    VertexTable.Remove(vLookup);
                    VertexTable.ForEach(item => {
                        if (item.Vertex >= thisVertex) {
                            item.Vertex--;
                            if (item.Line != null) {
                                item.Line.vStart--;
                                if (item.Line.vEnd != 0) {
                                    item.Line.vEnd--;
                                }
                            }
                        };
                        if (m_Lr && item.isVertex  && item.Line.vEnd >= VertexTable.Count) {
                            item.Line.vEnd = 0;
                        };
                    });
                    int end = thisVertex;
                    int start = thisVertex - 1;
                    if (m_Lr && thisVertex >= VertexTable.Count ) 
                        end = 0;
                    if (m_Lr && thisVertex == 0)
                        start = VertexTable.Count - 1;
                    Debug.Log($"start : {start}, End : {end}");
                    if (VertexTable.Count > 1) {
                        VertexTable.Find(item => item.Vertex == start).Line.MoveEnd(VertexTable.Find(item => item.Vertex == end).Com.transform.position);
                    } else {
                        Destroy(gameObject);
                    }
                }
            }
            transform.parent.SendMessage("RemoveVertex", this, SendMessageOptions.DontRequireReceiver);
        }

        private Datapoint _createVertex(Vector3 vertex, int i) {
            GameObject handle = Instantiate(m_handlePrefab, vertex, Quaternion.identity, transform );
            Datapoint com = handle.GetComponent<Datapoint>();
            VertexTable.Add(new VertexLookup() { Id = com.GetId(), Vertex = i, isVertex = true, Com = com });
            com.SetMaterial(mainMat, selectedMat);
            handle.transform.localScale = m_symbology["point"].Transform.Scale;
            return com;
        }

        private LineSegment _createSegment(Vector3 start, Vector3 end, int i, bool close) {
            GameObject lineSegment = Instantiate(CylinderObject, start, Quaternion.identity, transform);
            LineSegment com = lineSegment.GetComponent<LineSegment>();
            com.Draw(start, end, i, i + 1, m_symbology["line"].Transform.Scale.magnitude);
            com.SetMaterial(m_lineMain, m_lineSelected);
            if (close)
                com.vEnd = 0;
            VertexTable.Find(item => item.Vertex == i).Line = com;
            return com;
        }

        /// <summary>
        /// get the center of the line
        /// 
        /// <returns></returns>
        private Vector3 Center() {
            return (Vector3) Curve.CenterMark();
        }

        private Vector3 _labelPosition() {
            return Center() + transform.TransformVector(Vector3.up) * m_symbology["line"].Transform.Scale.magnitude;
        }

        public override Dictionary<string, object> GetMetadata() {
            return feature.GetAll();
        }

        public override void SetMetadata(Dictionary<string, object> meta) {
            throw new NotImplementedException();
        }
    }
}
