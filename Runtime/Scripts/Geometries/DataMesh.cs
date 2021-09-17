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
using Virgis;
using System.Collections.Generic;
using System;

public class DataMesh : VirgisFeature
{
    protected bool BlockMove = false; // is entity in a block-move state
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
        MeshCollider[] mc = GetComponents<MeshCollider>();
        MeshRenderer mr = GetComponent<MeshRenderer>();
        mr.material = mat;
        mf.mesh = mesh.ToMesh();
        mc[0].sharedMesh = mf.mesh;
        Mesh imesh = new Mesh();
        imesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        imesh.vertices = mf.mesh.vertices;
        imesh.uv = mf.mesh.uv;
        int[] t = mf.mesh.triangles;
        Array.Reverse(t);
        imesh.triangles = t;
        mc[1].sharedMesh = imesh;
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
