// copyright Runette Software Ltd, 2020. All rights reserved
using Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Virgis {

    public interface ILayer {

        void AddFeature(MoveArgs args);

        void Draw();

        void CheckPoint();

        RecordSet Save();

        VirgisComponent GetClosest(Vector3 coords, Guid[] exclude);

        VirgisComponent GetFeature(Guid id);

        Guid GetId();

        RecordSet GetMetadata();

        void SetVisible(bool visible);

        bool IsVisible();

        /// <summary>
        /// Sets a marker that this particular layer is being edited.
        /// </summary>
        /// 
        /// There can be only one layer being edited during an edit session.
        /// 
        /// <param name="inSession"></param> true to indicate that this layer is in edit session,
        /// or false if otherwise.
        void SetEditable(bool inSession);

        bool IsEditable();
    }

    /// <summary>
    /// Abstract parent for all Layer entities
    /// </summary>
    public abstract class Layer<T, S> : MonoBehaviour, ILayer where T : RecordSet {

        readonly Type LayerType = typeof(T);
        readonly Type DataType = typeof(S);


        public T layer; // holds the RecordSet data for this layer
        public S features; // holds the feature data for this layer
        public bool changed = true; // true is this layer has been changed from the original file

        protected Guid _id;
        protected bool _visible;
        protected bool _editable;

        void Awake() {
            _id = Guid.NewGuid();
            _visible = true;
            _editable = false;
        }

        /// <summary>
        /// Get the event Manager and register listeners
        /// </summary>
        void Start() {
            AppState.instance.AddStartEditSessionListener(StartEditSession);
            AppState.instance.AddEndEditSessionListener(ExitEditSession);
        }


        /// <summary>
        /// Called to initialise this layer
        /// 
        /// If the data cannot be read, fails quitely and creates an empty layer
        /// </summary>
        /// <param name="layer"> The GeographyCollection object that defines this layer</param>
        /// <returns>refernce to this GameObject for chaining</returns>
        public async Task<Layer<T, S>> Init(T layer) {
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
        /// <param name="args">MOveArgs with details about whwre to create the new layer</param>
        public void AddFeature(MoveArgs args) {
            if (AppState.instance.InEditSession() && IsEditable()) {
                _addFeature(args);
            }
        }

        /// <summary>
        /// implement the layer specfiic code for creating a new feature here
        /// </summary>
        /// <param name=args"></param>
        protected abstract void _addFeature(MoveArgs args);

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
        public abstract void Translate(MoveArgs args);

        /// <summary>
        /// Called whenevr a member entity is asked to Change Axis
        /// </summary>
        /// <param name="args">MoveArgs Object</param>
        public abstract void MoveAxis(MoveArgs args);

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
        public VirgisComponent GetClosest(Vector3 coords, Guid[] exclude) {
            List<VirgisComponent> list = transform.GetComponentsInChildren<VirgisComponent>().ToList();
            list = list.FindAll(item => !exclude.Contains(item.id));
            KdTree<VirgisComponent> tree = new KdTree<VirgisComponent>();
            tree.AddAll(list);
            return tree.FindClosest(transform.position) as VirgisComponent;
        }

        /// <summary>
        /// Get the feature that matches the ID provided 
        /// </summary>
        /// <param name="id"> ID</param>
        /// <returns>returns the featue contained in an enitity of type S</returns>
        public VirgisComponent GetFeature(Guid id) {
            return GetComponents<VirgisComponent>().ToList().Find(item => item.id == id);
        }

        public Guid GetId() {
            return _id;
        }

        public RecordSet GetMetadata() {
            return layer;
        }

        public void SetVisible(bool visible) {
            if (_visible != visible) {
                _visible = visible;
                //for (int i = 0; i < transform.childCount; i++) {
                //    transform.GetChild(i).gameObject.SetActive(visible);
                //}
                gameObject.SetActive(visible);
            }
        }

        public bool IsVisible() {
            return _visible;
        }

        public void SetEditable(bool inSession) {
            _editable = inSession;
        }

        public bool IsEditable() {
            return _editable;
        }
    }
}

