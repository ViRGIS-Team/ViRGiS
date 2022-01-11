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

namespace Virgis
{
    /// <summary>
    /// Controls an instance of a Polygon ViRGIS component
    /// </summary>
    public class Dataplane : Datashape
    {
        public string gisId;
        public Dictionary<string, object> gisProperties;

        /// <summary>
        /// Called to draw the Polygon based upon the 
        /// </summary>
        /// <param name="perimeter">LineString defining the perimter of the polygon</param>
        /// <param name="mat"> Material to be used</param>
        /// <returns></returns>
        public GameObject Draw( Vector3[] top, Vector3[] bottom,  Material mat = null)
        {
            Polygon = new List<DCurve3>();
            lines = new List<Dataline>();
            Polygon.Add(new DCurve3(top.ToList<Vector3>().ConvertAll(item => (Vector3d)item), true));
            for (int i = 0; i < bottom.Length; i++) {
                Polygon[0].AppendVertex(bottom[bottom.Length - i - 1]);
            }

            Shape = Instantiate(shapePrefab, transform);
            MeshRenderer mr = Shape.GetComponent<MeshRenderer>();
            mr.material = mat;

            // call the generic polygon draw function from DataShape
            _redraw();
            return gameObject;
        }

        public override Dictionary<string, object> GetInfo() {
            Dictionary<string, object> temp = new Dictionary<string, object>(gisProperties);
            temp.Add("ID", gisId);
            return temp;
        }

        public override void SetInfo(Dictionary<string, object> meta) {
            throw new System.NotImplementedException();
        }
    }
}