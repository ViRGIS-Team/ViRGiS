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
using Virgis;
using g3;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace pdal
 {
	public class PointView : IDisposable
	{
		private const string PDALC_LIBRARY = "pdalc";
		private const int BUFFER_SIZE = 1024;

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALDisposePointView")]
		private static extern void dispose(IntPtr view);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetPointViewId")]

		private static extern int id(IntPtr view);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetPointViewSize")]

		private static extern ulong size(IntPtr view);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALIsPointViewEmpty")]

		private static extern bool empty(IntPtr view);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALClonePointView")]

		private static extern IntPtr clone(IntPtr view);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetPointViewProj4")]

		private static extern uint getProj4(IntPtr view, StringBuilder buffer, uint size);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetPointViewWkt")]

		private static extern uint getWkt(IntPtr view, StringBuilder buffer, uint size, bool pretty);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetPointViewLayout")]

		private static extern IntPtr getLayout(IntPtr view);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetPackedPoint")]

		private static extern uint getPackedPoint(IntPtr view, IntPtr types, ulong idx, [MarshalAs(UnmanagedType.LPArray)] byte[] buf);


		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetAllPackedPoints")]

		private static extern ulong getAllPackedPoints(IntPtr view, IntPtr types, [MarshalAs(UnmanagedType.LPArray)] byte[] buf);

		private IntPtr mNative = IntPtr.Zero;

		public PointView(IntPtr nativeView)
		{
			mNative = nativeView;
		}

		public void Dispose()
		{
			dispose(mNative);
			mNative = IntPtr.Zero;
		}

		public int Id
		{
			get { return id(mNative); }
		}

		public ulong Size
		{
			get { return size(mNative); }
		}

		public bool Empty
		{
			get { return empty(mNative); }
		}

		public string Proj4
		{
			get
			{
				StringBuilder buffer = new StringBuilder(BUFFER_SIZE);
				getProj4(mNative, buffer, (uint)buffer.Capacity);
				return buffer.ToString();
			}
		}

		public string Wkt
		{
			get
			{
				StringBuilder buffer = new StringBuilder(BUFFER_SIZE);
				getWkt(mNative, buffer, (uint)buffer.Capacity, false);
				return buffer.ToString();
			}
		}

		public string PrettyWkt
		{
			get
			{
				StringBuilder buffer = new StringBuilder(BUFFER_SIZE);
				getWkt(mNative, buffer, (uint)buffer.Capacity, true);
				return buffer.ToString();
			}
		}

		public PointLayout Layout
		{
			get
			{
				PointLayout layout = null;
				IntPtr nativeLayout = getLayout(mNative);

				if (nativeLayout != IntPtr.Zero)
				{
					layout = new PointLayout(nativeLayout);
				}
				return layout;
			}
		}

        public byte[] GetAllPackedPoints(DimTypeList dims)
        {
            byte[] data = null;

			if (this.Size > 0 && dims != null && dims.Size > 0)
			{
				ulong byteCount = this.Size * dims.ByteCount;
				data = new byte[byteCount];
				getAllPackedPoints(mNative, dims.Native, data);
			}

			return data;
        }

        public byte[] GetPackedPoint(DimTypeList dims, ulong idx)
        {
            byte[] data = null;

			if (this.Size > idx && dims != null && dims.Size > 0)
			{
				data = new byte[dims.ByteCount];
				getPackedPoint(mNative, dims.Native, idx, data);
			}

			return data;
        }

		public PointView Clone()
		{
			PointView clonedView = null;
			IntPtr nativeClone = clone(mNative);
        
			if (nativeClone != IntPtr.Zero)
			{
                clonedView = new PointView(nativeClone);
			}

			return clonedView;
		}

        public BakedPointCloud GetBakedPointCloud(long size) {
            BakedPointCloud pc = new BakedPointCloud();
            PointLayout layout = Layout;
            DimTypeList typelist = layout.Types;
            byte[] data = GetAllPackedPoints(typelist);
            List<Vector3d> positions = new List<Vector3d>();
            List<Vector3f> colors = new List<Vector3f>();

            uint pointSize = layout.PointSize;
            Dictionary<string, int> indexs = new Dictionary<string, int>();
            Dictionary<string, string> types = new Dictionary<string, string>();
            int count = 0;
            bool hasColor = false;
            for (uint j = 0; j < typelist.Size; j++) {
                DimType type = typelist.at(j);
                string interpretationName = type.InterpretationName;
                int interpretationByteCount = type.InterpretationByteCount;
                string name = type.IdName;
                indexs.Add(name, count);
                types.Add(name, interpretationName);
                if (name == "Red")
                    hasColor = true;
                count += interpretationByteCount;
            }

            for (long i = 0; i < size; i++) {
                positions.Add( new Vector3d(parseDouble(data, types["X"], (int) (i * pointSize + indexs["X"])), 
                                            parseDouble(data, types["Y"], (int) (i * pointSize + indexs["Y"])),
                                            parseDouble(data, types["Z"], (int) (i * pointSize + indexs["Z"]))
                              ));
                if (hasColor) 
                    colors.Add(new Vector3f((float) parseColor(data, types["Red"], (int) (i * pointSize + indexs["Red"])),
                                            (float) parseColor(data, types["Green"], (int) (i * pointSize + indexs["Green"])),
                                            (float) parseColor(data, types["Blue"], (int) (i * pointSize + indexs["Blue"]))
                            ));
            }

            pc.Initialize(positions, colors, (int)size);
            return pc;
        }

        private double parseDouble(byte[] buffer, string interpretationName, int position) {
            double value = 0;
            if (interpretationName == "double") {
                value = BitConverter.ToDouble(buffer, position);
            } else if (interpretationName == "float") {
                value = BitConverter.ToSingle(buffer, position);
            } else if (interpretationName.StartsWith("uint64")) {
                value = BitConverter.ToUInt64(buffer, position);
            } else if (interpretationName.StartsWith("uint32")) {
                value = BitConverter.ToUInt32(buffer, position);
            } else if (interpretationName.StartsWith("uint16")) {
                value = BitConverter.ToUInt16(buffer, position);
            } else if (interpretationName.StartsWith("uint8")) {
                value = buffer[position];
            } else if (interpretationName.StartsWith("int64")) {
                value = BitConverter.ToInt64(buffer, position);
            } else if (interpretationName.StartsWith("int32")) {
                value = BitConverter.ToInt32(buffer, position);
            } else if (interpretationName.StartsWith("int16")) {
                value = BitConverter.ToInt16(buffer, position);
            } else if (interpretationName.StartsWith("int8")) {
                value = ((sbyte) buffer[position]);
            }
            return value;
        }

        private float parseColor(byte[] buffer, string interpretationName, int position) {
            return (float) parseDouble(buffer, interpretationName, position) / 256;
        }  
    }
 }