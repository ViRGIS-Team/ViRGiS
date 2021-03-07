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
    private Int32 currentHitTri;

    public override void Selected(SelectionType button) {
        nullifyHitPos = true;
        transform.parent.SendMessage("Selected", button, SendMessageOptions.DontRequireReceiver);
        if (button == SelectionType.SELECTALL) {
            BlockMove = true;
        } else {
            MeshFilter mf = GetComponent<MeshFilter>();
            Mesh mesh = mf.sharedMesh;
            currentHit = transform.InverseTransformVector(AppState.instance.lastHitPosition);
            Vector3d target = currentHit;
            //
            // ToDo - This is not very optimized - could use directional vectors and triangls to always move closr to the point 
            //
            //int current = -1;
            //float currentDist = float.MaxValue;
            //if (currentHit != null) {
            //    for (int i = 0; i < mesh.vertices.Length; i++) {
            //        Vector3 vtx = mesh.vertices[i];
            //        float dist = (currentHit - vtx).sqrMagnitude;
            //        if (dist < currentDist) {
            //            current = i;
            //            currentDist = dist;
            //        }
            //    }
            //}
            System.Random rand = new System.Random();
            int count = 0;
            //
            // The algorithm will find local optima - o repeat until you get the tru optima
            // but limit the interation using a count
            //
            Int32 current = 0;
            while (count < 10) {
                count ++;
                //
                // choose a random starting point
                //
                current = rand.Next(0, dmesh.VertexCount);
                Vector3d vtx = dmesh.GetVertex(current);
                double currentDist = vtx.DistanceSquared(target);
                //
                // find the ring of triangles arunf the current point
                //
                int iter = 0;
                while (true) {
                    iter++;
                    //List<Int32> oneRing = new List<Int32>();
                    //MeshResult res = dmesh.GetVtxTriangles(current, oneRing, true);
                    //if (res != MeshResult.Ok)
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
                    }
                }
                //
                // if e found on triamgle that contain the current hit then we have finished
                //
                if (f2)
                    break;
            }
            selectedVertex = current;
            sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = transform.TransformVector(mesh.vertices[current]);
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
        Vector3 v = mesh.vertices[selectedVertex];
        Mesh imesh = mc[1].sharedMesh;
        Vector3[] vtxs = imesh.vertices;
        vtxs[selectedVertex] = v;
        imesh.vertices = vtxs;
        dmesh.SetVertex(selectedVertex, v);
    }

    public override void MoveTo(MoveArgs args) {
        if (BlockMove) {
            if (args.translate != Vector3.zero) {
                transform.Translate(args.translate, Space.World);
                transform.parent.SendMessage("Translate", args);
            }
        } else {
            if (args.translate != Vector3.zero) {
                Vector3 localTranslate = transform.InverseTransformVector(args.translate);
                MeshFilter mf = GetComponent<MeshFilter>();
                Mesh mesh = mf.sharedMesh;
                Vector3[] vertices = mesh.vertices;
                vertices[selectedVertex] += localTranslate;
                sphere.transform.localPosition = vertices[selectedVertex];
                mesh.vertices = vertices;
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                mesh.RecalculateTangents();
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

    public Transform Draw(DMesh3 dmeshin, Material mat) {
        dmesh = dmeshin.Compactify();
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshCollider[] mc = GetComponents<MeshCollider>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        mr.material = mat;
        Mesh umesh = dmesh.ToMesh(false);
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
}
