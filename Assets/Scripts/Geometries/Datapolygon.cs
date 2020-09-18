// copyright Runette Software Ltd, 2020. All rights reserved
// parts from https://answers.unity.com/questions/546473/create-a-plane-from-points.html

using System.Collections.Generic;
using UnityEngine;
using g3;
using System.Linq;
using System;
using DelaunatorSharp;
using DelaunatorSharp.Unity.Extensions;

namespace Virgis
{


    /// <summary>
    /// Controls an instance of a Polygon ViRGIS component
    /// </summary>
    public class Datapolygon : VirgisFeature {

        private bool BlockMove = false; // Is this component in a block move state
        private GameObject Shape; // gameObject to be used for the shape
        public List<VertexLookup> VertexTable = new List<VertexLookup>();
        public List<Dataline> Polygon;


        public override void Selected(SelectionType button) {
            if (button == SelectionType.SELECTALL) {
                gameObject.BroadcastMessage("Selected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
                BlockMove = true;
                GetComponentsInChildren<Dataline>().ToList<Dataline>().ForEach(item => item.Selected(SelectionType.SELECTALL));
            }
        }

        public override void UnSelected(SelectionType button) {
            if (button != SelectionType.BROADCAST) {
                gameObject.BroadcastMessage("UnSelected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
                BlockMove = false;
            }
        }

        public override void VertexMove(MoveArgs data) {
            if (!BlockMove) {
                ShapeMoveVertex(data);
            }
        }

        public override void Translate(MoveArgs args) {
            if (BlockMove) {
                transform.Translate(args.translate, Space.World);
            }

        }

        // https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
        public override void MoveAxis(MoveArgs args) {
            if (args.translate != null) {
                Shape.transform.Translate(args.translate, Space.World);
            }
            args.rotate.ToAngleAxis(out float angle, out Vector3 axis);
            Shape.transform.RotateAround(args.pos, axis, angle);
            Vector3 A = Shape.transform.localPosition;
            Vector3 B = transform.InverseTransformPoint(args.pos);
            Vector3 C = A - B;
            float RS = args.scale;
            Vector3 FP = B + C * RS;
            if (FP.magnitude < float.MaxValue) {
                Shape.transform.localScale = Shape.transform.localScale * RS;
                Shape.transform.localPosition = FP;
            }
        }

        public override void MoveTo(MoveArgs args) {
            throw new System.NotImplementedException();
        }



        /// <summary>
        /// Called to draw the Polygon based upon the 
        /// </summary>
        /// <param name="perimeter">LineString defining the perimter of the polygon</param>
        /// <param name="mat"> Material to be used</param>
        /// <returns></returns>
        public GameObject Draw(List<Dataline> polygon, Material mat = null) {

            Polygon = polygon;

            Shape = new GameObject("Polygon Shape");
            Shape.transform.parent = gameObject.transform;


            MakeMesh();

            Renderer rend = Shape.GetComponent<MeshRenderer>();
            if (rend == null) {
                rend = Shape.AddComponent<MeshRenderer>();
                rend.material = mat;
            };

            return gameObject;

        }

        /// <summary>
        /// Generates the actual mesh for the polyhedron
        /// </summary>
        private void MakeMesh() {
            MeshFilter mf;
            mf = Shape.GetComponent<MeshFilter>();
            if (mf == null) mf = Shape.AddComponent<MeshFilter>();
            mf.mesh = null;
            Mesh mesh = new Mesh();
            //TriangulatedPolygonGenerator tpg = new TriangulatedPolygonGenerator();
            Frame3f frame = new Frame3f();
            Vector3[] vertices;
            GeneralPolygon2d polygon2d;
            Delaunator delaunator;
            List<int> triangles = new List<int>();

            try {

                polygon2d = Polygon.ToPolygon(ref frame);
                delaunator = new Delaunator(polygon2d.AllVerticesItr().ToPoints());
                VertexTable.Clear();

                foreach (Dataline ring in Polygon) {
                    foreach (VertexLookup v in ring.VertexTable) {
                        VertexTable.Add(v);
                    }
                }
                IEnumerable<Vector2d> vlist = delaunator.Points.ToVectors2d();
                vertices = new Vector3[vlist.Count()];

                for (int i = 0; i < vlist.Count(); i++) {
                    Vector2d v = vlist.ElementAt(i);
                    try {
                        VertexLookup vl = VertexTable.Find(item => v.Distance(frame.ToPlaneUV(item.Com.transform.position, 3)) < 0.001);
                        vertices[i] = Shape.transform.InverseTransformPoint(vl.Com.transform.position);
                        vl.pVertex = i;
                    } catch {
                        Debug.Log("UhU");
                    }
                }

                IEnumerable<ITriangle> tris =   delaunator.GetTriangles();
                for (int i = 0; i < tris.Count(); i++) {

                    ITriangle tri = tris.ElementAt(i);
                    //Segment2d a = new Segment2d(tri.Points.ElementAt(0).ToVector2d(), tri.Points.ElementAt(1).ToVector2d());
                    //Segment2d b = new Segment2d(tri.Points.ElementAt(1).ToVector2d(), tri.Points.ElementAt(2).ToVector2d());
                    //Segment2d c = new Segment2d(tri.Points.ElementAt(2).ToVector2d(), tri.Points.ElementAt(0).ToVector2d());

                    //bool fail = false;

                    //foreach (Segment2d seg in new Segment2d[3] { a, b, c }) {
                    //    if (polygon2d.IsOutside(seg)) {
                    //        fail = true;
                    //    }
                    //}

                    //if (fail)
                    //    continue;

                    if (polygon2d.Contains(delaunator.GetTriangleCenter(i).ToVector2d())) {
                        int index = 3 * i;
                        triangles.Add(delaunator.Triangles[index]);
                        triangles.Add(delaunator.Triangles[index + 1]);
                        triangles.Add(delaunator.Triangles[index + 2]);
                    }
                    
                }

            } catch {
                throw new Exception("feature is not a valid Polygon");
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles.ToArray();
            mesh.uv = delaunator.Points.ToVectors2();

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            mf.mesh = mesh;
        }


        /// <summary>
        /// Move a vertex of the polygon and recreate the mesh
        /// </summary>
        /// <param name="data">MoveArgs</param>
        public void ShapeMoveVertex(MoveArgs data) {
            Mesh mesh = Shape.GetComponent<MeshFilter>().mesh;
            Vector3[] vertices = mesh.vertices;
            vertices[VertexTable.Find(item => item.Id == data.id).pVertex] = Shape.transform.InverseTransformPoint(data.pos);
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
            MakeMesh();
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
                result[i] = VertexTable.Find(item => item.isVertex && item.pVertex == i).Com as Datapoint;
            }
            return result;
        }
    }
}

