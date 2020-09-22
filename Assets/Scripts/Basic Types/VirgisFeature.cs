﻿using System;
using System.Collections.Generic;
using UnityEngine;
using OSGeo.OGR;
using System.Linq;

namespace Virgis
{

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
        Dictionary<string, object> GetMetadata();
        void SetMetadata(Dictionary<string, object> meta);

        void Hover();
        void UnHover();

    }

    public abstract class VirgisFeature : MonoBehaviour, IVirgisFeature
    {
        protected Material mainMat; // color of the component
        protected Material selectedMat; // color of the component when selected

        private Guid _id; // internal ID for this component - used when it is part of a larger structure
        public Transform label; //  Go of the label or billboard
        public Feature feature; // Feature tht was the source for this GO





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
            //do nothing
        }

        /// <summary>
        /// Use to tell the Component that it is un selected
        /// </summary>
        /// <param name="button"> SelectionType</param>
        public virtual void UnSelected(SelectionType button) {
            //do nothing
        }

        


        /// <summary>
        /// Sent by the UI to request this component to move.
        /// </summary>
        /// <param name="args">MoveArgs : Either a trabslate vectir OR a Vector position to move to, both in World space coordinates</param>
        public virtual void MoveTo(MoveArgs args) {
            //do nothing
        }

        /// <summary>
        /// received when a Move Axis request is made by the user
        /// </summary>
        /// <param name="delta"> Vector representing this channge to the transform</param>
        public virtual void MoveAxis(MoveArgs args) {
            //do nothing
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

        public abstract Dictionary<string, object> GetMetadata();

        public abstract void SetMetadata(Dictionary<string, object> meta);

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
        public void Hover() {
            Dictionary<string, object> meta = GetMetadata();
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
    }
}
