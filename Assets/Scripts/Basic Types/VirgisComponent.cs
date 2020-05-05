using System.Collections;
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
        void EditEnd();
    }

    /// <summary>
    /// Abstract Parent for all symbology relevant in game entities
    /// </summary>
    public interface IVirgisComponent : IVirgisEntity
    {
        void SetColor(Color color);
        //void MoveTo(Vector3 newPos);

        void MoveTo(Vector3 newPos);
        void MoveAxis(MoveArgs args);
        void Translate(MoveArgs args);
        void VertexMove(MoveArgs args);

    }

    public abstract class VirgisComponent : MonoBehaviour, IVirgisComponent
    {
        public Color color; // color of the component
        public Color anticolor; // color of the component when selected
        public string gisId; // ID of this component in the geoJSON
        public IDictionary<string, object> gisProperties; //  geoJSON properties of this component

        public int id; // internal ID for this component - used when it is part of a larger structure
        public Transform label; //  Go of the label or billboard


        /// <summary>
        /// Use to set the color of the feature
        /// </summary>
        /// <param name="color"> Color Object</param>
        public abstract void SetColor(Color color);

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
        /// Called when an edit session ends
        /// </summary>
        public abstract void EditEnd();


        /// <summary>
        /// Sent by the UI to request this component to move.
        /// </summary>
        /// <param name="newPos">Vector3 Worldspace Location to move to </param>
        public abstract void MoveTo(Vector3 newPos);

        /// <summary>
        /// received when a Move Axis request is made by the user
        /// </summary>
        /// <param name="delta"> Vector representing this channge to the transform</param>
        public abstract void MoveAxis(MoveArgs args);


        /// <summary>
        /// Set the Id of the marker
        /// </summary>
        /// <param name="value">ID</param>
        public void SetId(int value)
        {
            id = value;
        }

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
    }
}
