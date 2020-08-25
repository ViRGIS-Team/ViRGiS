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
using System.Collections.Generic;
using System.Runtime.InteropServices;

 namespace pdal
 {
	public class PointLayout
	{
		private const string PDALC_LIBRARY = "pdalc";

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetPointLayoutDimTypes")]
		private static extern IntPtr getDimTypeList(IntPtr layout);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALFindDimType")]
		[return:MarshalAs(UnmanagedType.Struct)]
		private static extern DimType.NativeDimType findDimType(IntPtr layout, string name);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetDimSize")]
		private static extern uint getDimSize(IntPtr layout, string name);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetDimPackedOffset")]
		private static extern uint getDimPackedOffset(IntPtr layout, string name);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetPointSize")]
		private static extern uint getPointSize(IntPtr layout);

		private IntPtr mNative = IntPtr.Zero;
		private DimTypeList mTypes = null;
		private Dictionary<string, DimType> mCache = new Dictionary<string, DimType>();

		public PointLayout(IntPtr nativeLayout)
		{
			mNative = nativeLayout;
			IntPtr nativeTypes = (mNative != IntPtr.Zero) ? getDimTypeList(mNative) : IntPtr.Zero;

			if (nativeTypes != IntPtr.Zero)
			{
				mTypes = new DimTypeList(nativeTypes);
			}
		}

		public DimTypeList Types
		{
			get { return mTypes; }
		}

		public uint PointSize
		{
			get { return getPointSize(mNative); }
		}

		public DimType FindDimType(string name)
		{
			DimType type = null;
			
			if (!mCache.TryGetValue(name, out type))
			{
				type = new DimType(findDimType(mNative, name));
				mCache.Add(name, type);
			}

			return type;
		}

		public uint GetDimSize(string name)
		{
			return getDimSize(mNative, name);
		}

		public uint GetDimPackedOffset(string name)
		{
			return getDimPackedOffset(mNative, name);
		}
	}
 }
