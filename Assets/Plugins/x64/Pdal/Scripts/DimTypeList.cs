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
using System.Collections.Generic;

 namespace pdal
 {
	public class DimTypeList : IDisposable
	{
		private const string PDALC_LIBRARY = "pdalc";

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetDimTypeListSize")]
		private static extern uint getSize(IntPtr list);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetDimTypeListByteCount")]
		private static extern ulong getByteCount(IntPtr list);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetDimType")]
		[return:MarshalAs( UnmanagedType.Struct)]
		private static extern DimType.NativeDimType getType(IntPtr list, uint index);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALDisposeDimTypeList")]
		private static extern void dispose(IntPtr list);

		private IntPtr mNative = IntPtr.Zero;
		private Dictionary<uint, DimType> mCache = new Dictionary<uint, DimType>();

		public DimTypeList(IntPtr nativeList)
		{
			mNative = nativeList;
		}

		public ulong ByteCount
		{
			get { return getByteCount(mNative); }
		}

		public IntPtr Native
		{
			get { return mNative; }
		}

		public uint Size
		{
			get { return getSize(mNative); }
		}

		public void Dispose()
		{
			dispose(mNative);
		}

		public DimType at(uint index)
		{
			DimType type = null;
			
			if (!mCache.TryGetValue(index, out type))
			{
				type = new DimType(getType(mNative, index));
				mCache.Add(index, type);
			}

			return type;
		}
	}
 }
