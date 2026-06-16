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

namespace Virgis {

    

    public static class VirgisMeshExtensions {
        /// <summary>
        /// Transform projected Dmesh to World Space
        /// </summary>
        /// <returns>bool true if successful</returns>
        public static bool Transform(this DMesh3 dMesh) {
            var crs = dMesh.FindMetadata("CRS");
            // if the Dmesh3 contains a CRS use that
            if (crs != null ) {
                SpatialReference from;
                switch (crs) {
                    case string s:
                        if (s == "")
                            return false;
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
                return dMesh.Project(trans, AxisOrder.ENU);
            }
            dMesh.axisOrder = AxisOrder.EUN;
            return false;
        }
    }
}