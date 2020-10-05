﻿using UnityEngine;
using g3;
using Virgis;
using System.Collections.Generic;

public class DataMesh : VirgisFeature
{
    private bool BlockMove = false; // is entity in a block-move state
    private DMesh3 mesh;

    private Vector3 firstHitPosition = Vector3.zero;
    private bool nullifyHitPos = true;
    public override void Selected(SelectionType button) {
        nullifyHitPos = true;
        transform.parent.SendMessage("Selected", button, SendMessageOptions.DontRequireReceiver);
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
        transform.parent.GetComponent<IVirgisEntity>().MoveAxis(args);
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

    public override Dictionary<string, object> GetMetadata() {
        if (mesh != null)
            return mesh.FindMetadata("properties") as Dictionary<string, object>;
        else
            return transform.parent.GetComponent<IVirgisFeature>().GetMetadata();
    }

    public override void SetMetadata(Dictionary<string, object> meta) {
        throw new System.NotImplementedException();
    }
}
