// copyright Runette Software Ltd, 2020. All rights reserved

using UnityEngine;

namespace Virgis
{

    /// <summary>
    /// Controls an instance of a line segment
    /// </summary>
    public class LineSegment : VirgisComponent
    {
        private Renderer thisRenderer; // convenience link to the rendere for this marker

        private Vector3 start; // coords of the start of the line in Map.local space coordinates
        private Vector3 end;  // coords of the start of the line in Map.local space coordinates
        private float diameter; // Diameter of the vertex in Map.local units
        public int vStart; // Vertex ID of the start of the lins
        public int vEnd; // Vertex ID of the end of the line

        /// <summary>
        /// Called to draw the line Segment 
        /// </summary>
        /// <param name="from">starting point of the line segment in worldspace coords</param>
        /// <param name="to"> end point for the line segment in worldspace coordinates</param>
        /// <param name="vertStart">vertex ID for the vertex at the start of the line segment</param>
        /// <param name="vertEnd"> vertex ID for the vertex at the end of the line segment </param>
        /// <param name="dia">Diamtere of the line segement in Map.local units</param>


        public void Draw(Vector3 from, Vector3 to, int vertStart, int vertEnd, float dia)
        {
            start = transform.parent.InverseTransformPoint(from);
            end = transform.parent.InverseTransformPoint(to);
            diameter = dia;
            vStart = vertStart;
            vEnd = vertEnd;
            _draw();

        }
        public override void Selected(SelectionTypes button)
        {
            if (button == SelectionTypes.SELECTALL)
            {

            }
        }

        public override void UnSelected(SelectionTypes button)
        {
            if (button != SelectionTypes.BROADCAST)
            {

            }
        }


        public override void SetMaterial(Material mainMat, Material selectedMat) {
            this.mainMat = mainMat;
            this.selectedMat = selectedMat;
            thisRenderer = GetComponentInChildren<Renderer>();
            if (thisRenderer) {
                thisRenderer.material = mainMat;
            }
        }

        // Move the start of line to newStart point in World Coords
        public void MoveStart(Vector3 newStart)
        {
            start = transform.parent.InverseTransformPoint(newStart);
            _draw();
        }

        // Move the start of line to newStart point in World  Coords
        public void MoveEnd(Vector3 newEnd)
        {
            end = transform.parent.InverseTransformPoint(newEnd);
            _draw();
        }

        private void _draw()
        {

            transform.localPosition = start;
            transform.LookAt(transform.parent.TransformPoint(end));
            float length = Vector3.Distance(start, end) / 2.0f;
            Vector3 linescale = transform.parent.localScale;
            transform.localScale = new Vector3(diameter / linescale.x, diameter / linescale.y, length);
        }


        public override void Translate(MoveArgs args)
        {
            
        }

        public override void VertexMove(MoveArgs args)
        {
            
        }

        public override void MoveAxis(MoveArgs args)
        {
      
        }

        public override void MoveTo(MoveArgs args)
        {
            throw new System.NotImplementedException();
        }

        public override Vector3 GetClosest(Vector3 coords)
        {
            throw new System.NotImplementedException();
        }

        public override T GetGeometry<T>()
        {
            throw new System.NotImplementedException();
        }
    }
}
