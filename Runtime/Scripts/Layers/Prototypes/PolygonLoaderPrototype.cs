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
        protected GameObject m_handlePrefab;
        protected GameObject m_linePrefab;
        protected PolygonLayer parent;


        public override void ReadSymbology() {
            parent = MParent as PolygonLayer;
            RecordSet layer = _layer as RecordSet;

            if (MSymbology.ContainsKey("point") &&
                MSymbology["point"].ContainsKey("Shape")) {
                Shapes shape = MSymbology["point"].Shape;
                switch (shape) {
                    case Shapes.Spheroid:
                        m_handlePrefab = parent.SpherePrefab;
                        break;
                    case Shapes.Cuboid:
                        m_handlePrefab = parent.CubePrefab;
                        break;
                    case Shapes.Cylinder:
                        m_handlePrefab = parent.CylinderPrefab;
                        break;
                    default:
                        m_handlePrefab = parent.SpherePrefab;
                        break;
                }
            } else {
                m_handlePrefab = parent.SpherePrefab;
            }

            if (MSymbology.ContainsKey("line") && 
                MSymbology["line"].ContainsKey("Shape")) {
                Shapes shape = MSymbology["line"].Shape;
                switch (shape) {
                    case Shapes.Cuboid:
                        m_linePrefab = parent.CuboidLinePrefab;
                        break;
                    case Shapes.Cylinder:
                        m_linePrefab = parent.CylinderLinePrefab;
                        break;
                    default:
                        m_linePrefab = parent.CylinderLinePrefab;
                        break;
                }
            } else {
                m_linePrefab = parent.CylinderLinePrefab;
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
                        GetNextFID()
                    );
                default:
                    throw new System.Exception("Incorrect Type passed to _addFeature");
            }
        }

        protected VirgisFeature _drawFeature(List<DCurve3> poly, object fid, string label = "")
        {
            //Create the GameObjects
            GameObject dataPoly = Instantiate(parent.PolygonPrefab, transform, false);
            Datapolygon p = dataPoly.GetComponent<Datapolygon>();
            p.SetFID(fid);
            if (label !=  "") {
                //Set the label
                GameObject labelObject = Instantiate(parent.LabelPrefab, dataPoly.transform, false);
                labelObject.transform.Translate(dataPoly.transform.TransformVector(Vector3.up) *
                                                MSymbology["point"].Transform.Scale.magnitude, Space.Self);
                Text labelText = labelObject.GetComponentInChildren<Text>();
                labelText.text = label;
            }
            p.Spawn(transform);

            // Draw the LinearRings
            List<Dataline> polygon = new();
            foreach (DCurve3 curve in poly) {
                GameObject dataLine = Instantiate(m_linePrefab, dataPoly.transform, false);
                Dataline com = dataLine.GetComponent<Dataline>();
                com.Spawn(dataPoly.transform);
                com.Symbology = MSymbology.ToDictionary(
                        item => item.Key,
                        item => item.Value as UnitPrototype
                    );
                curve.Closed = true;
                com.Draw(curve,
                    MMaterials, 
                    m_handlePrefab, 
                    null
                );
                polygon.Add(com);
            }

            //Draw the Polygon
            p.Draw(polygon, MMaterials);

            return p;
        }

        protected Task<int> _drawFeatureAsync(List<DCurve3> poly, object fid, string label = "") {

            Task<int> t1 = new Task<int>(() => {
                _drawFeature(poly, fid, label);
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
