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
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using VirgisGeometry;
using System.Collections;

namespace Virgis
{

    /// <summary>
    /// The parent entity for a instance of a Line Layer - that holds one MultiLineString FeatureCollection
    /// </summary>
    public abstract class LineLoaderPrototype<T> : VirgisLoader<T>
    {
        protected GameObject m_handlePrefab;
        protected GameObject m_linePrefab;
        protected Dictionary<string, Unit> m_symbology;
        protected LineLayer parent;

        protected Task<int> Load() {
            RecordSet layer = _layer as RecordSet;

            if (m_symbology.ContainsKey("point") && m_symbology["point"].ContainsKey("Shape")) {
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

            if (m_symbology.ContainsKey("line") && m_symbology["line"].ContainsKey("Shape")) {
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

            foreach(string key in m_symbology.Keys) {
                Unit unit = m_symbology[key];
                SerializableMaterialHash hash = new() {
                    Name = key,
                    Color = unit.Color,
                };
                m_materials.Add(key, hash);
            }
            return Task.FromResult(1);
        }

        protected VirgisFeature _addFeature(Vector3[] line)
        {
            DCurve3 curve = new(line, false);

            return _drawFeature(curve, "");
        }

        /// <summary>
        /// Draws a single feature based on world space coordinates
        /// </summary>
        /// <param name="line"> Geometry</param>
        /// <param name="fid">Feature ID</param>
        /// <param name="label">Label Text</param>
        protected VirgisFeature _drawFeature(DCurve3 line, object fid, string label = null)
        {
            GameObject dataLine = Instantiate(m_linePrefab, transform);

            //set the gisProject properties
            Dataline com = dataLine.GetComponent<Dataline>();
            com.SetFID(fid);
            com.Spawn(transform);
            com.Symbology = m_symbology.ToDictionary(
                    item => item.Key,
                    item => item.Value as UnitPrototype
                );

            com.Draw(line, 
                m_materials,
                m_handlePrefab, 
                parent.LabelPrefab
            );

            return com;
        }

        protected Task<int> _drawFeatureAsync(DCurve3 line, object fid, string label = null) {

            Task<int> t1 = new Task<int>(() => {
                _drawFeature(line, fid, label);
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
