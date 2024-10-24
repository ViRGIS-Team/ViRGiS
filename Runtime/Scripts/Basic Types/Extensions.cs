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

using VirgisGeometry;
using OSGeo.OSR;
using DXF = netDxf;

namespace Virgis {

    public static class VirgisVectorExtensionsGeo {

        /// <summary>
        /// Convert vector3D to a netDXF Vector3 in z-up coordinate frame
        /// using the optional CoordinateTranform to reproject the dpoint if present
        /// </summary>
        /// <param name="v"> Vector3d</param>
        /// <param name="transform">Coordinate Transform</param>
        /// <returns>DXF.Vector3</returns>
        public static DXF.Vector3 ToDxfVector3(this Vector3d v, CoordinateTransformation transform = null, AxisOrder ax = default) {
            v.ChangeAxisOrderTo(ax == default ? AxisOrder.ENU : ax);
            double[] vlocal = new double[3] { v.x, v.y, v.z };
            if (transform != null) {
                transform.TransformPoint(vlocal);
            }
            return new DXF.Vector3(vlocal[0], vlocal[1], vlocal[2]);
        }
    }
    
    public static class DXFExtensions {
        /// <summary>
        /// Convert a netDXF Vector3 in z-up coordinate frame to a Vector3D 
        /// </summary>
        /// <param name="v">DXF.Vector3</param>
        /// <returns>Vector3d</returns>
        public static Vector3d ToVector3d(this DXF.Vector3 v, AxisOrder ax = default) {
            return new Vector3d((float) v.X, (float) v.Y, (float) v.Z) { 
                axisOrder = ax == default ? AxisOrder.ENU : ax
                };
        }
    }

    public static class VirgisMeshExtensions {
        /// <summary>
        /// Transform projected Dmesh to World Space
        /// </summary>
        /// <returns>bool true if successful</returns>
        public static bool Transform(this DMesh3 dMesh) {
            var crs = dMesh.FindMetadata("CRS");
            // if the Dmesh3 contains a CRS use that
            if (crs != null && crs != "") {
                SpatialReference from;
                switch (crs) {
                    case string s:
                        from = OsrExtensions.TextToSR(s);
                        break;
                    case SpatialReference sr:
                        from = sr;
                        break;
                    default:
                        UnityEngine.Debug.LogError("Invalid CRS Metadata in DMesh");
                        return false;
                }
                CoordinateTransformation trans = AppState.instance.projectTransformer(from);
                return dMesh.Project(trans, AppState.instance.projectCrs.GetAxisOrder());
            }
            dMesh.axisOrder = AxisOrder.EUN;
            return false;
        }
    }
}