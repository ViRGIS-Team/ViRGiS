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

using OSGeo.OSR;
using g3;
using Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;

namespace Virgis {

    /// <summary>
    /// Abstract parent for all Layer entities
    /// </summary>
    public abstract class VirgisLayer : MonoBehaviour, IVirgisLayer {

        public RecordSet _layer;
        public FeatureType featureType { get; protected set; }

        public string sourceName { get; set; }

        public List<IVirgisLayer> subLayers {
            get; protected set;
        }

        /// <summary>
        /// true if this layer has been changed from the original file
        /// </summary>
        public bool changed {
            get {
                return m_changed;
            }
            set {
                m_changed = value;
                IVirgisLayer parent = transform.parent?.GetComponent<IVirgisLayer>();
                if (parent != null) parent.changed = value;
            }
        }
        public bool isContainer { get; protected set; }  // if this is a container layer - do not Draw
        public bool isWriteable { // only allow edit and save for layers that can be written
            get;
            set;
        }
        protected Guid m_id;
        protected bool m_editable;
        protected SpatialReference m_crs;
        private bool m_changed;

        private readonly List<IDisposable> m_subs = new List<IDisposable>();

        protected void Awake() {
            m_id = Guid.NewGuid();
            m_editable = false;
            changed = true;
            isContainer = false;
            isWriteable = false;
        }

        protected void Start() {
            AppState appState = AppState.instance;
            m_subs.Add(appState.editSession.StartEvent.Subscribe(_onEditStart));
            m_subs.Add(appState.editSession.EndEvent.Subscribe(_onEditStop));
        }


        protected void OnDestroy() {
            m_subs.ForEach(item => item.Dispose());
        }

        public void Destroy() {
            Destroy(gameObject);
        }

        public virtual bool Load(string file) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Called to initialise this layer
        /// 
        /// If the data cannot be read, fails quitely and creates an empty layer
        /// </summary>
        /// <param name="layer"> The RecordSet object that defines this layer</param>
        /// 
        public async Task Init(RecordSet layer) {
            await SubInit(layer);
            await Draw();
            Debug.Log($"Loaded Layer : {layer.DisplayName}");
            AppState.instance.addLayer(this);
        }

        /// <summary>
        /// Called to Initialise a sublayer
        /// </summary>
        /// <param name="layer"> The RecordSet object that defines this layer</param>
        /// 
        public async Task SubInit(RecordSet layer) {
            try {
                await _init();
                gameObject.SetActive(layer.Visible);
            } catch (Exception e) {
                Debug.LogError($"Layer : { layer.DisplayName} :  {e.ToString()}");
            }
        }

        /// <summary>
        /// Implement the layer specific init code in this method
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        protected abstract Task _init();

        /// <summary>
        /// Call this to create a new feature
        /// </summary>
        /// <param name="position">Vector3 or DMesh3</param>
        public IVirgisFeature AddFeature<T>(T geometry) {
            if (AppState.instance.InEditSession() && IsEditable()) {
                changed = true;
                switch (geometry) {
                    case Vector3[] v:
                        return _addFeature(v);
                    case DMesh3 d:
                        return _addFeature(d);
                    default: return null;
                }
            }
            return null;
        }

        /// <summary>
        /// implement the layer specfiic code for creating a new feature here
        /// </summary>
        /// <param name=position"></param>
        protected abstract IVirgisFeature _addFeature<T>(T geometry);

        /// <summary>
        /// Draw the layer based upon the features in the features RecordSet
        /// </summary>
        public async Task Draw() {
            //change nothing if there are no changes
            if (changed) {
                if (!isContainer) {
                    //make sure the layer is empty
                    for (int i = transform.childCount - 1; i >= 0; i--) {
                        Transform child = transform.GetChild(i);
                        Destroy(child.gameObject);
                    }

                    transform.rotation = Quaternion.identity;
                    transform.localPosition = Vector3.zero;
                    transform.localScale = Vector3.one;
                }

                await _draw();
                changed = false;
            }
            return;
        }


        /// <summary>
        /// Implment the layer specific draw code in this method
        /// </summary>
        /// <returns></returns>
        protected abstract Task _draw();

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
        public virtual async Task<RecordSet> Save(bool flag = false) {
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
        /// Called whenever a member entity is asked to Change Axis
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
        public IVirgisFeature GetClosest(Vector3 coords, Guid[] exclude) {
            List<VirgisFeature> list = transform.GetComponentsInChildren<VirgisFeature>().ToList();
            list = list.FindAll(item => !exclude.Contains(item.GetId()));
            KdTree<VirgisFeature> tree = new KdTree<VirgisFeature>();
            tree.AddAll(list);
            return tree.FindClosest(transform.position);
        }

        /// <summary>
        /// Get the feature that matches the ID provided 
        /// </summary>
        /// <param name="id"> ID</param>
        /// <returns>returns the featue contained in an enitity of type S</returns>
        public IVirgisFeature GetFeature(Guid id) {
            return GetComponents<VirgisFeature>().ToList().Find(item => item.GetId() == id);
        }

        /// <summary>
        /// Fecth the layer GUID
        /// </summary>
        /// <returns>GUID</returns>
        public Guid GetId() {
            if (m_id == Guid.Empty)
                m_id = Guid.NewGuid();
            return m_id;
        }

        /// <summary>
        /// Get the metadata for this Layer
        /// </summary>
        /// <returns></returns>
        public RecordSet GetMetadata() {
            return _layer;
        }

        /// <summary>
        /// Sets the layer Metadata
        /// </summary>
        /// <param name="layer">Data tyoe that inherits form RecordSet</param>
        public void SetMetadata(RecordSet layer) {
            _layer = layer;
        }

        /// <summary>
        /// Fetches the feature shape to be used to create new features
        /// </summary>
        /// <returns></returns>
        public virtual GameObject GetFeatureShape() {
            return default;
        }

        /// <summary>
        /// Change the layer visibility
        /// </summary>
        /// <param name="visible"></param>
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
            if (isWriteable) {
                m_editable = inSession;
                _set_editable();
            }
        }

        protected virtual void _set_editable() {
        }

        /// <summary>
        /// Test to see if this layer is currently being edited
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsEditable() {
            return m_editable;
        }

        /// <summary>
        /// Set the Layer CRS
        /// </summary>
        /// <param name="crs">SpatialReference</param>
        public void SetCrs(SpatialReference crs) {
            m_crs = crs;
        }

        protected virtual void _onEditStart(bool test) {
            if (IsEditable()) {
                VirgisFeature[] coms = GetComponentsInChildren<VirgisFeature>();
                foreach (VirgisFeature com in coms) {
                    com.OnEdit(true);
                }
            }
        }

        protected virtual void _onEditStop(bool test) {
            if (IsEditable()) {
                VirgisFeature[] coms = GetComponentsInChildren<VirgisFeature>();
                foreach (VirgisFeature com in coms) {
                    com.OnEdit(false);
                }
            }
        }

        /// <summary>
        /// Get the Layer CRS
        /// </summary>
        /// <returns></returns>
        public SpatialReference GetCrs() {
            return m_crs;
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
            return m_id.GetHashCode();
        }
        public bool Equals(VirgisLayer other) {
            if (other == null)
                return false;
            return (this.m_id.Equals(other.GetId()));
        }

        public T GetLayer<T>() {
            switch (typeof(T)) {
                case Type virgislayer when virgislayer == typeof(VirgisLayer):
                    return (T) System.Convert.ChangeType(this, typeof(T));
                case Type virgislayer when virgislayer == typeof(IVirgisLayer):
                    return (T) System.Convert.ChangeType(this as IVirgisLayer, typeof(T));
            }
            return default;
        }

        public void OnEdit(bool inSession) {
            // do nothing
        }

        public virtual Dictionary<string, object> GetInfo(VirgisFeature feat) {
            return default;
        }

        public void MessageUpwards(string method, object args) {
            transform.SendMessageUpwards(method, args, SendMessageOptions.DontRequireReceiver);
        }

        VirgisFeature IVirgisLayer.AddFeature<T>(T geometry) {
            throw new NotImplementedException();
        }

        VirgisFeature IVirgisLayer.GetFeature(Guid id) {
            throw new NotImplementedException();
        }

        VirgisFeature IVirgisEntity.GetClosest(Vector3 coords, Guid[] exclude) {
            throw new NotImplementedException();
        }

        public Dictionary<string, object> GetInfo() {
            throw new NotImplementedException();
        }

        public void SetInfo(Dictionary<string, object> meta) {
            throw new NotImplementedException();
        }
    }

    public abstract class VirgisLayer<T, S> : VirgisLayer where T : RecordSet {
        readonly Type LayerType = typeof(T);
        readonly Type DataType = typeof(S);

        public S features; // holds the feature data for this layer

        /// <summary>
        /// Get the layer Metadata
        /// </summary>
        /// <returns>RecordSet Layer Metatdata</returns>
        public new T GetMetadata() {
            return base.GetMetadata() as T;
        }

        /// <summary>
        /// Set the Layer Metadata
        /// </summary>
        /// <param name="layer">RecordSet Layer Data</param>
        public void SetMetadata(T layer) {
            base.SetMetadata(layer);
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
            return m_id.GetHashCode();
        }
        public bool Equals(VirgisLayer<T, S> other) {
            if (other == null)
                return false;
            return (this.m_id.Equals(other.GetId()));
        }

        protected override IVirgisFeature _addFeature<D>(D geometry) {
            throw new NotImplementedException();
        }
    }
}