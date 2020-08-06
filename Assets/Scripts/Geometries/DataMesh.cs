using UnityEngine;
using g3;
using Virgis;

public class DataMesh : VirgisFeature
{
    private bool BlockMove = false; // is entity line in a block-move state
    private DMesh3 mesh;

    private Vector3 firstHitPosition = Vector3.zero;
    private bool nullifyHitPos = true;
    public override void Selected(SelectionType button) {
        nullifyHitPos = true;
        transform.parent.SendMessage("Selected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
        if (button == SelectionType.SELECTALL) {
            BlockMove = true;
        }
    }

    public override void UnSelected(SelectionType button) {
        transform.parent.SendMessage("UnSelected", SelectionType.BROADCAST, SendMessageOptions.DontRequireReceiver);
        BlockMove = false;
    }

    public override void MoveTo(MoveArgs args) {
        transform.parent.SendMessage("Translate", args);
    }

    public override void MoveAxis(MoveArgs args) {
        if (nullifyHitPos)
            firstHitPosition = args.pos;
        args.pos = firstHitPosition;
        transform.parent.SendMessage("MoveAxis", args);
        nullifyHitPos = false;
    }

    public Transform Draw(DMesh3 mesh, Material mat) {
        this.mesh = mesh;
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshCollider mc = GetComponent<MeshCollider>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        mr.material = mat;
        mf.mesh = mesh.ToMesh();
        mc.sharedMesh = mf.mesh;
        return transform;
    }

    public DMesh3 GetMesh() {
        return mesh;
    }

}
