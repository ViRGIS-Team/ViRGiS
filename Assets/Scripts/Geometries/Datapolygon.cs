// copyright Runette Software Ltd, 2020. All rights reserved
// parts from https://answers.unity.com/questions/546473/create-a-plane-from-points.html

using System.Collections.Generic;
using UnityEngine;
using g3;
using System.Linq;

namespace Virgis
{
 

    /// <summary>
    /// Controls an instance of a Polygon ViRGIS component
    /// </summary>
    public class Datapolygon : VirgisFeature
    {

        private bool BlockMove = false; // Is this component in a block move state
        private GameObject Shape; // gameObject to be used for the shape
        public List<VertexLookup> VertexTable;
        public List<Dataline> Polygon;


        public override void Selected(SelectionTypes button)
        {
            if (button == SelectionTypes.SELECTALL)
            {
                gameObject.BroadcastMessage("Selected", SelectionTypes.BROADCAST, SendMessageOptions.DontRequireReceiver);
                BlockMove = true;
                Dataline com = gameObject.GetComponentInChildren<Dataline>();
                com.Selected(SelectionTypes.SELECTALL);
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

        public override void VertexMove(MoveArgs data)
        {
            if (!BlockMove)
            {
                ShapeMoveVertex(data);
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
        public GameObject Draw( List<Dataline> polygon,  Material mat = null)
        {
            
            VertexTable = polygon[0].VertexTable;
            Polygon = polygon;
            
            Shape = new GameObject("Polygon Shape");
            Shape.transform.parent = gameObject.transform;


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
            TriangulatedPolygonGenerator tpg = new TriangulatedPolygonGenerator();
            tpg.Polygon = Polygon.ToPolygon();
            tpg.Generate();
            List<Vector3> vertices3 = new List<Vector3>();
            foreach (Vector3d v in Vertices()) {
                vertices3.Add((Vector3)v);
            }
            List<Vector2> uvs = new List<Vector2>();
            IEnumerable<Vector2d> uv2d = tpg.uv.AsVector2f();
            foreach (Vector2d uv in uv2d) {
                uvs.Add((Vector2) uv);
            }
            mesh.vertices = vertices3.ToArray();
            mesh.triangles = tpg.triangles.ToArray<int>();
            mesh.uv = uvs.ToArray<Vector2>(); 

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            mf.mesh = mesh;
        }


        /// <summary>
        /// Move a vertex of the polygon and recreate the mesh
        /// </summary>
        /// <param name="data">MoveArgs</param>
        public void ShapeMoveVertex(MoveArgs data)
        {
            Mesh mesh = Shape.GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = mesh.vertices;
            vertices[VertexTable.Find(item => item.Id == data.id ).Vertex ] = Shape.transform.InverseTransformPoint(data.pos);
            mesh.vertices = vertices;
            mesh.uv = BuildUVs(vertices);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }

        public override VirgisFeature AddVertex(Vector3 position) {
            _redraw();
            return base.AddVertex(position);
        }

        public override void RemoveVertex(VirgisFeature vertex) {
            if (BlockMove) {
                gameObject.Destroy();
            } else {
                _redraw();
            }
        }

        private void _redraw() {
            VertexTable = GetComponentInChildren<Dataline>().VertexTable;
            MakeMesh();
        }


        /// <summary>
        /// Calculate the verteces of the polygon from the LineSString
        /// </summary>
        /// <param name="poly">Vector3[] LineString in Worlspace coordinates</param>
        /// <param name="center">Vector3 centroid in Worldspace coordinates</param>
        /// <returns></returns>
        public Vector3d[] Vertices()
        {
            Vector3d[] vertices = new Vector3d[VertexTable.Count];
            //vertices[0] = Shape.transform.InverseTransformPoint(Centroid.transform.position);


            for (int i = 0; i < VertexTable.Count; i++)
            {
                vertices[i] = Shape.transform.InverseTransformPoint(VertexTable.Find(item => item.Vertex == i).Com.transform.position);
            }

            return vertices;
        }

        // STATIC METHODS TO HELP CREATE A POLYGON


        static Vector2[] BuildUVs(Vector3[] vertices)
        {

            float xMin = Mathf.Infinity;
            float yMin = Mathf.Infinity;
            float xMax = -Mathf.Infinity;
            float yMax = -Mathf.Infinity;

            Vector3[] UVWs = new Vector3[vertices.Length];

            Vector3[] edges = new Vector3[vertices.Length];

            edges[0] = Vector3.zero;

            for (int i = 1; i< vertices.Length; i++)
            {
                edges[i] = vertices[0] - vertices[i];
            }

            UVWs[1] = Vector3.zero;
            Vector3 baselineEdge = vertices[vertices.Length - 1] - vertices[1];
            UVWs[vertices.Length - 1] = Vector3.right * baselineEdge.magnitude;
            float theta = Vector3.Angle(baselineEdge, edges[vertices.Length - 1]);
            UVWs[0] = UVWs[vertices.Length - 1] + Quaternion.Euler(0, 0, theta) * Vector3.right * edges[vertices.Length - 1].magnitude;

            float thetaStash = 0;
  
            for (int i = 2; i < vertices.Length -1 ; i++)
            {
                theta = Vector3.Angle(edges[1], edges[i]);
                if (theta < thetaStash) theta = 360 - theta;
                thetaStash = theta;
                UVWs[i] = UVWs[0] + Quaternion.Euler(0, 0, 180 - theta) * UVWs[0].normalized * edges[i].magnitude;
            }

            foreach (Vector3 v3 in UVWs)
            {
                if (v3.x < xMin)
                    xMin = v3.x;
                if (v3.y < yMin)
                    yMin = v3.y;
                if (v3.x > xMax)
                    xMax = v3.x;
                if (v3.y > yMax)
                    yMax = v3.y;
            }

            float xRange = xMax - xMin;
            float yRange = yMax - yMin;

            Vector2[] uvs = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                uvs[i].x = (UVWs[i].x - xMin) / xRange;
                uvs[i].y = (UVWs[i].y - yMin) / yRange;


            }
            return uvs;
        }

        /// <summary>
        /// Get an array of the Datapoint components for the vertexes
        /// </summary>
        /// <returns> Datapoint[]</returns>
        public Datapoint[] GetVertexes() {
            Datapoint[] result = new Datapoint[VertexTable.Count ];
            for (int i = 0; i < result.Length; i++) {
                result[i] = VertexTable.Find(item => item.isVertex && item.Vertex == i).Com as Datapoint;
            }
            return result;
        }

    
        public Vector3[] GetVertexPositions() {
            return GetComponentInChildren<Dataline>().GetVertexPositions();
        }
    }
}

