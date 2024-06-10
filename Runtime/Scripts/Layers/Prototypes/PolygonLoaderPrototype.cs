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
using g3;
using System.Linq;
using System.Collections;
using static Codice.Client.Common.WebApi.WebApiEndpoints;

namespace Virgis
{

    /// <summary>
    /// Controls an instance of a Polygon Layer
    /// </summary>
    public abstract class PolygonLoaderPrototype<T> : VirgisLoader<T>
    {
        protected GameObject m_handlePrefab;
        protected GameObject m_linePrefab;

        protected Dictionary<string, Unit> m_symbology;
        protected PolygonLayer parent;


        protected Task<int> Load() {
            parent = m_parent as PolygonLayer;
            RecordSet layer = _layer as RecordSet;

            if (m_symbology.ContainsKey("point") &&
                m_symbology["point"].ContainsKey("Shape")) {
                Shapes shape = m_symbology["point"].Shape;
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

            if (m_symbology.ContainsKey("line") && 
                m_symbology["line"].ContainsKey("Shape")) {
                Shapes shape = m_symbology["line"].Shape;
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

            foreach (string key in m_symbology.Keys) {
                Unit unit = m_symbology[key];
                SerializableMaterialHash hash = new() {
                    Name = key,
                    Color = unit.Color,
                };
                m_materials.Add(key, hash);
            }
            return Task.FromResult(1);
        }

        protected VirgisFeature _addFeature(Vector3[] line) {
            changed = true;
            return _drawFeature(
                new() { new DCurve3(line.Cast<Vector3d>(), true) },
                GetNextFID()
            );
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
                                                m_symbology["point"].Transform.Scale.magnitude, Space.Self);
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
                com.Symbology = m_symbology.ToDictionary(
                        item => item.Key,
                        item => item.Value as UnitPrototype
                    );
                curve.Closed = true;
                com.Draw(curve,
                    m_materials, 
                    m_handlePrefab, 
                    null
                );
                polygon.Add(com);
            }

            //Draw the Polygon
            p.Draw(polygon);

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

        public override void _checkpoint()
        {
        }

        public override Shapes GetFeatureShape() {
            if (m_symbology.ContainsKey("point") &&
                m_symbology["point"].ContainsKey("Shape")) {
                return m_symbology["point"].Shape;
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
