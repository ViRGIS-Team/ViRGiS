using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Virgis
{


	/// <summary>
	/// Imports a .ply file as a ParticleData Object
	/// 
	/// Adapted from https://github.com/leon196/PointCloudExporter/blob/master/Assets/Scripts/SimpleImporter.cs
	/// </summary>
	public class PlyImport
	{

		public async Task<ParticleData> Load(string filePath, int maximumVertex = 10000000)
		{
			ParticleData data = new ParticleData();
			int levelOfDetails = 1;
			try
			{
				using (BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
				{
					int cursor = 0;
					int length = (int)reader.BaseStream.Length;
					string lineText = "";
					bool header = true;
					int vertexCount = 0;
					int colorDataCount = 3;
					int index = 0;
					int step = 0;
					int normalDataCount = 0;
					while (cursor + step < length)
					{
						if (header)
						{
							char v = reader.ReadChar();
							if (v == '\n')
							{
								if (lineText.Contains("end_header"))
								{
									header = false;
								}
								else if (lineText.Contains("element vertex"))
								{
									string[] array = lineText.Split(' ');
									if (array.Length > 0)
									{
										int subtractor = array.Length - 2;
										vertexCount = Convert.ToInt32(array[array.Length - subtractor]);
										if (vertexCount > maximumVertex)
										{
											levelOfDetails = 1 + (int)Mathf.Floor(vertexCount / maximumVertex);
											vertexCount = maximumVertex;
										}
										data.vertexCount = vertexCount;
										data.vertices = new List<Vector3>();
										data.normals = new List<Vector3>();
										data.colors = new List<Color32>();
									}
								}
								else if (lineText.Contains("property uchar alpha"))
								{
									colorDataCount = 4;
								}
								else if (lineText.Contains("property float n"))
								{
									normalDataCount += 1;
								}
								lineText = "";
							}
							else
							{
								lineText += v;
							}
							step = sizeof(char);
							cursor += step;
						}
						else
						{
							if (index < vertexCount)
							{

								data.vertices.Add(new Vector3(-reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
								if (normalDataCount == 3)
								{
									data.normals.Add(new Vector3(-reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()));
								}
								else
								{
									data.normals.Add(Vector3.one);
								}
								data.colors.Add(new Color32(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), 255));

								step = sizeof(float) * 6 * levelOfDetails + sizeof(byte) * colorDataCount * levelOfDetails;
								cursor += step;
								if (colorDataCount > 3)
								{
									reader.ReadByte();
								}

								if (levelOfDetails > 1)
								{
									for (int l = 1; l < levelOfDetails; ++l)
									{
										for (int f = 0; f < 3 + normalDataCount; ++f)
										{
											reader.ReadSingle();
										}
										for (int b = 0; b < colorDataCount; ++b)
										{
											reader.ReadByte();
										}
									}
								}
								++index;
							}
						}
					}
				}
			}
			catch (Exception e) when (
		 e is UnauthorizedAccessException ||
		 e is DirectoryNotFoundException ||
		 e is FileNotFoundException ||
		 e is NotSupportedException
		 )
			{
				Debug.LogError("Failed to Load" + filePath + " : " + e.ToString());
				return null;
			}
			return data;
		}
	}
}

