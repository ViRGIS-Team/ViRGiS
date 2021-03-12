using UnityEngine;
using g3;
using Virgis;
using System.Collections.Generic;
using System;
using System.Linq;

public class EditableMesh : VirgisFeature
{   
    private bool BlockMove = false; // is entity in a block-move state
    private DMesh3 dmesh;
    private int selectedVertex;
    private GameObject sphere;

    private Vector3 firstHitPosition = Vector3.zero;
    private bool nullifyHitPos = true;
    private Vector3 currentHit;
    private Index3i currentHitTri;
    private DMesh3 newDmesh;
    private int n = 0;
    private List<Int32> nRing;

    public override void Selected(SelectionType button) {
        nullifyHitPos = true;
        transform.parent.SendMessage("Selected", button, SendMessageOptions.DontRequireReceiver);
        if (button == SelectionType.SELECTALL) {
            BlockMove = true;
        } else {
            MeshFilter mf = GetComponent<MeshFilter>();
            Mesh mesh = mf.sharedMesh;
            currentHit = transform.InverseTransformPoint(AppState.instance.lastHitPosition);
            Vector3d target = currentHit;
            System.Random rand = new System.Random();
            int count = 0;
            //
            // The algorithm will find local optima - repeat until you get the tru optima
            // but limit the interation using a count
            //
            Int32 current = 0;
            while (count < dmesh.VertexCount) {
                count ++;
                //
                // choose a random starting point
                //
                current = rand.Next(0, dmesh.VertexCount);
                Vector3d vtx = dmesh.GetVertex(current);
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
                    foreach (Int32 v in dmesh.VtxVerticesItr(current)) {
                        Vector3d thisVtx = dmesh.GetVertex(v);
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
                foreach (Int32 t in dmesh.VtxTrianglesItr(current)) {
                    Index3i tri = dmesh.GetTriangle(t);
                    Triangle3d triangle = new Triangle3d(
                            dmesh.GetVertex(tri.a),
                            dmesh.GetVertex(tri.b),
                            dmesh.GetVertex(tri.c)
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
                        currentHitTri = tri;
                    }
                }
                //
                // if we found on triamgle that contain the current hit then we have finished
                //
                if (f2)
                    break;
            }
            if (count >= dmesh.VertexCount) {
                //
                // This is the unoptimized verion but it is guaranteed to find a solution if one exits
                //

                current = -1;
                float currentDist = float.MaxValue;
                if (currentHit != null) {
                    for (int i = 0; i < mesh.vertices.Length; i++) {
                        Vector3 vtx = mesh.vertices[i];
                        float dist = (currentHit - vtx).sqrMagnitude;
                        if (dist < currentDist) {
                            current = i;
                            currentDist = dist;
                        }
                    }
                }
                //
                // Check that the closet vertex to the point is an actual solution
                //
                bool f2 = false;
                foreach (Int32 t in dmesh.VtxTrianglesItr(current)) {
                    Index3i tri = dmesh.GetTriangle(t);
                    Triangle3d triangle = new Triangle3d(
                            dmesh.GetVertex(tri.a),
                            dmesh.GetVertex(tri.b),
                            dmesh.GetVertex(tri.c)
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
                        currentHitTri = tri;
                    }
                }
                //
                // if we found on triamgle that contain the current hit then we have finished
                //
                if (!f2) {
                    Debug.LogError(" Mesh Vertex Search : No Solution Found");
                    return;
                }
            }
            selectedVertex = current;
            sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = transform.TransformPoint(mesh.vertices[current]);
            sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            sphere.transform.parent = transform;
            MoveArgs moveto = new MoveArgs();
            moveto.translate = AppState.instance.lastHitPosition - sphere.transform.position;
            MoveTo(moveto);
        }
    }

    public override void UnSelected(SelectionType button) {
        transform.parent.SendMessage("UnSelected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
        BlockMove = false;
        Destroy(sphere);
        MeshCollider[] mc = GetComponents<MeshCollider>();
        MeshFilter mf = GetComponent<MeshFilter>();
        Mesh mesh = mf.sharedMesh;
        mc[0].sharedMesh = mesh;
        Mesh imesh = mc[1].sharedMesh;
        imesh.vertices = mesh.vertices;
        mc[1].sharedMesh = imesh;
        dmesh = newDmesh;
        n = -1;
    }

    public override void MoveTo(MoveArgs args) {
        if (BlockMove) {
            if (args.translate != Vector3.zero) {
                transform.Translate(args.translate, Space.World);
                transform.parent.SendMessage("Translate", args);
            }
        } else {
            if (args.translate != Vector3.zero) {
                MeshFilter mf = GetComponent<MeshFilter>();
                Mesh mesh = mf.sharedMesh;
                Vector3 localTranslate = transform.InverseTransformVector(args.translate);
                Vector3d target = dmesh.GetVertex(selectedVertex) + localTranslate;
                if (AppState.instance.editScale > 2) {
                    if (n != AppState.instance.editScale) {
                        n = AppState.instance.editScale;
                        //
                        // first we need to find the n-ring of vertices
                        //
                        // first get 1-ring
                        nRing = new List<int>();
                        List<Int32> inside = new List<int>();
                        nRing.Add(selectedVertex);
                        for (int i = 0; i < n; i++) {
                            int[] working = nRing.ToArray();
                            nRing.Clear();
                            foreach (int v in working) {
                                if (!inside.Contains(v))
                                    foreach (int vring in dmesh.VtxVerticesItr(v)) {
                                        if (!inside.Contains(vring))
                                            nRing.Add(vring);
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
                    LaplacianMeshDeformer deform = new LaplacianMeshDeformer(dmesh);
                    deform.SetConstraint(selectedVertex, target, 1, false);
                    foreach (int v in nRing) {
                        deform.SetConstraint(v, dmesh.GetVertex(v), 10, false);
                    }
                    deform.SolveAndUpdateMesh();
                    newDmesh = deform.Mesh;
                } else {
                    dmesh.SetVertex(selectedVertex, target);
                }
                //
                // reset the Unity meh
                // 
                if (sphere != null)
                    sphere.transform.localPosition = (Vector3) dmesh.GetVertex(selectedVertex);
                List<Vector3> vtxs = new List<Vector3>();
                foreach (int v in newDmesh.VertexIndices())
                    vtxs.Add((Vector3)newDmesh.GetVertex(v));
                mesh.vertices = vtxs.ToArray();
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
            }
        }
    }

    public override void MoveAxis(MoveArgs args) {
        if (nullifyHitPos)
            firstHitPosition = args.pos;
        args.pos = firstHitPosition;
        transform.parent.GetComponent<IVirgisEntity>().MoveAxis(args);
        nullifyHitPos = false;
    }

    public Transform Draw(DMesh3 dmeshin, Material mat, Material Wf, bool project) {
        mainMat = mat;
        selectedMat = Wf;
        dmesh = dmeshin.Compactify();
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshCollider[] mc = GetComponents<MeshCollider>();
        mr = GetComponent<MeshRenderer>();
        mr.material = mainMat;
        Mesh umesh = dmesh.ToMesh(project);
        umesh.RecalculateBounds();
        umesh.RecalculateNormals();
        umesh.RecalculateTangents();
        mf.mesh = umesh;
        mc[0].sharedMesh = mf.sharedMesh;
        Mesh imesh = new Mesh();
        imesh.MarkDynamic();
        imesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        imesh.vertices = umesh.vertices;
        imesh.uv = umesh.uv;
        int[] t = umesh.triangles;
        Array.Reverse(t);
        imesh.triangles = t;
        mc[1].sharedMesh = imesh;
        return transform;
    }

    public DMesh3 GetMesh() {
        return dmesh;
    }

    public override Dictionary<string, object> GetMetadata() {
        if (dmesh != null)
            return dmesh.FindMetadata("properties") as Dictionary<string, object>;
        else
            return transform.parent.GetComponent<IVirgisFeature>().GetMetadata();
    }

    public override void SetMetadata(Dictionary<string, object> meta) {
        throw new System.NotImplementedException();
    }

    public void OnEdit(bool inSession) {
        if (inSession) {
            mr.material = selectedMat;
        } else {
            mr.material = mainMat;
        }
    }
}
