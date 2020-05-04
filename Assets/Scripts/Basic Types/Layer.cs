// copyright Runette Software Ltd, 2020. All rights reserved
using UnityEngine;
using Project;
using System.Threading.Tasks;


namespace Virgis
{

    public interface ILayer 
    {
        void Add(MoveArgs args);
        void Draw();
        void CheckPoint();
        RecordSet Save();
    }

    /// <summary>
    /// Abstract parent for all Layer entities
    /// </summary>
    public abstract class Layer<T,S> : MonoBehaviour, ILayer where T : RecordSet
    {

        public T layer; // holds the RecordSet data for this layer
        public S features; // holds the feature data for this layer
        public bool changed = true; // true is this layer has been changed from the original file

        /// <summary>
        /// Get the event Manager and register listeners
        /// </summary>
        private void Start()
        {
            AppState.instance.AddEndEditSessionListener(ExitEditsession);
        }


        /// <summary>
        /// Called to initialise this layer
        /// 
        /// If the data cannot be read, fails quitely and creates an empty layer
        /// </summary>
        /// <param name="layer"> The GeographyCollection object that defines this layer</param>
        /// <returns>refernce to this GameObject for chaining</returns>
        public async Task<Layer<T,S>> Init(T layer)
        {
            this.layer = layer;
            await _init(layer);
            return this;
        }


        /// <summary>
        /// Implement the layer specific init code in this method
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public abstract Task _init(T layer);

        /// <summary>
        /// Call this to create a new feature
        /// </summary>
        /// <param name="args">MOveArgs with details about whwre to create the new layer</param>
        public void Add(MoveArgs args)
        {
            if (AppState.instance.InEditSession())
            {
                _add(args);
            }
        }

        /// <summary>
        /// implement the layer specfiic code for creating a new feature here
        /// </summary>
        /// <param name=args"></param>
        public abstract void _add(MoveArgs args);

        /// <summary>
        /// Draw the layer based upon the features in the features GeographyCollection
        /// </summary>
        public void Draw()
        {
            //change nothing if there are no changes
            if (changed)
            {
                //make sure the layer is empty
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = transform.GetChild(i);
                    child.Destroy();
                }

                _draw();
                changed = false;
            }
        }


        /// <summary>
        /// Implment the layer specific draw code in this method
        /// </summary>
        /// <returns></returns>
        public abstract void _draw();

        /// <summary>
        /// Call this to tell the layers to create a checkpoint. 
        /// 
        /// Only valid outside of an Edit Session. Inside an Edit Session use Save() as CheckPoint() will do nothing
        /// </summary>
        public void CheckPoint()
        {
            if (!AppState.instance.InEditSession())
            {
                _cp();
            }

        }

        /// <summary>
        /// Implement the layer specific checkpoint code here
        /// </summary>
        public abstract void _cp();

        /// <summary>
        /// Called to save the current layer data to source
        /// </summary>
        /// <returns>A copy of the data save dot the source</returns>
        public RecordSet Save() 
        {
            if (changed)
            {
                _save();
            }
            return layer;
        }

        /// <summary>
        /// Implment the layer specific draw code in this method
        /// </summary>
        /// <returns></returns>
        public abstract void _save();

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
        /// Called when an edit session ends
        /// </summary>
        public abstract void ExitEditsession();


    }
}

