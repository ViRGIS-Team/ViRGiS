// copyright Runette Software Ltd, 2020. All rights reserved
using OSGeo.OSR;
using Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Virgis {

    public interface IVirgisLayer : IVirgisEntity {

        FeatureType featureType {
            get; set;
        }

        VirgisFeature AddFeature(Vector3[] geometry);
        void Draw();
        void CheckPoint();
        Task<RecordSet> Save();
        VirgisFeature GetFeature(Guid id);
        GameObject GetFeatureShape();
        RecordSet GetMetadata();
        void SetMetadata(RecordSet meta);
        void SetVisible(bool visible);
        bool IsVisible();
        void SetEditable(bool inSession);
        bool IsEditable();
    }

    /// <summary>
    /// Abstract parent for all Layer entities
    /// </summary>
    public abstract class VirgisLayer : MonoBehaviour, IVirgisLayer {

        public RecordSet _layer;
        public FeatureType featureType { get; set; }

        public bool changed = true; // true is this layer has been changed from the original file
        protected Guid _id;
        protected bool _editable;
        protected SpatialReference _crs;

        void Awake() {
            _id = Guid.NewGuid();
            _editable = false;
        }

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
                    Destroy(child.gameObject);
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
        public virtual async Task<RecordSet> Save() {
            if (changed) {
                await _save();
            }
            return GetMetadata();
        }

        /// <summary>
        /// Implment the layer specific draw code in this method
        /// </summary>
        /// <returns></returns>
        protected abstract Task _save();

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
        /// called when a daughter IVirgisEntity is selected
        /// </summary>
        /// <param name="button"> SelectionType</param>
        public virtual void Selected(SelectionType button) {
            changed = true;
        }

        /// <summary>
        /// Called when a daughter IVirgisEntity is UnSelected
        /// </summary>
        /// <param name="button">SelectionType</param>
        public virtual void UnSelected(SelectionType button) {
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
        /// Get the metadata for this Layer
        /// </summary>
        /// <returns></returns>
        public RecordSet GetMetadata() {
            return _layer;
        }

        /// <summary>
        /// Sets the layer Metadat
        /// </summary>
        /// <param name="layer">Data tyoe that inherits form RecordSet</param>
        public void SetMetadata(RecordSet layer) {
            _layer = layer;
        }


        public virtual GameObject GetFeatureShape() {
            return default;
        }

        public virtual void SetVisible(bool visible) {
            if (_layer.Visible != visible) {
                _layer.Visible = visible;
                gameObject.SetActive(visible);
                _set_visible();
            }
        }

        public virtual void _set_visible() {
        }

        /// <summary>
        /// Test if this layer is currently visible
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsVisible() {
            return _layer.Visible;
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

        /// <summary>
        /// Set the Layer CRS
        /// </summary>
        /// <param name="crs">SpatialReference</param>
        public void SetCrs(SpatialReference crs) {
            _crs = crs;
        }

        /// <summary>
        /// Get the Layer CRS
        /// </summary>
        /// <returns></returns>
        public SpatialReference GetCrs() {
            return _crs;
        }

        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            VirgisLayer com = obj as VirgisLayer;
            if (com == null)
                return false;
            else
                return Equals(com);
        }

        public override int GetHashCode() {
            return _id.GetHashCode();
        }
        public bool Equals(VirgisLayer other) {
            if (other == null)
                return false;
            return (this._id.Equals(other.GetId()));
        }

        public VirgisLayer GetLayer() {
            return this;
        }
    }

    public abstract class VirgisLayer<T, S> : VirgisLayer where T : RecordSet {
        readonly Type LayerType = typeof(T);
        readonly Type DataType = typeof(S);

        public S features; // holds the feature data for this layer



        /// <summary>
        /// Called to initialise this layer
        /// 
        /// If the data cannot be read, fails quitely and creates an empty layer
        /// </summary>
        /// <param name="layer"> The GeographyCollection object that defines this layer</param>
        /// <returns>refernce to this GameObject for chaining</returns>
        public async Task<VirgisLayer<T, S>> Init(T layer ) {
            SetMetadata(layer as RecordSet);
            await _init();
            return this;
        }

        /// <summary>
        /// Implement the layer specific init code in this method
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        protected abstract Task _init();

        /// <summary>
        /// Get the layer Metadata
        /// </summary>
        /// <returns>RecordSet Layer Metatdata</returns>
        public new T GetMetadata() {
            return (this as VirgisLayer).GetMetadata() as T;
        }

        /// <summary>
        /// Set the Layer Metadata
        /// </summary>
        /// <param name="layer">RecordSet Layer Data</param>
        public void SetMetadata(T layer) {
            (this as VirgisLayer).SetMetadata(layer);
        }

        /// <summary>
        /// Set the feature Data for layer explicitly
        /// </summary>
        /// <param name="features">Feature Data in the correct format for the layer</param>
        public void SetFeatures(S features) {
            if (features != null) {
                this.features = features;
            } 
        }


        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            VirgisLayer<T, S> com = obj as VirgisLayer<T, S>;
            if (com == null)
                return false;
            else
                return Equals(com);
        }

        public override int GetHashCode() {
            return _id.GetHashCode();
        }
        public bool Equals(VirgisLayer<T, S> other) {
            if (other == null)
                return false;
            return (this._id.Equals(other.GetId()));
        }

    }
}