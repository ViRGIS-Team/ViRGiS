using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UI;
using System.Collections;

namespace Virgis {
    public abstract class PointLoaderPrototype<T> : VirgisLoader<T> {
        private GameObject _mPointPrefab;
        private PointLayer _mParent;

        public override void ReadSymbology() {
            _mParent = MParent as PointLayer;
            MDisplacement = 1.0f;
            if (MSymbology.ContainsKey("point") &&
                MSymbology["point"].ContainsKey("Shape")) {
                Shapes shape = MSymbology["point"].Shape;
                switch (shape) {
                    case Shapes.Spheroid:
                        _mPointPrefab = _mParent?.SpherePrefab;
                        break;
                    case Shapes.Cuboid:
                        _mPointPrefab = _mParent?.CubePrefab;
                        break;
                    case Shapes.Cylinder:
                        _mPointPrefab = _mParent?.CylinderPrefab;
                        MDisplacement = 1.5f;
                        break;
                    default:
                        _mPointPrefab = _mParent?.SpherePrefab;
                        break;
                }
            } else {
                _mPointPrefab = _mParent?.SpherePrefab;
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
        ///  Draws a single feature based on world space coordinates
        /// </summary>
        /// <param name="position">Vector3 loaclPosition</param>
        /// <param name="fid">Feature Id</param>
        /// <param name="gid">Geometry Id</param>
        /// <param name="label">Label fpr this faeture</param>
        /// <returns></returns>
        private VirgisFeature DrawFeature(Vector3 position, object fid, object gid,string label = "") {
            //instantiate the prefab with coordinates defined above
            GameObject dataPoint = Instantiate(_mPointPrefab, transform);
            Datapoint com = dataPoint.GetComponent<Datapoint>();
            com.SetFid(fid);
            com.SetGid(gid);
            com.Spawn(transform);
            if (!MMaterials.TryGetValue("point", out SerializableMaterialHash pointHash))
                pointHash = new();
            com.SetMaterial(pointHash);

            // add the data from source
            dataPoint.transform.localPosition = position;

            //Set the symbology
            if (MSymbology.ContainsKey("point")) {
                dataPoint.transform.localScale = MSymbology["point"].Transform.Scale;
                dataPoint.transform.localRotation = MSymbology["point"].Transform.Rotate;
                dataPoint.transform.Translate(MSymbology["point"].Transform.Position, Space.Self);
            }


            //Set the label
            if (label != "") {
                GameObject labelObject = Instantiate(_mParent.LabelPrefab,
                                                     dataPoint.transform, false
                                                     );
                labelObject.transform.localScale = labelObject.transform.localScale * Vector3.one.magnitude / dataPoint.transform.localScale.magnitude;
                labelObject.transform.localPosition = Vector3.up * MDisplacement;
                Text labelText = labelObject.GetComponentInChildren<Text>();
                labelText.text = label;
            }

            return com;
        }

        protected Task<int> DrawFeatureAsync(Vector3 position, object fid, object gid, string label = "") {
            Task<int> t1 = new Task<int>(() => {
                DrawFeature(position, fid, gid, label);
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
                    VirgisFeature newFeature = DrawFeature(v, GetNextFid(), 0);
                    changed = true;
                    return newFeature;
                default:
                    throw new System.Exception("Incorrect Type passed to _addFeature");
            }
        }

        public void RemoveVertex(VirgisFeature vertex) {
            if (AppState.Instance.InEditSession() && IsWriteable) {
                Destroy(vertex.gameObject);
            }
        }

        protected abstract object GetNextFid();

        public async override Task _save() {
            IEnumerator saver = Hydrate();
            while (saver.MoveNext()) {
                await Task.Yield();
            }
            await transform.parent.GetComponent<VirgisLayer>().GetLoader()._save();

        }

    }
}
