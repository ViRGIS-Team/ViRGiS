using System;
using System.Collections.Generic;
using UnityEngine;

namespace Virgis
{

    /// <summary>
    /// Abstract parent for all in game entities
    /// </summary>
    public interface IVirgisEntity
    {
        void Selected(SelectionTypes button);
        void UnSelected(SelectionTypes button);
        void StartEditSession();
        void ExitEditSession(bool saved);
    }

    /// <summary>
    /// Abstract Parent for all symbology relevant in game entities
    /// </summary>
    public interface IVirgisComponent : IVirgisEntity
    {
        void SetMaterial(Material mainMat, Material selectedMat);
        //void MoveTo(Vector3 newPos);

        void MoveTo(MoveArgs args);
        void MoveAxis(MoveArgs args);
        void Translate(MoveArgs args);
        void VertexMove(MoveArgs args);

        Vector3 GetClosest(Vector3 coords);
        T GetGeometry<T>();

    }

    public abstract class VirgisComponent : MonoBehaviour, IVirgisComponent
    {
        protected Material mainMat; // color of the component
        protected Material selectedMat; // color of the component when selected
        public string gisId; // ID of this component in the geoJSON
        public IDictionary<string, object> gisProperties; //  geoJSON properties of this component

        public Guid id; // internal ID for this component - used when it is part of a larger structure
        public Transform label; //  Go of the label or billboard


        void Awake()
        {
            id = Guid.NewGuid();
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
        public abstract void Selected(SelectionTypes button);

        /// <summary>
        /// Use to tell the Component that it is un selected
        /// </summary>
        /// <param name="button"> SelectionType</param>
        public abstract void UnSelected(SelectionTypes button);

        /// <summary>
        /// Called when an edit session starts
        /// </summary>
        public virtual void StartEditSession() { 
            //do nothing
        }

        /// <summary>
        /// Called when an edit session ends
        /// </summary>
        public virtual void ExitEditSession(bool saved) {
            // do nothing
        }

        /// <summary>
        /// Sent by the UI to request this component to move.
        /// </summary>
        /// <param name="newPos">Vector3 Worldspace Location to move to </param>
        public abstract void MoveTo(MoveArgs args);

        /// <summary>
        /// received when a Move Axis request is made by the user
        /// </summary>
        /// <param name="delta"> Vector representing this channge to the transform</param>
        public abstract void MoveAxis(MoveArgs args);

        /// <summary>
        /// Called when a child component is translated by User action
        /// </summary>
        /// <param name="args">MoveArgs</param>
        public abstract void Translate(MoveArgs args);

        /// <summary>
        /// Called when a child Vertex moves to the point in the MoveArgs - which is in World Coordinates
        /// </summary>
        /// <param name="data">MoveArgs</param>
        public abstract void VertexMove(MoveArgs args);

        /// <summary>
        /// Gets the closest point of the faeture geometry to the coordinates
        /// </summary>
        /// <param name="coords"> Vector3 Target Coordinates </param>
        /// <returns> Vector3 in world space coordinates </returns>
        public abstract Vector3 GetClosest(Vector3 coords);

        public virtual void AddVertex(MoveArgs args) {
            throw new System.NotImplementedException();
        }


        /// <summary>
        /// Get Geometry from the Feature
        /// </summary>
        /// <typeparam name="T">The Type of the geometry</typeparam>
        /// <returns> Gemoetry of type T </returns>
        public abstract T GetGeometry<T>();
    }
}
