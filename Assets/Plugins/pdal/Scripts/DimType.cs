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
	public class DimType
	{
		private const string PDALC_LIBRARY = "pdalc";
		private const int BUFFER_SIZE = 1024;

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct NativeDimType
		{
			public UInt32 id;
			public UInt32 interpretation;
			public double scale;
			public double offset;
		};

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetInvalidDimType")]
		[return:MarshalAs(UnmanagedType.Struct)]
		private static extern NativeDimType getInvalidDimType();

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetDimTypeIdName")]
		private static extern int getIdName([MarshalAs(UnmanagedType.Struct)] NativeDimType type, [MarshalAs(UnmanagedType.LPStr)] StringBuilder buffer, uint size);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetDimTypeInterpretationName")]
		private static extern int getInterpretationName([MarshalAs(UnmanagedType.Struct)] NativeDimType type, [MarshalAs(UnmanagedType.LPStr)] StringBuilder buffer, uint size);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetDimTypeInterpretationByteCount")]
		private static extern int getInterpretationByteCount([MarshalAs(UnmanagedType.Struct)] NativeDimType type);

		private NativeDimType mType = getInvalidDimType();

		public DimType()
		{
			// Do nothing
		}

		public DimType(NativeDimType nativeType)
		{
			mType = nativeType;
		}

		public DimType(uint id, uint interpretation, double scale = 1.0, double offset = 0.0)
		{
			mType.id = id;
			mType.interpretation = interpretation;
			mType.scale = scale;
			mType.offset = offset;
		}

		public uint Id
		{
			get { return mType.id; }
			set { mType.id = value; }
		}

		public string IdName
		{
			get
			{
				StringBuilder buffer = new StringBuilder(256);
				getIdName(mType, buffer, (uint) buffer.Capacity);
				return buffer.ToString();
			}
		}

		public uint Interpretation
		{
			get { return mType.interpretation; }
			set { mType.interpretation = value; }
		}

		public string InterpretationName
		{
			get
			{
				StringBuilder buffer = new StringBuilder(256);
				getInterpretationName(mType, buffer, (uint) buffer.Capacity);
				return buffer.ToString();
			}
		}

		public int InterpretationByteCount
		{
			get { return getInterpretationByteCount(mType); }
		}

        public double Scale
        {
            get { return mType.scale; }
        }

        public double Offset
        {
            get { return mType.offset; }
        }
    }
 }
