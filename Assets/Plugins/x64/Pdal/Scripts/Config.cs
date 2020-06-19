/******************************************************************************
 * Copyright (c) 2019, Simverge Software LLC. All rights reserved.
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
using System.Runtime.InteropServices;
using System.Text;

namespace pdal
{
	/**
	 * A utility to retrieve PDAL version and configuration information
	 */
	public class Config
	{
		private const string PDALC_LIBRARY = "pdalc";

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetGdalDataPath")]
		private static extern int getGdalDataPath([MarshalAs(UnmanagedType.LPStr)] StringBuilder buffer, uint size);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetProj4DataPath")]
		private static extern int getProj4DataPath([MarshalAs(UnmanagedType.LPStr)] StringBuilder buffer, uint size);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALSetGdalDataPath")]
		private static extern void setGdalDataPath(string path);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALSetProj4DataPath")]
		private static extern void setProj4DataPath(string path);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALFullVersionString")]
		private static extern void getFullVersion([MarshalAs(UnmanagedType.LPStr)] StringBuilder buffer, uint size);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALVersionString")]
		private static extern void getVersion([MarshalAs(UnmanagedType.LPStr)] StringBuilder buffer, uint size);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALVersionInteger")]
		private static extern int getVersionInteger();

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALSha1")]
		private static extern void getSha1([MarshalAs(UnmanagedType.LPStr)] StringBuilder buffer, uint size);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALVersionMajor")]
		private static extern int getVersionMajor();

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALVersionMinor")]
		private static extern int getVersionMinor();

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALVersionPatch")]
		private static extern int getVersionPatch();

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALDebugInformation")]
		private static extern void getDebugInfo([MarshalAs(UnmanagedType.LPStr)] StringBuilder buffer, uint size);

		/**
		 * Creates a new PDAL configuration.
		 *
		 * @post GdalData will be initialized to `"Assets/gdal/Data"`
		 * @post Proj4Data will be initialized to `"Assets/proj4/Data"`
		 *
		 * @note Set GdalData and Proj4Data as needed if their initialization
		 *       values do not correspond to the actual GDAL and proj4 data
		*        directory paths.
		 */
		public Config()
		{
			//string cwd = Environment.CurrentDirectory;

			GdalData = "Assets/plugis/x64/Gdal/gdal-data";
			Proj4Data = "Assets/plugins/x64/Gdal/gdal-data";
		}

		/// The path to the GDAL data directory
		public string GdalData
		{
			get
			{
				StringBuilder buffer = new StringBuilder(256);
				getGdalDataPath(buffer, (uint) buffer.Capacity);
				return buffer.ToString();
			}

			set	{ setGdalDataPath(value); }
		}

		/// The path to the proj4 data directory
		public string Proj4Data
		{
			get
			{
				StringBuilder buffer = new StringBuilder(256);
				getProj4DataPath(buffer, (uint) buffer.Capacity);
				return buffer.ToString();
			}

			set	{ setProj4DataPath(value); }
		}

		/// The PDAL full version string with dot-separated major, minor, and patch version numbers and PDAL commit SHA1
		public string FullVersion
		{
			get
			{
				StringBuilder buffer = new StringBuilder(64);
				getFullVersion(buffer, (uint) buffer.Capacity);
				return buffer.ToString();
			}
		}

		/// The PDAL version string with dot-separated major, minor, and patch version numbers
		public string Version
		{
			get
			{
				StringBuilder buffer = new StringBuilder(64);
				getVersion(buffer, (uint) buffer.Capacity);
				return buffer.ToString();
			}
		}

		/**
		 * The PDAL version number as a single integer.
		 * The version number is equal to the sum of 10,000 times the major version number,
		 * 100 times the minor version number, and the patch version number. For example,
		 * version _1.7.1_ is represented as `10701`.
		 */
		public int VersionInteger
		{
			get { return getVersionInteger(); }
		}

		/// The SHA1 checksum of the Git commit used to build the underlying PDAL library
		public string Sha1
		{
			get
			{
				StringBuilder buffer = new StringBuilder(64);
				getSha1(buffer, (uint) buffer.Capacity);
				return buffer.ToString();
			}
		}

		/// The PDAL major version number
		public int VersionMajor
		{
			get { return getVersionMajor(); }
		}

		/// The PDAL minor version number
		public int VersionMinor
		{
			get { return getVersionMinor(); }
		}

		/// The PDAL patch version number
		public int VersionPatch
		{
			get { return getVersionPatch(); }
		}

		/// The string describing useful debugging information from the underlying PDAL library
		public string DebugInfo
		{
			get
			{
				StringBuilder buffer = new StringBuilder(1024);
				getDebugInfo(buffer, (uint) buffer.Capacity);
				return buffer.ToString();
			}
		}
	}
}
