// copyright Runette Software Ltd, 2020. All rights reserved
using System.Collections;
using UnityEngine;
using Project;
using GeoJSON.Net.Feature;
using System.Threading.Tasks;

namespace ViRGIS
{

    /// <summary>
    /// Abstract parent for all Layer entities
    /// </summary>
    public abstract class Layer : MonoBehaviour
    {
        // Name of the input file, no extension
        public string inputfile;

        public GeographyCollection layer; // holds the RecordSet data for this layer
        public FeatureCollection features; // holds the feature data for this layer
        public bool changed = true; // true is this layer has been changed from the original file

        /// <summary>
        /// Get the event Manager and register listeners
        /// </summary>
        private void Start()
        {
            StartCoroutine(GetEvents());
        }


        /// <summary>
        /// Called to initialise this layer
        /// 
        /// If the data cannot be read, fails quitely and creates an empty layer
        /// </summary>
        /// <param name="layer"> The GeographyCollection object that defines this layer</param>
        /// <returns>refernce to this GameObject for chaining</returns>
        public async Task<GameObject> Init(GeographyCollection layer)
        {
            this.layer = layer;
            await _init(layer);
            return gameObject;
        }


        /// <summary>
        /// Implement the layer specific init code in this method
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        public abstract Task _init(GeographyCollection layer);

        /// <summary>
        /// Draw the layer based upon the features in the features GeographyCollection
        /// </summary>
        /// <returns>refernce to this GameObject for chaining</returns>
        public GameObject Draw()
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

            return gameObject;
        }


        /// <summary>
        /// Implment the layer specific draw code in this method
        /// </summary>
        /// <returns></returns>
        public abstract void _draw();

        /// <summary>
        /// Called to save the current layer data to source
        /// </summary>
        /// <returns>A copy of the data save dot the source</returns>
        public abstract GeographyCollection Save();

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

        /// <summary>
        /// Gets the EventManager, waiting for it to instantiate if it does not exist. Adss the listerners required :
        /// ExitEditSession,
        /// </summary>
        /// <returns>EventManager</returns>
        IEnumerator GetEvents()
        {
            GameObject Map = Global.Map;
            EventManager eventManager;
            do
            {
                eventManager = Map.GetComponent<EventManager>();
                if (eventManager == null) { new WaitForSeconds(.5f); };
            } while (eventManager == null);
            eventManager.EditSessionEndEvent.AddListener(ExitEditsession);
            yield return eventManager;
        }
    }
}

