/******************************************************************************
 *
 * Name:     GdalConfiguration.cs.pp
 * Project:  GDAL CSharp Interface
 * Purpose:  A static configuration utility class to enable GDAL/OGR.
 * Author:   Felix Obermaier
 *
 ******************************************************************************
 * Copyright (c) 2012-2018, Felix Obermaier
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 *****************************************************************************/

/******************************************************************************
* Added PDAL config code Copyright (c) 2019, Simverge Software LLC. All rights reserved.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following
* conditions are met:
*
* 1. Redistributions of source code must retain the above copyright notice,
*    this list of conditions and the following disclaimer.
* 2. Redistributions in binary form must reproduce the above copyright notice,
     this list of conditions and the following disclaimer in the documentation
*    and/or other materials provided with the distribution.
* 3. Neither the name of Simverge Software LLC nor the names of its
*    contributors may be used to endorse or promote products derived from this
*    software without specific prior written permission.
*
* THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
* AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
* IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
* ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
* LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
* CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
* SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
* INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
* CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
* ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
* POSSIBILITY OF SUCH DAMAGE.
*****************************************************************************/

using System;
using System.IO;
using UnityEngine;
using System.Runtime.InteropServices;
using Gdal = OSGeo.GDAL.Gdal;
using Ogr = OSGeo.OGR.Ogr;
using Osr = OSGeo.OSR.Osr;
using Debug = UnityEngine.Debug;

namespace Virgis
{
    public static class GdalConfiguration 
    {
        private static volatile bool _configuredOgr;
        private static volatile bool _configuredGdal;
        private static volatile bool _usable;

        public enum GDAL_TYPES {
            
        }

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetDefaultDllDirectories(uint directoryFlags);
        //               LOAD_LIBRARY_SEARCH_USER_DIRS | LOAD_LIBRARY_SEARCH_SYSTEM32
        private const uint DllSearchFlags = 0x00000400 | 0x00000800;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AddDllDirectory(string lpPathName);

        /// <summary>
        /// Construction of Gdal/Ogr
        /// </summary>
        static GdalConfiguration()
        {
            try
            {
                // Set the GDAL environment variables.
                string gdalPath = Application.streamingAssetsPath;
                string gdalData = Path.Combine(gdalPath, "gdal-data");
                string projData = Path.Combine(gdalPath, "proj");
                Environment.SetEnvironmentVariable("GDAL_DATA", gdalData);
                Gdal.SetConfigOption("GDAL_DATA", gdalData);

                //string driverPath = Path.Combine(nativePath, "plugins");
                Environment.SetEnvironmentVariable("GDAL_DRIVER_PATH", gdalData);
                Gdal.SetConfigOption("GDAL_DRIVER_PATH", gdalData);

                Environment.SetEnvironmentVariable("GEOTIFF_CSV", gdalData);
                Gdal.SetConfigOption("GEOTIFF_CSV", gdalData);

                //string projSharePath = Path.Combine(gdalPath, "share");
                Environment.SetEnvironmentVariable("PROJ_LIB", projData);
                Gdal.SetConfigOption("PROJ_LIB", projData);
                Osr.SetPROJSearchPath(projData);

                _usable = true;

                Debug.Log($"GDAL version string : {Gdal.VersionInfo(null)}");
            }
            catch (Exception e)
            {
                _usable = false;
                Debug.LogError(e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Gets a value indicating if the GDAL package is set up properly.
        /// </summary>
        public static bool Usable
        {
            get { return _usable; }
        }

        /// <summary>
        /// Method to ensure the static constructor is being called.
        /// </summary>
        /// <remarks>Be sure to call this function before using Gdal/Ogr/Osr</remarks>
        public static void ConfigureOgr()
        {
            if (!_usable) return;
            if (_configuredOgr) return;

            // Register drivers
            Ogr.RegisterAll();
            _configuredOgr = true;

            PrintDriversOgr();
        }

        /// <summary>
        /// Method to ensure the static constructor is being called.
        /// </summary>
        /// <remarks>Be sure to call this function before using Gdal/Ogr/Osr</remarks>
        public static void ConfigureGdal()
        {
            if (!_usable) return;
            if (_configuredGdal) return;

            // Register drivers
            Gdal.AllRegister();
            _configuredGdal = true;

            PrintDriversGdal();
        }


        /// <summary>
        /// Function to determine which platform we're on
        /// </summary>
        private static string GetPlatform()
        {
            return Environment.Is64BitProcess ? "x64" : "x86";
        }

        /// <summary>
        /// Gets a value indicating if we are on a windows platform
        /// </summary>
        private static bool IsWindows
        {
            get
            {
                var res = !(Environment.OSVersion.Platform == PlatformID.Unix ||
                            Environment.OSVersion.Platform == PlatformID.MacOSX);

                return res;
            }
        }
        private static void PrintDriversOgr()
        {
            if (_usable)
            {
                string drivers = "";
                var num = Ogr.GetDriverCount();
                for (var i = 0; i < num; i++)
                {
                    var driver = Ogr.GetDriver(i);
                    drivers += $"OGR {i}: {driver.GetName()}";
                    drivers += ", ";
                }
                Debug.Log($"OGR Drivers : {drivers}");

            }
        }

        private static void PrintDriversGdal()
        {
            if (_usable)
            {
                string drivers = "";
                var num = Gdal.GetDriverCount();
                for (var i = 0; i < num; i++)
                {
                    var driver = Gdal.GetDriver(i);
                    drivers += $"GDAL {i}: {driver.ShortName}-{driver.LongName}";
                    drivers += ", ";
                }
                Debug.Log($"GDAL Drivers : {drivers}");

            }
        }

        public static void ConfigureMdal() {
            Debug.Log("Mdal Version " + Mdal.Mdal.GetVersion());
        }

        public static void ConfiurePdal() {
            pdal.Config config = new pdal.Config();
            Debug.Log("GDAL Data Path: " + config.GdalData);
            Debug.Log("Proj4 Data Path: " + config.Proj4Data);

            Debug.Log("PDAL Version Integer: " + config.VersionInteger);
            Debug.Log("PDAL Version Major: " + config.VersionMajor);
            Debug.Log("PDAL Version Minor: " + config.VersionMinor);
            Debug.Log("PDAL Version Patch: " + config.VersionPatch);

            Debug.Log("PDAL Full Version: " + config.FullVersion);
            Debug.Log("PDAL Version: " + config.Version);
            Debug.Log("PDAL SHA1: " + config.Sha1);
            Debug.Log("PDAL Debug Info: " + config.DebugInfo);
        }
    }
}