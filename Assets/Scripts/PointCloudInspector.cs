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
using System.IO;
using UnityEngine;

public class PointCloudInspector : MonoBehaviour
{
	private void Start()
	{
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

        string path = "Assets/pdal/Examples/classification-ground.json";
        string json = File.ReadAllText(path);

        pdal.Pipeline pipeline = new pdal.Pipeline(json);

        long pointCount = pipeline.Execute();
        Debug.Log("Executed pipeline at " + path);
        Debug.Log("Point count: " + pointCount);
        Debug.Log("Log Level: " + pipeline.LogLevel);
        Debug.Log("Metadata: " + pipeline.Metadata);
        Debug.Log("Schema: " + pipeline.Schema);
        Debug.Log("Log: " + pipeline.Log);
        Debug.Log("Pipeline JSON: " + json);
        Debug.Log("Result JSON: " + pipeline.Json);

        pdal.PointViewIterator views = pipeline.Views;
        pdal.PointView view = views != null ? views.Next : null;

        while (view != null)
        {
            Debug.Log("View " + view.Id);
            Debug.Log("\tproj4: " + view.Proj4);
            Debug.Log("\tWKT: " + view.Wkt);
            Debug.Log("\tSize: " + view.Size + " points");
            Debug.Log("\tEmpty? " + view.Empty);

            pdal.PointLayout layout = view.Layout;
            Debug.Log("\tHas layout? " + (layout != null));

            if (layout != null)
            {
                Debug.Log("\tLayout - Point Size: " + layout.PointSize + " bytes");
                pdal.DimTypeList types = layout.Types;
                Debug.Log("\tLayout - Has dimension type list? " + (types != null));

                if (types != null)
                {
                    uint size = types.Size;
                    Debug.Log("\tLayout - Dimension type count: " + size + " dimensions");
                    Debug.Log("\tLayout - Point size calculated from dimension type list: " + types.ByteCount + " bytes");

                    Debug.Log("\tDimension Types (including value of first point in view)");
                    byte[] point = view.GetPackedPoint(types, 0);
                    int position = 0;

                    for (uint i = 0; i < size; ++i)
                    {
                        pdal.DimType type = types.at(i);
                        string interpretationName = type.InterpretationName;
                        int interpretationByteCount = type.InterpretationByteCount;
                        string value = "?";

                        if (interpretationName == "double")
                        {
                            value = BitConverter.ToDouble(point, position).ToString();
                        }
                        else if (interpretationName == "float")
                        {
                            value = BitConverter.ToSingle(point, position).ToString();
                        }
                        else if (interpretationName.StartsWith("uint64"))
                        {
                            value = BitConverter.ToUInt64(point, position).ToString();
                        }
                        else if (interpretationName.StartsWith("uint32"))
                        {
                            value = BitConverter.ToUInt32(point, position).ToString();
                        }
                        else if (interpretationName.StartsWith("uint16"))
                        {
                            value = BitConverter.ToUInt16(point, position).ToString();
                        }
                        else if (interpretationName.StartsWith("uint8"))
                        {
                            value = point[position].ToString();
                        }
                        else if (interpretationName.StartsWith("int64"))
                        {
                            value = BitConverter.ToInt64(point, position).ToString();
                        }
                        else if (interpretationName.StartsWith("int32"))
                        {
                            value = BitConverter.ToInt32(point, position).ToString();
                        }
                        else if (interpretationName.StartsWith("int16"))
                        {
                            value = BitConverter.ToInt16(point, position).ToString();
                        }
                        else if (interpretationName.StartsWith("int8"))
                        {
                            value = ((sbyte)point[position]).ToString();
                        }

                        Debug.Log("\t\tType " + type.Id + " [" + type.IdName 
                            + " (" + type.Interpretation + ":" + type.InterpretationName + " <" + type.InterpretationByteCount + " bytes>" 
                            + "), position " + position
                            + ", scale " + type.Scale
                            + ", offset " + type.Offset + "]: " + value);

                        position += interpretationByteCount;
                    }
                }

                types.Dispose();
            }

            view.Dispose();
            view = views.Next;
        }

        if (views != null)
        {
            views.Dispose();
        }
 
        pipeline.Dispose();
    }

}
