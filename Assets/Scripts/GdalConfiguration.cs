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

using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using System.Runtime.InteropServices;
using Gdal = OSGeo.GDAL.Gdal;
using Ogr = OSGeo.OGR.Ogr;
using Osr = OSGeo.OSR.Osr;

namespace Virgis
{
    public static class GdalConfiguration 
    {
        private static volatile bool _configuredOgr;
        private static volatile bool _configuredGdal;
        private static volatile bool _usable;

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
             string executingDirectory = null, gdalPath = null, nativePath = null;
            try
            {
                //if (!IsWindows)
                //{
                //    const string notSet = "_Not_set_";
                //    string tmp = Gdal.GetConfigOption("GDAL_DATA", notSet);
                //    _usable = tmp != notSet;
                //    return;
                //}

                executingDirectory = Directory.GetCurrentDirectory();

                if (string.IsNullOrEmpty(executingDirectory))
                    throw new InvalidOperationException("cannot get executing directory");


                //// modify search place and order
                //SetDefaultDllDirectories(DllSearchFlags);

                nativePath = Path.Combine(executingDirectory, "Assets\\plugins", GetPlatform());

                gdalPath = Path.Combine(nativePath, "Gdal");

                if (!Directory.Exists(gdalPath))
                    throw new DirectoryNotFoundException($"GDAL native directory not found at '{nativePath}'");
                if (!File.Exists(Path.Combine(gdalPath, "gdal_wrap.dll")))
                    throw new FileNotFoundException(
                        $"GDAL native wrapper file not found at '{Path.Combine(nativePath, "gdal_wrap.dll")}'");

                //// Add directories
                AddDllDirectory(gdalPath);
                AddDllDirectory(Path.Combine(nativePath, "plugins"));

                // Set the additional GDAL environment variables.
                string gdalData = Path.Combine(gdalPath, "gdal-data");
                Environment.SetEnvironmentVariable("GDAL_DATA", gdalData);
                Gdal.SetConfigOption("GDAL_DATA", gdalData);

                //string driverPath = Path.Combine(nativePath, "plugins");
                Environment.SetEnvironmentVariable("GDAL_DRIVER_PATH", gdalData);
                Gdal.SetConfigOption("GDAL_DRIVER_PATH", gdalData);

                Environment.SetEnvironmentVariable("GEOTIFF_CSV", gdalData);
                Gdal.SetConfigOption("GEOTIFF_CSV", gdalData);

                //string projSharePath = Path.Combine(gdalPath, "share");
                Environment.SetEnvironmentVariable("PROJ_LIB", gdalData);
                Gdal.SetConfigOption("PROJ_LIB", gdalData);
                Osr.SetPROJSearchPath(gdalData);

                _usable = true;

                UnityEngine.Debug.Log($"GDAL version string : {Gdal.VersionInfo(null)}");
            }
            catch (Exception e)
            {
                _usable = false;
                UnityEngine.Debug.LogError(e.ToString());
                UnityEngine.Debug.LogError($"Executing directory: {executingDirectory}");
                UnityEngine.Debug.LogError($"gdal directory: {gdalPath}");
                UnityEngine.Debug.LogError($"native directory: {nativePath}");

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
                UnityEngine.Debug.Log($"OGR Drivers : {drivers}");

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
                UnityEngine.Debug.Log($"GDAL Drivers : {drivers}");

            }
        }
    }
}