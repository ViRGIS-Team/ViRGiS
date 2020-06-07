﻿// copyright Runette Software Ltd, 2020. All rights reserved
using Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Virgis {

    public interface IVirgisLayer: IVirgisEntity {

        VirgisFeature AddFeature(Vector3[] geometry);
        void Draw();
        void CheckPoint();
        RecordSet Save();
        VirgisFeature GetFeature(Guid id);
        GameObject GetFeatureShape();
        void SetVisible(bool visible);
        bool IsVisible();
        void SetEditable(bool inSession);
        bool IsEditable();
    }

    /// <summary>
    /// Abstract parent for all Layer entities
    /// </summary>
    public abstract class VirgisLayer<T, S> : MonoBehaviour, IVirgisLayer where T : RecordSet {

        readonly Type LayerType = typeof(T);
        readonly Type DataType = typeof(S);


        public T layer; // holds the RecordSet data for this layer
        public S features; // holds the feature data for this layer
        public bool changed = true; // true is this layer has been changed from the original file

        protected Guid _id;
        protected bool _editable;

        void Awake() {
            _id = Guid.NewGuid();
            _editable = false;
        }

        /// <summary>
        /// Get the event Manager and register listeners
        /// </summary>
        void Start() {
            AppState.instance.editSession.AddStartEditSessionListener(StartEditSession);
            AppState.instance.editSession.AddEndEditSessionListener(ExitEditSession);
        }


        /// <summary>
        /// Called to initialise this layer
        /// 
        /// If the data cannot be read, fails quitely and creates an empty layer
        /// </summary>
        /// <param name="layer"> The GeographyCollection object that defines this layer</param>
        /// <returns>refernce to this GameObject for chaining</returns>
        public async Task<VirgisLayer<T, S>> Init(T layer) {
            this.layer = layer;
            await _init(layer);
            return this;
        }


        /// <summary>
        /// Implement the layer specific init code in this method
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        protected abstract Task _init(T layer);

        /// <summary>
        /// Call this to create a new feature
        /// </summary>
        /// <param name="position">Vector3 where to create the new layer</param>
        public VirgisFeature AddFeature(Vector3[] geometry) {
            if (AppState.instance.InEditSession() && IsEditable()) {
                return _addFeature(geometry);
            }
            return null;
        }

        /// <summary>
        /// implement the layer specfiic code for creating a new feature here
        /// </summary>
        /// <param name=position"></param>
        protected abstract VirgisFeature _addFeature(Vector3[] geometry);

        /// <summary>
        /// Draw the layer based upon the features in the features GeographyCollection
        /// </summary>
        public void Draw() {
            //change nothing if there are no changes
            if (changed) {
                //make sure the layer is empty
                for (int i = transform.childCount - 1; i >= 0; i--) {
                    Transform child = transform.GetChild(i);
                    child.gameObject.Destroy();
                }

                transform.rotation = Quaternion.identity;
                transform.localPosition = Vector3.zero;
                transform.localScale = Vector3.one;

                _draw();
                changed = false;
            }
        }


        /// <summary>
        /// Implment the layer specific draw code in this method
        /// </summary>
        /// <returns></returns>
        protected abstract void _draw();

        /// <summary>
        /// Call this to tell the layers to create a checkpoint. 
        /// 
        /// Only valid outside of an Edit Session. Inside an Edit Session use Save() as CheckPoint() will do nothing
        /// </summary>
        public void CheckPoint() {
            if (!AppState.instance.InEditSession()) {
                _checkpoint();
            }

        }

        /// <summary>
        /// Implement the layer specific checkpoint code here
        /// </summary>
        protected abstract void _checkpoint();

        /// <summary>
        /// Called to save the current layer data to source
        /// </summary>
        /// <returns>A copy of the data save dot the source</returns>
        public RecordSet Save() {
            if (changed) {
                _save();
            }
            return layer;
        }

        /// <summary>
        /// Implment the layer specific draw code in this method
        /// </summary>
        /// <returns></returns>
        protected abstract void _save();

        /// <summary>
        /// Called Whenever a member entity is asked to Translate
        /// </summary>
        /// <param name="args">MoveArge Object</param>
        public virtual void Translate(MoveArgs args) {
            //do nothing
        }

        /// <summary>
        /// Called whenevr a member entity is asked to Change Axis
        /// </summary>
        /// <param name="args">MoveArgs Object</param>
        public virtual void MoveAxis(MoveArgs args) {
            // do nothing 
        }

        public virtual void MoveTo(MoveArgs args) {
            //do nothing
        }

        public virtual void VertexMove(MoveArgs args) {
            //do nothing 
        }

        /// <summary>
        /// Called when an edit session starts
        /// </summary>
        public virtual void StartEditSession() {
           // do nothing
        }

        /// <summary>
        /// Called when an edit session ends
        /// </summary>
        /// <param name="saved">true if stop and save, false if stop and discard</param>
        public virtual void ExitEditSession(bool saved) {
            // do nothing
        }

        /// <summary>
        /// called when a daughter IVirgisEntity is selected
        /// </summary>
        /// <param name="button"> SelectionType</param>
        public virtual void Selected(SelectionTypes button) {
            //do nothing
        }

        /// <summary>
        /// Called when a daughter IVirgisEntity is UnSelected
        /// </summary>
        /// <param name="button">SelectionType</param>
        public virtual void UnSelected(SelectionTypes button) {
            // do nothing
        }

        /// <summary>
        ///  Get the Closest Feature to the coordinates. Exclude any Component Ids in the Exclude Array. The exclude lis  is primarily used to avoid a GetClosest to a Faeture picking up the feature itself
        /// </summary>
        /// <param name="coords"> coordinates </param>
        /// <returns>returns the featue contained in an enitity of type S</returns>
        public VirgisFeature GetClosest(Vector3 coords, Guid[] exclude) {
            List<VirgisFeature> list = transform.GetComponentsInChildren<VirgisFeature>().ToList();
            list = list.FindAll(item => !exclude.Contains(item.GetId()));
            KdTree<VirgisFeature> tree = new KdTree<VirgisFeature>();
            tree.AddAll(list);
            return tree.FindClosest(transform.position) as VirgisFeature;
        }

        /// <summary>
        /// Get the feature that matches the ID provided 
        /// </summary>
        /// <param name="id"> ID</param>
        /// <returns>returns the featue contained in an enitity of type S</returns>
        public VirgisFeature GetFeature(Guid id) {
            return GetComponents<VirgisFeature>().ToList().Find(item => item.GetId() == id);
        }

        /// <summary>
        /// Fecth the layer GUID
        /// </summary>
        /// <returns>GUID</returns>
        public Guid GetId() {
            return _id;
        }

        /// <summary>
        /// Fetch the metadata for this Layer
        /// </summary>
        /// <returns></returns>
        public RecordSet GetMetadata() {
            return layer;
        }


        public abstract GameObject GetFeatureShape();

        public void SetVisible(bool visible) {
            if (layer.Visible != visible) {
                layer.Visible = visible;
                gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Test if this layer is currently visible
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsVisible() {
            return layer.Visible;
        }

        /// <summary>
        /// Sets a marker that this particular layer is being edited.
        /// </summary>
        /// 
        /// There can be only one layer being edited during an edit session.
        /// 
        /// <param name="inSession"></param> true to indicate that this layer is in edit session,
        /// or false if otherwise.
        public void SetEditable(bool inSession) {
            _editable = inSession;
        }

        /// <summary>
        /// Test to see if this layer is editable
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsEditable() {
            return _editable;
        }

        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            VirgisLayer<T,S> com = obj as VirgisLayer<T,S>;
            if (com == null)
                return false;
            else
                return Equals(com);
        }

        public override int GetHashCode() {
            return _id.GetHashCode();
        }
        public bool Equals(VirgisLayer<T,S> other) {
            if (other == null)
                return false;
            return (this._id.Equals(other.GetId()));
        }
    }
}

