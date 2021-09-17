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
using System.Linq;
using System;

namespace Virgis
{
    /// <summary>
    /// Controls an instance of a Polygon ViRGIS component
    /// </summary>
    public class Datapolygon : Datashape {

        private float _tiling_size;

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
            transform.parent.GetComponent<IVirgisEntity>().MoveAxis(args);
            transform.GetComponentsInChildren<Dataline>().ToList<Dataline>().ForEach(line => line.MoveAxisAction(args));
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
        public GameObject Draw(List<Dataline> polygon, Material mat, float tiling_size = 10) {

            _tiling_size = tiling_size;
            
            lines = polygon;

            Shape = Instantiate(shapePrefab, transform);
            MeshRenderer mr = Shape.GetComponent<MeshRenderer>();
            mr.material = mat;

            // call the generic polygon draw function in DataShape
            _redraw();

            mat.SetVector("_Tiling", new Vector2(scaleX / tiling_size, scaleY / tiling_size));
            return gameObject;
        }

        /// <summary>
        /// Move a vertex of the polygon and recreate the mesh
        /// </summary>
        /// <param name="data">MoveArgs</param>
        public void ShapeMoveVertex(MoveArgs data) {
            Mesh mesh = Shape.GetComponent<MeshFilter>().mesh;
            MeshCollider mc = Shape.GetComponent<MeshCollider>();
            MeshRenderer mr = Shape.GetComponent<MeshRenderer>();
            Material mat = mr.material;
            Destroy(mc);
            mc = Shape.AddComponent<MeshCollider>();
            Vector3[] vertices = mesh.vertices;
            vertices[VertexTable.Find(item => item.Id == data.id).pVertex] = Shape.transform.InverseTransformPoint(data.pos);
            mesh.vertices = vertices;
            mesh.uv = BuildUVs(vertices);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            try {
                Physics.BakeMesh(mesh.GetInstanceID(), false);
                mc.sharedMesh = mesh;
            } catch (Exception e) {
                Debug.Log(e.ToString());
            }
            mat.SetVector("_Tiling", new Vector2(scaleX / _tiling_size, scaleY / _tiling_size));

        }

        public override Dictionary<string, object> GetMetadata() {
            return feature.GetAll();
        }

        public override void SetMetadata(Dictionary<string, object> meta) {
            throw new NotImplementedException();
        }
    }
}

