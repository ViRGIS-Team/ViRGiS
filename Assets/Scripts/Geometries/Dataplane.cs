// copyright Runette Software Ltd, 2020. All rights reserved

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

        public override void MoveAxis(MoveArgs args)
        {
            // Do nothing
        }

        public override void MoveTo(MoveArgs args)
        {
            throw new System.NotImplementedException();
        }

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

        public override Dictionary<string, object> GetMetadata() {
            Dictionary<string, object> temp = new Dictionary<string, object>(gisProperties);
            temp.Add("ID", gisId);
            return temp;
        }

        public override void SetMetadata(Dictionary<string, object> meta) {
            throw new System.NotImplementedException();
        }
    }
}