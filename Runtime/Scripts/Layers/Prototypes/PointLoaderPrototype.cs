using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using Project;
using UnityEngine.UI;
using System.Collections;

namespace Virgis {
    public abstract class PointLoaderPrototype<T> : VirgisLoader<T> {
        protected GameObject m_pointPrefab;
        protected PointLayer parent;
        protected Dictionary<string, Unit> m_symbology;

        protected Task<int> Load() {
            parent = m_parent as PointLayer;
            m_displacement = 1.0f;
            if (m_symbology.ContainsKey("point") &&
                m_symbology["point"].ContainsKey("Shape")) {
                Shapes shape = m_symbology["point"].Shape;
                switch (shape) {
                    case Shapes.Spheroid:
                        m_pointPrefab = parent.SpherePrefab;
                        break;
                    case Shapes.Cuboid:
                        m_pointPrefab = parent.CubePrefab;
                        break;
                    case Shapes.Cylinder:
                        m_pointPrefab = parent.CylinderPrefab;
                        m_displacement = 1.5f;
                        break;
                    default:
                        m_pointPrefab = parent.SpherePrefab;
                        break;
                }
            } else {
                m_pointPrefab = parent.SpherePrefab;
            }

            foreach (string key in m_symbology.Keys) {
                Unit unit = m_symbology[key];
                SerializableMaterialHash hash = new() {
                    Name = key,
                    Color = unit.Color,
                };
                m_materials.Add(key, hash);
                if (key == "point") m_parent.m_DefaultCol.Value = hash;
            }
            return Task.FromResult(0);
        }

        /// <summary>
        /// Draws a single feature based on world space coordinates
        /// </summary>
        /// <param name="position"> Vector3 position</param>

        protected VirgisFeature DrawFeature(Vector3 position, object fid, string label = "") {
            //instantiate the prefab with coordinates defined above
            GameObject dataPoint = Instantiate(m_pointPrefab, transform);
            Datapoint com = dataPoint.GetComponent<Datapoint>();
            com.SetFID(fid);
            com.Spawn(transform);
            SerializableMaterialHash point_hash;
            if (!m_materials.TryGetValue("point", out point_hash))
                point_hash = new();
            com.SetMaterial(point_hash);

            // add the data from source
            dataPoint.transform.position = position;
            var localPostion = dataPoint.transform.localPosition;

            //Set the symbology
            if (m_symbology.ContainsKey("point")) {
                dataPoint.transform.localScale = m_symbology["point"].Transform.Scale;
                dataPoint.transform.localRotation = m_symbology["point"].Transform.Rotate;
                dataPoint.transform.Translate(m_symbology["point"].Transform.Position, Space.Self);
            }


            //Set the label
            if (label != "") {
                GameObject labelObject = Instantiate(parent.LabelPrefab,
                                                     dataPoint.transform, false
                                                     );
                labelObject.transform.localScale = labelObject.transform.localScale * Vector3.one.magnitude / dataPoint.transform.localScale.magnitude;
                labelObject.transform.localPosition = Vector3.up * m_displacement;
                Text labelText = labelObject.GetComponentInChildren<Text>();
                labelText.text = label;
            }

            return com;
        }

        protected Task<int> DrawFeatureAsync(Vector3 position, object fid, string label = "") {
            Task<int> t1 = new Task<int>(() => {
                DrawFeature(position, fid, label);
                return 1;
            });
            t1.Start(TaskScheduler.FromCurrentSynchronizationContext());
            return t1;
        }

        public override Shapes GetFeatureShape() {
            if (m_symbology.ContainsKey("point") &&
                m_symbology["point"].ContainsKey("Shape")) {
                return m_symbology["point"].Shape;
            }
            return Shapes.None;
        }

        public override void _checkpoint() {
        }

        protected VirgisFeature _addFeature(Vector3[] geometry) {
            VirgisFeature newFeature = DrawFeature(geometry[0], GetNextFID());
            changed = true;
            return newFeature;
        }

        public void RemoveVertex(VirgisFeature vertex) {
            if (AppState.instance.InEditSession() && IsWriteable) {
                Destroy(vertex.gameObject);
            }
        }

        protected abstract object GetNextFID();

        public async override Task _save() {
            IEnumerator saver = hydrate();
            while (saver.MoveNext()) {
                await Task.Yield();
            };
            await transform.parent.GetComponent<VirgisLayer>().GetLoader()._save();
            return;
        }

        protected abstract IEnumerator hydrate();

    }
}
