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

        public override void ReadSymbology() {
            parent = MParent as PointLayer;
            MDisplacement = 1.0f;
            if (MSymbology.ContainsKey("point") &&
                MSymbology["point"].ContainsKey("Shape")) {
                Shapes shape = MSymbology["point"].Shape;
                switch (shape) {
                    case Shapes.Spheroid:
                        m_pointPrefab = parent.SpherePrefab;
                        break;
                    case Shapes.Cuboid:
                        m_pointPrefab = parent.CubePrefab;
                        break;
                    case Shapes.Cylinder:
                        m_pointPrefab = parent.CylinderPrefab;
                        MDisplacement = 1.5f;
                        break;
                    default:
                        m_pointPrefab = parent.SpherePrefab;
                        break;
                }
            } else {
                m_pointPrefab = parent.SpherePrefab;
            }

            MMaterials = new Dictionary<string, SerializableMaterialHash>();

            foreach (string key in MSymbology.Keys) {
                UnitPrototype unit = MSymbology[key];
                SerializableMaterialHash hash = new() {
                    Name = key,
                    Color = unit.Color,
                };
                MMaterials.Add(key, hash);
                if (key == "point") MParent.DefaultCol.Value = hash;
            }
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
            if (!MMaterials.TryGetValue("point", out point_hash))
                point_hash = new();
            com.SetMaterial(point_hash);

            // add the data from source
            dataPoint.transform.localPosition = position;
            var localPostion = dataPoint.transform.localPosition;

            //Set the symbology
            if (MSymbology.ContainsKey("point")) {
                dataPoint.transform.localScale = MSymbology["point"].Transform.Scale;
                dataPoint.transform.localRotation = MSymbology["point"].Transform.Rotate;
                dataPoint.transform.Translate(MSymbology["point"].Transform.Position, Space.Self);
            }


            //Set the label
            if (label != "") {
                GameObject labelObject = Instantiate(parent.LabelPrefab,
                                                     dataPoint.transform, false
                                                     );
                labelObject.transform.localScale = labelObject.transform.localScale * Vector3.one.magnitude / dataPoint.transform.localScale.magnitude;
                labelObject.transform.localPosition = Vector3.up * MDisplacement;
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
            if (MSymbology.ContainsKey("point") &&
                MSymbology["point"].ContainsKey("Shape")) {
                return MSymbology["point"].Shape;
            }
            return Shapes.None;
        }

        public override IVirgisFeature _addFeature<S>(S geometry) {
            switch (geometry) {
                case Vector3 v:
                    VirgisFeature newFeature = DrawFeature(v, GetNextFID());
                    changed = true;
                    return newFeature;
                default:
                    throw new System.Exception("Incorrect Type passed to _addFeature");
            }
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
