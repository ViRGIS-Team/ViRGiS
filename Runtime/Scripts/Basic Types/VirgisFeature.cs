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

using OSGeo.OGR;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Virgis {

    /// <summary>
    /// Abstract parent for all in game entities
    /// </summary>
    public interface IVirgisEntity
    {
        void Selected(SelectionType button);
        void UnSelected(SelectionType button);
        Guid GetId();
        VirgisFeature GetClosest(Vector3 coords, Guid[] exclude);
        void MoveAxis(MoveArgs args);
        void Translate(MoveArgs args);
        void MoveTo(MoveArgs args);
        void VertexMove(MoveArgs args);
        VirgisLayer GetLayer();
        void OnEdit(bool inSession);
    }

    /// <summary>
    /// Abstract Parent for all symbology relevant in game entities
    /// </summary>
    public interface IVirgisFeature : IVirgisEntity
    {
        void SetMaterial(Material mainMat, Material selectedMat);
        //void MoveTo(Vector3 newPos);
        VirgisFeature AddVertex(Vector3 position);
        void RemoveVertex(VirgisFeature vertex);
        T GetGeometry<T>();

        void Hover(Vector3 hit);
        void UnHover();
        public Dictionary<string, object> GetInfo();
        public void SetInfo(Dictionary<string, object> meta);

    }

    public abstract class VirgisFeature : MonoBehaviour, IVirgisFeature
    {
        protected Material mainMat; // color of the component
        protected Material selectedMat; // color of the component when selected
        protected MeshRenderer mr;
        protected Vector3 m_firstHitPosition = Vector3.zero;
        protected bool m_nullifyHitPos = true;
        protected bool m_blockMove = false; // is entity in a block-move state

        private Guid _id; // internal ID for this component - used when it is part of a larger structure
        public Transform label; //  Go of the label or billboard
        public Feature feature; // Feature tht was the source for this GO
        public Vector3 lastHit; // last hit location


        void Awake()
        {
            _id = Guid.NewGuid();
        }


        /// <summary>
        /// Use to set the material of the feature
        /// </summary>
        /// <param name="mainMat"> Usual material</param>
        /// /// <param name="selectedMat"> Material to be used when feature is selected</param>
        public virtual void SetMaterial(Material mainMat, Material selectedMat) {
            this.mainMat = mainMat;
            this.selectedMat = selectedMat;
        }

        /// <summary>
        /// Use to tell the Component that it is selected
        /// </summary>
        /// <param name="button"> SelectionType</param>
        public virtual void Selected(SelectionType button) {
            m_nullifyHitPos = true;
            if (button != SelectionType.BROADCAST)
                transform.parent.GetComponent<IVirgisEntity>().Selected(button);
            if (button == SelectionType.SELECTALL) {
                m_blockMove = true;
            }
        }

        /// <summary>
        /// Use to tell the Component that it is un selected
        /// </summary>
        /// <param name="button"> SelectionType</param>
        public virtual void UnSelected(SelectionType button) {
            if (button != SelectionType.BROADCAST)
                transform.parent.GetComponent<IVirgisEntity>().UnSelected(SelectionType.BROADCAST);
            m_blockMove = false;
        }

        


        /// <summary>
        /// Sent by the UI to request this component to move.
        /// </summary>
        /// <param name="args">MoveArgs : Either a trabslate vectir OR a Vector position to move to, both in World space coordinates</param>
        public virtual void MoveTo(MoveArgs args) {
            transform.parent.GetComponent<IVirgisEntity>().Translate(args);
        }

        /// <summary>
        /// received when a Move Axis request is made by the user
        /// </summary>
        /// <param name="delta"> Vector representing this channge to the transform</param>
        public virtual void MoveAxis(MoveArgs args) {
            args.id = GetId();
            if (m_nullifyHitPos)
                m_firstHitPosition = args.pos;
            args.pos = m_firstHitPosition;
            transform.parent.GetComponent<IVirgisEntity>().MoveAxis(args);
            m_nullifyHitPos = false;
        }

        /// <summary>
        /// Called when a child component is translated by User action
        /// </summary>
        /// <param name="args">MoveArgs</param>
        public virtual void Translate(MoveArgs args) {
            //do nothing
        }

        /// <summary>
        /// Called when a child Vertex moves to the point in the MoveArgs - which is in World Coordinates
        /// </summary>
        /// <param name="data">MoveArgs</param>
        public virtual void VertexMove(MoveArgs args) {
            //do nothing
        }

        /// <summary>
        /// Gets the closest point of the faeture geometry to the coordinates
        /// </summary>
        /// <param name="coords"> Vector3 Target Coordinates </param>
        /// <returns> Vector3 in world space coordinates </returns>
        public virtual VirgisFeature GetClosest(Vector3 coords, Guid[] exclude) {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// call this to add a vertex to a feature.
        /// </summary>
        /// <param name="position">Vector3</param>
        /// <returns>VirgisComponent The new vertex</returns>
        public virtual VirgisFeature AddVertex(Vector3 position) {
            // do nothing
            return this;
        }

        /// <summary>
        /// call this to remove a vertxe from a feature
        /// </summary>
        /// <param name="vertex">Vertex to remove</param>
        public virtual void RemoveVertex(VirgisFeature vertex) {
            // do nothing
            throw new System.NotImplementedException();
        }


        /// <summary>
        /// Get Geometry from the Feature
        /// </summary>
        /// <typeparam name="T">The Type of the geometry</typeparam>
        /// <returns> Gemoetry of type T </returns>
        public virtual T GetGeometry<T>() {
            throw new System.NotImplementedException();
        }

        public Guid GetId() {
            return _id;
        }

        public abstract Dictionary<string, object> GetInfo();

        public abstract void SetInfo(Dictionary<string, object> meta);

        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            VirgisFeature com = obj as VirgisFeature;
            if (com == null)
                return false;
            else
                return Equals(com);
        }
        public override int GetHashCode() {
            return _id.GetHashCode();
        }
        public bool Equals(VirgisFeature other) {
            if (other == null)
                return false;
            return (this._id.Equals(other.GetId()));
        }

        /// <summary>
        /// Called whnen the pointer hovers on this feature
        /// </summary>
        public void Hover(Vector3 hit) {
            lastHit = hit;
            Dictionary<string, object> meta = GetInfo();
            if (meta != null && meta.Count > 0) {
                string output = string.Join("\n", meta.Select(x => $"{x.Key}:\t{x.Value}"));
                AppState.instance.Info.Set(output);
            }
        }

        /// <summary>
        /// called when the pointer stops hoveringon this feature
        /// </summary>
        public void UnHover() {
            AppState.instance.Info.Set("");
        }

        public VirgisLayer GetLayer() {
            return transform.parent.GetComponent<IVirgisEntity>().GetLayer();
        }

        public virtual void OnEdit(bool inSession) {
            // do nothing
        }
    }
}
