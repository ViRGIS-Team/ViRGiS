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
	public class Pipeline : IDisposable
	{
		private const string PDALC_LIBRARY = "pdalc";
		private const int BUFFER_SIZE = 1024;

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALCreatePipeline")]
		private static extern IntPtr create(string json);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALDisposePipeline")]
		private static extern void dispose(IntPtr pipeline);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetPipelineAsString")]
		private static extern int asString(IntPtr pipeline, [MarshalAs(UnmanagedType.LPStr)] StringBuilder buffer, uint size);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetPipelineMetadata")]
		private static extern int getMetadata(IntPtr pipeline, [MarshalAs(UnmanagedType.LPStr)] StringBuilder buffer, uint size);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetPipelineSchema")]
		private static extern int getSchema(IntPtr pipeline, [MarshalAs(UnmanagedType.LPStr)] StringBuilder buffer, uint size);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetPipelineLog")]
		private static extern int getLog(IntPtr pipeline, [MarshalAs(UnmanagedType.LPStr)] StringBuilder buffer, uint size);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALSetPipelineLogLevel")]
		private static extern void setLogLevel(IntPtr pipeline, int level);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetPipelineLogLevel")]
		private static extern int getLogLevel(IntPtr pipeline);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALExecutePipeline")]
		private static extern long execute(IntPtr pipeline);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALValidatePipeline")]
		private static extern bool validate(IntPtr pipeline);

		[DllImport(PDALC_LIBRARY, EntryPoint="PDALGetPointViews")]
		private static extern IntPtr getViews(IntPtr pipeline);

		/// The native C API PDAL pipeline pointer
		private IntPtr mNative = IntPtr.Zero;
		private PointViewIterator mViews = null;

		/**
		 * Creates an uninitialized and unexecuted pipeline.
		 * 
		 * @note Set the Json property to initialize the pipeline.
		 *       Once initialized, call the Execute method to execute the pipeline.
		 */
		public Pipeline()
		{
			// Do nothing
		}

		/**
		 * Creates a pipeline initialized with the provided JSON string.
		 
		 * @note Call the Execute method to execute the pipeline.
		 *
		 * @param json The JSON pipeline string
		 */
		public Pipeline(string json)
		{
			mNative = create(json);
		}

		/// Disposes the native PDAL pipeline object.
		public void Dispose()
		{
			dispose(mNative);
			mViews = null;
			mNative = IntPtr.Zero;
		}

		/// The JSON representation of the PDAL pipeline
		public string Json
		{
			get
			{
				StringBuilder buffer = new StringBuilder(BUFFER_SIZE);
				asString(mNative, buffer, (uint) buffer.Capacity);
				return buffer.ToString();
			}

			set
			{
				dispose(mNative);
				mNative = create(value);
			}
		}

		/// The pipeline's post-execution metadata
		public string Metadata
		{
			get
			{
				StringBuilder buffer = new StringBuilder(BUFFER_SIZE);
				getMetadata(mNative, buffer, (uint) buffer.Capacity);
				return buffer.ToString();
			}
		}

		/// The pipeline's post-execution schema
		public string Schema
		{
			get
			{
				StringBuilder buffer = new StringBuilder(BUFFER_SIZE);
				getSchema(mNative, buffer, (uint) buffer.Capacity);
				return buffer.ToString();
			}
		}

		/// The pipeline's execution log
		public string Log
		{
			get
			{
				StringBuilder buffer = new StringBuilder(BUFFER_SIZE);
				getLog(mNative, buffer, (uint) buffer.Capacity);
				return buffer.ToString();
			}
		}

		/// The pipeline's log level
		public int LogLevel
		{
			get { return getLogLevel(mNative); }
			set { setLogLevel(mNative, value); }
		}

		/// The pipeline's validation state
		public bool Valid
		{
			get { return validate(mNative); }
		}

		public PointViewIterator Views
		{
			get
			{
				if (mViews == null)
				{
					IntPtr nativeViews = getViews(mNative);

					if (nativeViews != IntPtr.Zero)
					{
						mViews = new PointViewIterator(nativeViews);
					}
				}

				return mViews;
			}
		}

		/**
		 * Executes the pipeline.
		 * 
		 * @return The total number of points produced by the pipeline
		 */
		public long Execute()
		{
			return execute(mNative);
		}
	}
 }
