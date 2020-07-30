// copyright Runette Software Ltd, 2020. All rights reserved
// parts from https://answers.unity.com/questions/546473/create-a-plane-from-points.html

using System.Collections.Generic;
using UnityEngine;
using g3;
using OSGeo.OGR;
using System.Linq;

namespace Virgis
{
 

    /// <summary>
    /// Controls an instance of a Polygon ViRGIS component
    /// </summary>
    public class Dataplane : VirgisFeature
    {

        private bool BlockMove = false; // Is this component in a block move state
        private GameObject Shape; // gameObject to be used for the shape
        public List<Vector3> VertexTable;
        private Vector3[] top;
        private Vector3[] bottom;


        public override void Selected(SelectionType button)
        {
            if (button == SelectionType.SELECTALL)
            {
                gameObject.BroadcastMessage("Selected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
                BlockMove = true;
                Dataline com = gameObject.GetComponentInChildren<Dataline>();
                com.Selected(SelectionType.SELECTALL);
            }
        }

        public override void UnSelected(SelectionType button)
        {
            if (button != SelectionType.BROADCAST)
            {
                gameObject.BroadcastMessage("UnSelected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
                BlockMove = false;
            }
        }

        public override void VertexMove(MoveArgs data)
        {
            if (!BlockMove)
            {
                //ShapeMoveVertex(data);
            }
        }

        public override void Translate(MoveArgs args)
        {
            if (BlockMove)
            {
                GameObject shape = gameObject.transform.Find("Polygon Shape").gameObject;
                shape.transform.Translate(args.translate, Space.World);
            }

        }

        // https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
        public override void MoveAxis(MoveArgs args)
        {
            if (args.translate != null)
            {
                Shape.transform.Translate(args.translate, Space.World);
            }
            args.rotate.ToAngleAxis(out float angle, out Vector3 axis);
            Shape.transform.RotateAround(args.pos, axis, angle);
            Vector3 A = Shape.transform.localPosition;
            Vector3 B = transform.InverseTransformPoint(args.pos);
            Vector3 C = A - B;
            float RS = args.scale;
            Vector3 FP = B + C * RS;
            if (FP.magnitude < float.MaxValue)
            {
                Shape.transform.localScale = Shape.transform.localScale * RS;
                Shape.transform.localPosition = FP;
            }
        }

        public override void MoveTo(MoveArgs args)
        {
            throw new System.NotImplementedException();
        }



        /// <summary>
        /// Called to draw the Polygon based upon the 
        /// </summary>
        /// <param name="perimeter">LineString defining the perimter of the polygon</param>
        /// <param name="mat"> Material to be used</param>
        /// <returns></returns>
        public GameObject Draw( Vector3[] top, Vector3[] bottom,  Material mat = null)
        {

            VertexTable = top.ToList<Vector3>();
            for (int i = 0; i < bottom.Length; i++) {
                VertexTable.Add(bottom[bottom.Length - i - 1]);
            }
            this.top = top;
            this.bottom = bottom;
            
            Shape = new GameObject("Polygon Shape");
            Shape.transform.parent = transform;

            MakeMesh();

            Renderer rend = Shape.GetComponent<MeshRenderer>();
            if (rend == null)
            {
                rend = Shape.AddComponent<MeshRenderer>();
                rend.material = mat;
            };

            return gameObject;

        }

        /// <summary>
        /// Generates the actual mesh for the polyhedron
        /// </summary>
        private void MakeMesh()
        {
            MeshFilter mf;
            mf = Shape.GetComponent<MeshFilter>();    
            if (mf == null)  mf = Shape.AddComponent<MeshFilter>();
            mf.mesh = null;
            Mesh mesh = new Mesh();
            Vector3[] vertices = Vertices();
            mesh.vertices = vertices;
            mesh.triangles = Triangles();
            mesh.uv = BuildUVs();

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.Optimize();

            mf.mesh = mesh;
        }


        /// <summary>
        /// Move a vertex of the polygon and recreate the mesh
        /// </summary>
        /// <param name="data">MoveArgs</param>
        //public void ShapeMoveVertex(MoveArgs data)
        //{
        //    Mesh mesh = Shape.GetComponent<MeshFilter>().mesh;
        //    Vector3[] vertices = mesh.vertices;
        //    vertices[VertexTable.Find(item => item.Id == data.id ).Vertex + 1] = Shape.transform.InverseTransformPoint(data.pos);
        //    mesh.vertices = vertices;
        //    mesh.uv = BuildUVs(vertices);
        //    mesh.RecalculateBounds();
        //    mesh.RecalculateNormals();
        //}

        //public override VirgisFeature AddVertex(Vector3 position) {
        //    _redraw();
        //    return base.AddVertex(position);
        //}

        //public override void RemoveVertex(VirgisFeature vertex) {
        //    if (BlockMove) {
        //        gameObject.Destroy();
        //    } else {
        //        _redraw();
        //    }
        //}

        //private void _redraw() {
        //    VertexTable = GetComponentInChildren<Dataline>().VertexTable;
        //    MakeMesh();
        //}


        /// <summary>
        /// Calculate the verteces of the polygon from the LineSString
        /// </summary>
        /// <param name="poly">Vector3[] LineString in Worlspace coordinates</param>
        /// <param name="center">Vector3 centroid in Worldspace coordinates</param>
        /// <returns></returns>
        public Vector3[] Vertices()
        {
            Vector3[] vertices = new Vector3[VertexTable.Count];


            for (int i = 0; i < VertexTable.Count; i++)
            {
                vertices[i] = Shape.transform.InverseTransformPoint(VertexTable[i]);
            }

            return vertices;
        }

        // STATIC METHODS TO HELP CREATE A POLYGON

        /// <summary>
        /// calculate the Triangles for a Polyhrderon with length verteces
        /// </summary>
        /// <param name="length">number of verteces not including the centroid</param>
        /// <returns></returns>
        public int[] Triangles()
        {
            int quadCount = VertexTable.Count() / 2 - 1;
            int lastVertex = VertexTable.Count() - 1;
            int[] triangles = new int[quadCount * 6];

            for (int i = 0; i < quadCount; i++) {
                triangles[i * 6] = i ;
                triangles[i * 6 + 1] = i  + 1;
                triangles[i * 6 + 2] = lastVertex - i ;
                triangles[i * 6 + 3] = lastVertex - i ;
                triangles[i * 6 + 4] = lastVertex - (i + 1);
                triangles[i * 6 + 5] = i + 1;
            }

            return triangles;
        }


        /// <summary>
        /// Reset the center vertex to be the center of the Linear Ring vertexes
        /// </summary>
        //public void ResetCenter() {
        //    VertexLookup centroid = VertexTable.Find(item => item.Vertex == -1);
        //    DCurve3 curve = new DCurve3();
        //    curve.Vector3(GetVertexPositions(), true);
        //    centroid.Com.transform.position = (Vector3)curve.Center();
        //    MakeMesh();
        //}

        private Vector2[] BuildUVs()
        {

            DCurve3 topCurve = new DCurve3();
            topCurve.Vector3(top,  false);
            float length = (float)topCurve.ArcLength;
            Vector2[] uvs = new Vector2[VertexTable.Count];
            float dist = 0f;
            uvs[0].y = 1f;
            uvs[0].x = 0f;
            uvs[uvs.Length - 1].x = 0f;
            uvs[uvs.Length - 1].y = 0f;
            for (int i = 1; i < uvs.Length/2; i++)
            {
                dist += Vector3.Distance(VertexTable[i], VertexTable[i - 1]);
                uvs[i].x = dist / length;
                uvs[i].y = 1f;
                uvs[uvs.Length - 1 - i].x = dist / length;
                uvs[uvs.Length - 1 - i].y = 0;
            }
            return uvs;
        }

        /// <summary>
        /// Get an array of the Datapoint components for the vertexes
        /// </summary>
        /// <returns> Datapoint[]</returns>
        //public Datapoint[] GetVertexes() {
        //    Datapoint[] result = new Datapoint[VertexTable.Count - 1];
        //    for (int i = 0; i < result.Length; i++) {
        //        result[i] = VertexTable.Find(item => item.isVertex && item.Vertex == i).Com as Datapoint;
        //    }
        //    return result;
        //}

    
        public Vector3[] GetVertexPositions() {
            return VertexTable.ToArray();
        }
    }
}

