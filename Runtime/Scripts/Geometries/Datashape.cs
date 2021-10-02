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

using System.Collections.Generic;
using UnityEngine;
using g3;
using System.Linq;
using System;
using DelaunatorSharp;

namespace Virgis
{
    /// <summary>
    /// Controls an instance of a Polygon ViRGIS component
    /// </summary>
    public class Datashape : VirgisFeature {

        public GameObject shapePrefab;
        protected GameObject Shape; // gameObject to be used for the shape
        protected List<VertexLookup> VertexTable = new List<VertexLookup>();
        protected List<Dataline> lines;
        protected List<DCurve3> Polygon;
        protected float scaleX;
        protected float scaleY;

        public override void Selected(SelectionType button) {
            if (button == SelectionType.SELECTALL) {
                gameObject.BroadcastMessage("Selected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
                m_blockMove = true;
                GetComponentsInChildren<Dataline>().ToList<Dataline>().ForEach(item => item.Selected(SelectionType.SELECTALL));
            }
        }

        public override void UnSelected(SelectionType button) {
            if (button != SelectionType.BROADCAST) {
                gameObject.BroadcastMessage("UnSelected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
                m_blockMove = false;
            }
        }

        public override void MoveTo(MoveArgs args) {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Makes the actual mesh
        /// </summary>
        protected void _redraw() {
            if (lines.Count > 0) {
                Polygon = new List<DCurve3>();
                foreach (Dataline ring in lines) {
                    foreach (VertexLookup v in ring.VertexTable) {
                        VertexTable.Add(v);
                    }
                    DCurve3 curve = new DCurve3();
                    curve.Vector3(ring.GetVertexPositions(), true);
                    Polygon.Add(curve);
                }
            }

            MeshFilter mf = Shape.GetComponent<MeshFilter>();
            MeshCollider[] mc = Shape.GetComponents<MeshCollider>();
            mf.mesh = null;
            Mesh mesh = new Mesh();
            Mesh imesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            imesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            Frame3f frame = new Frame3f();
            Vector3[] vertices;
            GeneralPolygon2d polygon2d;
            Delaunator delaunator;
            List<int> triangles = new List<int>();

            try {

                //
                // Map 3d Polygon to the bext fit 2d polygon and also return the frame used for the mapping
                //
                polygon2d = Polygon.ToPolygon(ref frame);

                //
                // calculate the dalaunay triangulation of the 2d polygon
                //
                delaunator = new Delaunator(polygon2d.AllVerticesItr().ToPoints());

                IEnumerable<Vector2d> vlist = delaunator.Points.ToVectors2d();
                vertices = new Vector3[vlist.Count()];

                //
                // for each vertex in the dalaunay triangulatin - map back to a 3d point and also populate the vertex table
                //
                for (int i = 0; i < vlist.Count(); i++) {
                    Vector2d v = vlist.ElementAt(i);
                    try {
                        Vector3d v1 = Polygon.AllVertexItr().Find(item => v.Distance(frame.ToPlaneUV((Vector3f)item, 2)) < 0.001);
                        vertices[i] = Shape.transform.InverseTransformPoint((Vector3)v1);
                        VertexLookup vl = VertexTable.Find(item => v1.Distance(item.Com.transform.position) < 0.001);
                        if (vl != null) vl.pVertex = i;
                    } catch {
                        Debug.Log("Mesh Error");
                    }
                }

                // 
                // eaxtract the triangles from the delaunay triangulation 
                //
                IEnumerable<ITriangle> tris =   delaunator.GetTriangles();
                for (int i = 0; i < tris.Count(); i++) {

                    ITriangle tri = tris.ElementAt(i);

                    if (polygon2d.Contains(tri.CetIncenter())) {
                        int index = 3 * i;
                        triangles.Add(delaunator.Triangles[index]);
                        triangles.Add(delaunator.Triangles[index + 1]);
                        triangles.Add(delaunator.Triangles[index + 2]);
                    } 
                }
            } catch (Exception e) {
                throw new Exception("feature is not a valid Polygon : " + e.ToString());
            }

            //
            // build the mesh entity
            //
            mesh.vertices = vertices;
            mesh.triangles = triangles.ToArray();
            mesh.uv = BuildUVs(vertices);

            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            imesh.vertices = mesh.vertices;
            imesh.triangles = triangles.Reverse<int>().ToArray();
            imesh.uv = mesh.uv;

            imesh.RecalculateBounds();
            imesh.RecalculateNormals();

            mf.mesh = mesh;
            try {
                mc[0].sharedMesh = mesh;
                mc[1].sharedMesh = imesh;
            } catch (Exception e) {
                Debug.Log(e.ToString());
            }
        }

        public override VirgisFeature AddVertex(Vector3 position) {
            _redraw();
            return base.AddVertex(position);
        }

        public override void RemoveVertex(VirgisFeature vertex) {
            if (m_blockMove) {
                Destroy(gameObject);
            } else {
                _redraw();
            }
        }

        /// <summary>
        /// Builds the UV values for thw mesh represented by the vertices 
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        protected Vector2[] BuildUVs(Vector3[] vertices) {
            List<Vector2> ret = new List<Vector2>();
            List<Vector3d> vertices3d = vertices.ToList<Vector3>().ConvertAll(item => (Vector3d)item);

            //
            // create a UV mapping plane
            // to make image planes work  - we assume that the origin of UV plane is the last vertex
            //
            OrthogonalPlaneFit3 orth = new OrthogonalPlaneFit3(vertices3d);
            Frame3f frame = new Frame3f( vertices[vertices.Length - 1], -1 * orth.Normal);

            //
            // check the orientation of the plane in UV space.
            // for image planes  - we assume that the x direction from the first point to the second point should always be positive
            // if not - reverse the frame
            //
            if (Math.Sign(
                frame.ToPlaneUV((Vector3f) vertices3d[0], 2).x -
                frame.ToPlaneUV((Vector3f) vertices3d[1], 2).x
                ) > -1) {
                frame = new Frame3f(vertices[vertices.Length - 1], orth.Normal);
            }

            //
            // map all of the points to UV space
            //
            foreach (Vector3d v in vertices3d) {
                ret.Add(frame.ToPlaneUV((Vector3f)v,2));
            }

            //
            // normalize UVs to [0..1, 0..1]
            //
            float maxX = float.NegativeInfinity;
            float maxY = float.NegativeInfinity;
            float minX = float.PositiveInfinity;
            float minY = float.PositiveInfinity;

            for (int i = 0; i < ret.Count; i++) {
                Vector2 v = ret[i];
                maxX = maxX > v.x ? maxX : v.x;
                minX = minX < v.x ? minX : v.x;
                maxY = maxY > v.y ? maxY : v.y;
                minY = minY < v.y ? minY : v.y;
            }

            scaleX = maxX - minX;
            scaleY = maxY - minY;


            for (int i = 0; i < ret.Count; i++) {
                ret[i] = new Vector2( (ret[i].x - minX) / scaleX, (ret[i].y - minY) / scaleY);
            }
            return ret.ToArray();
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

        public override Dictionary<string, object> GetMetadata() {
            return default;
        }

        public override void SetMetadata(Dictionary<string, object> meta) {
            throw new NotImplementedException();
        }
    }
}