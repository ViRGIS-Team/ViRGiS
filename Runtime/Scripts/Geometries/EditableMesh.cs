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

using UnityEngine;
using g3;
using gs;
using Virgis;
using System.Collections.Generic;
using System;
using System.Linq;

public class EditableMesh : VirgisFeature
{   
    private bool m_BlockMove = false; // is entity in a block-move state
    private DMesh3 m_dmesh; // This is the mesh expressed as a DMesh3 in either local
                            // space coordinates or projected coordinates depending 
                            // on m_project 
    private int m_selectedVertex;
    private GameObject m_sphere;

    private Vector3 m_currentHit; // current hit vertex
    private int? m_currentHitTri; // current hit triangle
    private DMesh3 m_newDmesh; // used when editing the mesh to hold the new reult
    private int n = 0;
    private List<Int32> m_nRing;
    private bool m_selectOn = false;

    public override void Selected(SelectionType button) {

        if (m_selectOn)
            UnSelected(SelectionType.SELECT);
        m_selectOn = true;
        m_newDmesh = m_dmesh;
        transform.parent.SendMessage("Selected", button, SendMessageOptions.DontRequireReceiver);
        if (button == SelectionType.SELECTALL) {
            m_BlockMove = true;
        } else {
            MeshFilter mf = GetComponent<MeshFilter>();
            Mesh mesh = mf.sharedMesh;
            m_currentHitTri = null;
            m_currentHit = transform.InverseTransformPoint(AppState.instance.lastHitPosition);
            Vector3d target = m_currentHit;
            System.Random rand = new System.Random();
            int count = 0;
            //
            // The algorithm will find local optima - repeat until you get the tru optima
            // but limit the interation using a count
            //
            Int32 current = 0;
            while (count < m_dmesh.VertexCount) {
                count ++;
                //
                // choose a random starting point
                //
                current = rand.Next(0, m_dmesh.VertexCount);
                Vector3d vtx = m_dmesh.GetVertex(current);
                double currentDist = vtx.DistanceSquared(target);
                //
                // find the ring of triangles around the current point
                //
                int iter = 0;
                while (true) {
                    iter++;
                    //    throw new InvalidOperationException("Meh One Ring operation invalid : " + res.ToString());
                    //
                    // Interate through the vertices in the one Ring and find the clost to the target point
                    //
                    bool flag = false;
                    foreach (Int32 v in m_dmesh.VtxVerticesItr(current)) {
                        Vector3d thisVtx = m_dmesh.GetVertex(v);
                        double thisDist = thisVtx.DistanceSquared(target);
                        if (thisDist < currentDist) {
                            flag = true;
                            current = v;
                            vtx = thisVtx;
                            currentDist = thisDist;
                        }
                    }
                    //
                    // if the current point as closest - then  have a local optima
                    //
                    if (!flag)
                        break;
                }
                //
                // we now have a local optima
                // to check for a global optima look to see if hit is contained by one of the triangles in the one ring
                //
                bool f2 = false;
                foreach (Int32 t in m_dmesh.VtxTrianglesItr(current)) {
                    Index3i tri = m_dmesh.GetTriangle(t);
                    Triangle3d triangle = new Triangle3d(
                            m_dmesh.GetVertex(tri.a),
                            m_dmesh.GetVertex(tri.b),
                            m_dmesh.GetVertex(tri.c)
                        );
                    double[] xs = new double[3] { triangle.V0.x, triangle.V1.x, triangle.V2.x };
                    double[] ys = new double[3] { triangle.V0.y, triangle.V1.y, triangle.V2.y };
                    double[] zs = new double[3] { triangle.V0.z, triangle.V1.z, triangle.V2.z };

                    if (
                        target.x >= xs.Min() &&
                        target.x <= xs.Max() &&
                        target.y >= ys.Min() &&
                        target.y <= ys.Max() &&
                        target.z >= zs.Min() &&
                        target.z <= zs.Max()
                        ) {
                        f2 = true;
                        m_currentHitTri = t;
                    }
                }
                //
                // if we found on triangle that contain the current hit then we have finished
                //
                if (f2)
                    break;
            }
            if (count >= m_dmesh.VertexCount) {
                //
                // This is the unoptimized verion but it is guaranteed to find a solution if one exits
                //

                current = -1;
                float currentDist = float.MaxValue;
                if (m_currentHit != null) {
                    for (int i = 0; i < mesh.vertices.Length; i++) {
                        Vector3 vtx = mesh.vertices[i];
                        float dist = (m_currentHit - vtx).sqrMagnitude;
                        if (dist < currentDist) {
                            current = i;
                            currentDist = dist;
                        }
                    }
                }
                //
                // Check that the closest vertex to the point is an actual solution
                //
                bool f2 = false;
                foreach (Int32 t in m_dmesh.VtxTrianglesItr(current)) {
                    Index3i tri = m_dmesh.GetTriangle(t);
                    Triangle3d triangle = new Triangle3d(
                            m_dmesh.GetVertex(tri.a),
                            m_dmesh.GetVertex(tri.b),
                            m_dmesh.GetVertex(tri.c)
                        );
                    double[] xs = new double[3] { triangle.V0.x, triangle.V1.x, triangle.V2.x };
                    double[] ys = new double[3] { triangle.V0.y, triangle.V1.y, triangle.V2.y };
                    double[] zs = new double[3] { triangle.V0.z, triangle.V1.z, triangle.V2.z };

                    // Use an AABB appraoch to check hit
                    if (
                        target.x >= xs.Min() &&
                        target.x <= xs.Max() &&
                        target.y >= ys.Min() &&
                        target.y <= ys.Max() &&
                        target.z >= zs.Min() &&
                        target.z <= zs.Max()
                        ) {
                        f2 = true;
                        m_currentHitTri = t;
                    }
                }
                //
                // if we found one triangle that contain the current hit then we have finished
                //
                if (!f2) {
                    Debug.LogError(" Mesh Vertex Search : No Solution Found");
                    m_selectOn = false;
                    return;
                }
            }
            m_selectedVertex = current;
            m_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_sphere.transform.position = transform.TransformPoint(mesh.vertices[current]);
            m_sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            m_sphere.transform.parent = transform;
        }
    }

    public new void UnSelected(SelectionType button) {
        m_selectOn = false;
        m_selectedVertex = 0;
        transform.parent.SendMessage("UnSelected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
        m_BlockMove = false;
        Destroy(m_sphere);
        MeshCollider[] mc = GetComponents<MeshCollider>();
        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh mesh = mf.sharedMesh;
        mc[0].sharedMesh = mesh;
        mc[1].sharedMesh = ReverseMesh(mesh);
        m_dmesh = m_newDmesh;
        n = -1;
    }

    public override void MoveTo(MoveArgs args) {
        if (m_BlockMove) {
            if (args.translate != Vector3.zero) {
                transform.Translate(args.translate, Space.World);
                transform.parent.SendMessage("Translate", args);
            }
        } else {
            if (args.translate != Vector3.zero && m_selectOn) {
                MeshFilter mf = GetComponent<MeshFilter>();
                Mesh mesh = mf.sharedMesh;
                Vector3 localTranslate = transform.InverseTransformVector(args.translate);
                Vector3d target = mesh.vertices[m_selectedVertex] + localTranslate;
                if (AppState.instance.editScale > 2) {
                    if (n != AppState.instance.editScale) {
                        n = AppState.instance.editScale;
                        //
                        // first we need to find the n-ring of vertices
                        //
                        // first get 1-ring
                        m_nRing = new List<int>();
                        List<Int32> inside = new List<int>();
                        m_nRing.Add(m_selectedVertex);
                        for (int i = 0; i < n; i++) {
                            int[] working = m_nRing.ToArray();
                            m_nRing.Clear();
                            foreach (int v in working) {
                                if (!inside.Contains(v))
                                    foreach (int vring in m_dmesh.VtxVerticesItr(v)) {
                                        if (!inside.Contains(vring))
                                            m_nRing.Add(vring);
                                    }
                                inside.Add(v);
                            }
                        }
                    }
                    //
                    // create the deformer
                    // set the constraint that the selected vertex is moved to position
                    // set the contraint that the n-ring remains stationary
                    //
                    LaplacianMeshDeformer deform = new LaplacianMeshDeformer(m_dmesh);
                    deform.SetConstraint(m_selectedVertex, target, 1, true);
                    foreach (int v in m_nRing) {
                        deform.SetConstraint(v, m_dmesh.GetVertex(v), 10, false);
                    }
                    deform.SolveAndUpdateMesh();
                    m_newDmesh = deform.Mesh;
                } else {
                    m_dmesh.SetVertex(m_selectedVertex, target);
                }
                //
                // reset the Unity meh
                // 
                if (m_sphere != null)
                    m_sphere.transform.localPosition = (Vector3) m_dmesh.GetVertex(m_selectedVertex);
                List<Vector3> vtxs = new List<Vector3>();
                foreach (int v in m_newDmesh.VertexIndices())
                    vtxs.Add((Vector3)m_newDmesh.GetVertex(v));
                mesh.vertices = vtxs.ToArray();
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
            }
        }
    }

    /// https://answers.unity.com/questions/14170/scaling-an-object-from-a-different-center.html
    public override void MoveAxis(MoveArgs args) {
        if (GetComponent<MeshFilter>().sharedMesh.bounds.Contains(transform.InverseTransformPoint(args.pos)));
        {
            if (args.translate != Vector3.zero)
                transform.Translate(args.translate, Space.World);
            args.rotate.ToAngleAxis(out float angle, out Vector3 axis);
            transform.RotateAround(args.pos, axis, angle);
            Vector3 A = transform.localPosition;
            Vector3 B = transform.parent.InverseTransformPoint(args.pos);
            Vector3 C = A - B;
            float RS = args.scale;
            Vector3 FP = B + C * RS;
            if (FP.magnitude < float.MaxValue) {
                transform.localScale = transform.localScale * RS;
                transform.localPosition = FP;
            }
            transform.parent.SendMessage("MoveAxis", args);
        }
    }

    /// <summary>
    /// Draws the mesh represented by dmeshin - which should be in local space coordinates
    /// i.i Map Space coordinates without CRs or projection but that could be affected by 
    /// layer level scaling
    /// </summary>
    /// <param name="dmeshin"></param>
    /// <param name="mat">Material to be used for mesh normally</param>
    /// <param name="Wf">Wirframe material to be used when the mesh is being edited</param>
    /// <returns></returns>
    public Transform Draw(DMesh3 dmeshin, Material mat, Material Wf) {
        mainMat = mat;
        selectedMat = Wf;
        m_dmesh = dmeshin.Compactify();
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshCollider[] mc = GetComponents<MeshCollider>();
        mr = GetComponent<MeshRenderer>();
        mr.material = mainMat;
        Mesh umesh = m_dmesh.ToMesh();
        umesh.RecalculateBounds();
        umesh.RecalculateNormals();
        umesh.RecalculateTangents();
        mf.mesh = umesh;
        mc[0].sharedMesh = mf.sharedMesh;
        mc[1].sharedMesh = ReverseMesh(mf.sharedMesh);
        return transform;
    }

    private Mesh ReverseMesh(Mesh mesh) {
        Mesh imesh = new Mesh();
        imesh.MarkDynamic();
        imesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        imesh.vertices = mesh.vertices;
        imesh.uv = mesh.uv;
        int[] t = mesh.triangles;
        Array.Reverse(t);
        imesh.triangles = t;
        return imesh;
    }

    public DMesh3 GetMesh() {
        MeshFilter mf = GetComponent<MeshFilter>();
        return mf.mesh.ToDmesh(transform);
    }

    public override Dictionary<string, object> GetMetadata() {
        if (m_dmesh != null)
            return m_dmesh.FindMetadata("properties") as Dictionary<string, object>;
        else
            return transform.parent.GetComponent<IVirgisFeature>().GetMetadata();
    }

    public override void SetMetadata(Dictionary<string, object> meta) {
        throw new NotImplementedException();
    }

    public override void OnEdit(bool inSession) {
        if (inSession) {
            mr.material = selectedMat;
        } else {
            mr.material = mainMat;
        }
    }

    public void MakeKinematic() {
        MeshCollider[] mcs = gameObject.GetComponents<MeshCollider>();
        mcs.ToList().ForEach(item => Destroy(item));
    }

    public override VirgisFeature AddVertex(Vector3 position) {
        if (m_currentHit != null && m_currentHitTri != null) {
            Vector3d V0 = new Vector3d();
            Vector3d V1 = new Vector3d();
            Vector3d V2 = new Vector3d();
            m_newDmesh.GetTriVertices(m_currentHitTri.Value, ref V0, ref V1, ref V2);
            Index3i tri = m_newDmesh.GetTriangle(m_currentHitTri.Value);
            Vector3d bari = MathUtil.BarycentricCoords(m_currentHit, V0, V1, V2);
            int edgeId = 0;
            if (bari.x > bari.y && bari.x > bari.z)
                edgeId = m_newDmesh.FindEdgeFromTri(tri.b, tri.c, m_currentHitTri.Value);
            if (bari.y > bari.x && bari.y > bari.z)
                edgeId = m_newDmesh.FindEdgeFromTri(tri.a, tri.c, m_currentHitTri.Value);
            if (bari.z > bari.y && bari.z > bari.x)
                edgeId = m_newDmesh.FindEdgeFromTri(tri.b, tri.a, m_currentHitTri.Value);
            DMesh3.EdgeSplitInfo result = new DMesh3.EdgeSplitInfo();
            m_newDmesh.SplitEdge(edgeId, out result);
            Mesh tempMesh = m_newDmesh.ToMesh();
            tempMesh.RecalculateBounds();
            tempMesh.RecalculateNormals();
            MeshFilter mf = GetComponent<MeshFilter>();
            mf.mesh = tempMesh;
            UnSelected(SelectionType.SELECT);
        }
        return this;
    }

    /// <summary>
    /// This is called when you want to delete the currently selected vertex
    /// </summary>
    public void Delete() {
        MeshFilter mf = GetComponent<MeshFilter>();
        m_newDmesh.RemoveVertex(m_selectedVertex, true, true);
        if (m_dmesh.IsClosed()) {
            MeshAutoRepair mr = new MeshAutoRepair(m_newDmesh);
            mr.Apply();
            m_newDmesh = mr.Mesh;
        } else {
            m_newDmesh.CompactInPlace();
        }
        Mesh tempMesh = m_newDmesh.ToMesh();
        tempMesh.RecalculateBounds();
        tempMesh.RecalculateNormals();
        mf.mesh = tempMesh;
        UnSelected(SelectionType.SELECT);
    }
}
