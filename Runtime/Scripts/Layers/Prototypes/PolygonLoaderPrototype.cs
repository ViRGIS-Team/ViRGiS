/* MIT License

Copyright (c) 2020 - 23 Runette Software

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

using Project;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VirgisGeometry;
using System.Linq;
using System.Collections;

namespace Virgis
{

    /// <summary>
    /// Controls an instance of a Polygon Layer
    /// </summary>
    public abstract class PolygonLoaderPrototype<T> : VirgisLoader<T>
    {
        private GameObject _mHandlePrefab;
        private GameObject _mLinePrefab;
        private PolygonLayer _mParent;


        public override void ReadSymbology() {
            _mParent = MParent as PolygonLayer;
            RecordSet layer = Layer as RecordSet;

            if (MSymbology.ContainsKey("point") &&
                MSymbology["point"].ContainsKey("Shape")) {
                Shapes shape = MSymbology["point"].Shape;
                switch (shape) {
                    case Shapes.Spheroid:
                        _mHandlePrefab = _mParent?.SpherePrefab;
                        break;
                    case Shapes.Cuboid:
                        _mHandlePrefab = _mParent?.CubePrefab;
                        break;
                    case Shapes.Cylinder:
                        _mHandlePrefab = _mParent?.CylinderPrefab;
                        break;
                    default:
                        _mHandlePrefab = _mParent?.SpherePrefab;
                        break;
                }
            } else {
                _mHandlePrefab = _mParent?.SpherePrefab;
            }

            if (MSymbology.ContainsKey("line") && 
                MSymbology["line"].ContainsKey("Shape")) {
                Shapes shape = MSymbology["line"].Shape;
                switch (shape) {
                    case Shapes.Cuboid:
                        _mLinePrefab = _mParent?.CuboidLinePrefab;
                        break;
                    case Shapes.Cylinder:
                        _mLinePrefab = _mParent?.CylinderLinePrefab;
                        break;
                    default:
                        _mLinePrefab = _mParent?.CylinderLinePrefab;
                        break;
                }
            } else {
                _mLinePrefab = _mParent?.CylinderLinePrefab;
            }
            
            MMaterials = new Dictionary<string, SerializableMaterialHash>();

            foreach (string key in MSymbology.Keys) {
                UnitPrototype unit = MSymbology[key];
                SerializableMaterialHash hash = new() {
                    Name = key,
                    Color = unit.Color,
                };
                if (key == "body") {
                    hash.AddProperty(new() {
                        Key = "_TextureSwitch",
                        Value = 0
                    });
                }
                MMaterials.Add(key, hash);
                if (key == "point")
                    MParent.DefaultCol.Value = hash;
            }
        }

        public override IVirgisFeature _addFeature<S>(S geometry) {
            switch (geometry) {
                case Vector3[] line:
                    changed = true;
                    return _drawFeature(
                        new List<DCurve3>() { 
                            new DCurve3(line, true) { 
                                axisOrder = AxisOrder.EUN 
                            } 
                        },
                        GetNextFid(),0
                    );
                default:
                    throw new System.Exception("Incorrect Type passed to _addFeature");
            }
        }

        private VirgisFeature _drawFeature(List<DCurve3> poly, object fid, object gid, string label = "")
        {
            //Create the GameObjects
            GameObject dataPoly = Instantiate(_mParent.PolygonPrefab, transform, false);
            Datapolygon p = dataPoly.GetComponent<Datapolygon>();
            p.SetFid(fid);
            p.SetGid(gid);
            if (label !=  "") {
                //Set the label
                GameObject labelObject = Instantiate(_mParent.LabelPrefab, dataPoly.transform, false);
                labelObject.transform.Translate(dataPoly.transform.TransformVector(Vector3.up) *
                                                MSymbology["point"].Transform.Scale.Magnitude, Space.Self);
                Text labelText = labelObject.GetComponentInChildren<Text>();
                labelText.text = label;
            }
            p.Spawn(transform);

            // Draw the LinearRings
            List<Dataline> polygon = new(); 
            for (int i =0; i < poly.Count; i++) {
                DCurve3 curve = poly[i];
                GameObject dataLine = Instantiate(_mLinePrefab, dataPoly.transform, false);
                Dataline com = dataLine.GetComponent<Dataline>();
                com.Spawn(dataPoly.transform);
                com.SetFid(fid);
                com.SetGid(i);
                com.Symbology = MSymbology.ToDictionary(
                        item => item.Key,
                        item => item.Value
                    );
                curve.Closed = true;
                com.Draw(curve,
                    MMaterials, 
                    _mHandlePrefab, 
                    null
                );
                polygon.Add(com);
            }

            //Draw the Polygon
            p.Draw(polygon, MMaterials);

            return p;
        }

        protected Task<int> _drawFeatureAsync(List<DCurve3> poly, object fid, object gid, string label = "") {

            Task<int> t1 = new Task<int>(() => {
                _drawFeature(poly, fid, gid, label);
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
