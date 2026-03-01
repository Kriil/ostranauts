using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

// Runtime OBJ/MTL importer. This appears to load mesh data from strings or
// files, build Unity meshes/materials, and optionally fetch external assets.
[AddComponentMenu("ObjReader/ObjReader")]
public class ObjReader : MonoBehaviour
{
	// Singleton-style accessor used by callers that expect one persistent reader.
	public static ObjReader use
	{
		get
		{
			if (ObjReader._use == null)
			{
				ObjReader._use = (UnityEngine.Object.FindObjectOfType(typeof(ObjReader)) as ObjReader);
			}
			return ObjReader._use;
		}
	}

	// Unity setup: enforces a single reader instance and keeps it alive across
	// scenes when it is not parented under another object.
	private void Awake()
	{
		if (UnityEngine.Object.FindObjectsOfType(typeof(ObjReader)).Length > 1)
		{
			UnityEngine.Object.Destroy(this);
			return;
		}
		if (base.transform.parent == null)
		{
			UnityEngine.Object.DontDestroyOnLoad(this);
		}
		base.transform.position = Vector3.zero;
		base.transform.rotation = Quaternion.identity;
		base.transform.localScale = Vector3.one;
	}

	// Simplest string import path for OBJ data without an MTL file.
	public GameObject[] ConvertString(string objString)
	{
		string empty = string.Empty;
		return this.Convert(ref objString, ref empty, null, null, string.Empty, false, string.Empty);
	}

	public GameObject[] ConvertString(string objString, string mtlString)
	{
		return this.Convert(ref objString, ref mtlString, null, null, string.Empty, true, string.Empty);
	}

	public GameObject[] ConvertString(string objString, Material standardMaterial)
	{
		string empty = string.Empty;
		return this.Convert(ref objString, ref empty, standardMaterial, null, string.Empty, false, string.Empty);
	}

	public GameObject[] ConvertString(string objString, string mtlString, Material standardMaterial)
	{
		return this.Convert(ref objString, ref mtlString, standardMaterial, null, string.Empty, true, string.Empty);
	}

	public GameObject[] ConvertString(string objString, string mtlString, Material standardMaterial, Material transparentMaterial)
	{
		return this.Convert(ref objString, ref mtlString, standardMaterial, transparentMaterial, string.Empty, true, string.Empty);
	}

	// Simplest file import path for OBJ files, optionally paired with MTL data.
	public GameObject[] ConvertFile(string objFilePath, bool useMtl)
	{
		return this.ConvertFile(objFilePath, useMtl, null, null, null);
	}

	public GameObject[] ConvertFile(string objFilePath, bool useMtl, Material standardMaterial)
	{
		return this.ConvertFile(objFilePath, useMtl, standardMaterial, null, null);
	}

	public GameObject[] ConvertFile(string objFilePath, bool useMtl, Material standardMaterial, Material transparentMaterial)
	{
		return this.ConvertFile(objFilePath, useMtl, standardMaterial, transparentMaterial, null);
	}

	// Async-style wrapper: kicks off a file import and returns a mutable ObjData
	// record that can be filled by the coroutine path.
	public ObjReader.ObjData ConvertFileAsync(string objFilePath, bool useMtl)
	{
		return this.ConvertFileAsync(objFilePath, useMtl, null, null);
	}

	public ObjReader.ObjData ConvertFileAsync(string objFilePath, bool useMtl, Material standardMaterial)
	{
		return this.ConvertFileAsync(objFilePath, useMtl, standardMaterial, null);
	}

	public ObjReader.ObjData ConvertFileAsync(string objFilePath, bool useMtl, Material standardMaterial, Material transparentMaterial)
	{
		ObjReader.ObjData objData = new ObjReader.ObjData();
		this.ConvertFile(objFilePath, useMtl, standardMaterial, transparentMaterial, objData);
		return objData;
	}

	// Shared file import path for both synchronous local files and coroutine-
	// driven remote/file URLs.
	private GameObject[] ConvertFile(string objFilePath, bool useMtl, Material standardMaterial, Material transparentMaterial, ObjReader.ObjData objData)
	{
		objFilePath = objFilePath.Replace('\\', '/');
		string text = string.Empty;
		if (objData != null)
		{
			if (objFilePath.StartsWith("http://") || objFilePath.StartsWith("https://") || objFilePath.StartsWith("file://") || objFilePath.StartsWith("ftp://"))
			{
				base.StartCoroutine(this.GetWWWFiles(objFilePath, useMtl, standardMaterial, transparentMaterial, objData));
			}
			else
			{
				Debug.LogError("File path must start with http://, https://, ftp://, or file://");
			}
			return null;
		}
		if (!File.Exists(objFilePath))
		{
			Debug.LogError("File not found: " + objFilePath);
			return null;
		}
		text = File.ReadAllText(objFilePath);
		if (text == null)
		{
			Debug.LogError("File not usable: " + objFilePath);
			return null;
		}
		string text2 = string.Empty;
		string empty = string.Empty;
		if (useMtl)
		{
			string mtlfileName = this.GetMTLFileName(ref objFilePath, ref text, ref empty);
			if (File.Exists(empty + mtlfileName))
			{
				text2 = File.ReadAllText(empty + mtlfileName);
			}
			else
			{
				Debug.LogWarning("MTL file not found: " + empty + mtlfileName);
			}
		}
		return this.Convert(ref text, ref text2, standardMaterial, transparentMaterial, empty, useMtl, Path.GetFileNameWithoutExtension(objFilePath));
	}

	// Parses MTL data when present, then forwards the OBJ contents into mesh
	// object creation.
	private GameObject[] Convert(ref string objFile, ref string mtlFile, Material standardMaterial, Material transparentMaterial, string filePath, bool useMtl, string fileName)
	{
		Dictionary<string, Material> dictionary = null;
		if (useMtl && mtlFile != string.Empty)
		{
			string[] lines;
			Dictionary<string, Texture2D> textures = this.GetTextures(ref mtlFile, out lines, filePath);
			dictionary = this.ParseMTL(lines, standardMaterial, transparentMaterial, filePath, textures);
			if (dictionary == null)
			{
				useMtl = false;
			}
		}
		return this.CreateObjects(ref objFile, useMtl, dictionary, standardMaterial, null, fileName);
	}

	// Main OBJ parser. Scans vertices, UVs, normals, groups, and faces, then
	// builds one or more Unity GameObjects from the parsed mesh data.
	private GameObject[] CreateObjects(ref string objFile, bool useMtl, Dictionary<string, Material> materials, Material standardMaterial, ObjReader.ObjData objData, string fileName)
	{
		string[] array = objFile.Split(new char[]
		{
			'\n'
		});
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		bool flag = false;
		bool flag2 = false;
		int item = 0;
		int num4 = 0;
		List<int> list = new List<int>();
		int num5 = 0;
		this.maxPoints = Mathf.Clamp(this.maxPoints, 0, 65534);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].Length >= 2)
			{
				char c = char.ToLower(array[i][0]);
				char c2 = char.ToLower(array[i][1]);
				if (c == 'f' && c2 == ' ')
				{
					if (!flag)
					{
						item = num5;
					}
					num5++;
					if (flag2 && !flag)
					{
						num4++;
						list.Add(item);
						flag2 = false;
					}
					flag = true;
				}
				else if ((c == 'o' && c2 == ' ') || (c == 'g' && c2 == ' ') || (c == 'u' && c2 == 's'))
				{
					flag2 = true;
					flag = false;
				}
				else if (c == 'v' && c2 == ' ')
				{
					num++;
				}
				else if (c == 'v' && c2 == 't')
				{
					num2++;
				}
				else if (this.useSuppliedNormals && c == 'v' && c2 == 'n')
				{
					num3++;
				}
			}
		}
		if (num4 == 0)
		{
			list.Add(item);
			num4 = 1;
		}
		if (num == 0)
		{
			Debug.LogError("No vertices found in file");
			return null;
		}
		if (num5 == 0)
		{
			Debug.LogError("No face data found in file");
			return null;
		}
		list.Add(-1);
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		Vector3[] array2 = new Vector3[num];
		Vector2[] array3 = new Vector2[num2];
		Vector3[] array4 = new Vector3[num3];
		List<string> list2 = new List<string>();
		bool flag3 = false;
		bool flag4 = false;
		string[] array5 = new string[num4];
		string[] array6 = new string[num4];
		int j = 0;
		string[] array7 = new string[0];
		int[] array8 = new int[num4 + 1];
		num5 = 0;
		num4 = 0;
		int num9 = 0;
		int num10 = 0;
		try
		{
			while (j < array.Length)
			{
				string text = array[j++];
				if (text.Length >= 3 && text[0] != '#')
				{
					this.CleanLine(ref text);
					if (text.Length >= 3)
					{
						while (text[text.Length - 1] == '\\' && j < array.Length)
						{
							text = text.Substring(0, text.Length - 1) + " " + array[j++].TrimEnd(new char[0]);
							this.CleanLine(ref text);
						}
						char c3 = char.ToLower(text[0]);
						char c4 = char.ToLower(text[1]);
						if (c3 == 'u' && c4 == 's')
						{
							if (useMtl && text.StartsWith("usemtl") && num9++ == 0)
							{
								array7 = text.Split(new char[]
								{
									' '
								});
								if (array7.Length > 1)
								{
									array6[num4] = array7[1];
									if (num10++ == 0)
									{
										if (this.useFileNameAsObjectName && fileName != string.Empty)
										{
											array5[num4] = fileName;
										}
										else
										{
											array5[num4] = array7[1];
										}
									}
								}
							}
						}
						else if (((c3 == 'o' && c4 == ' ') || (c3 == 'g' && c4 == ' ')) && num10++ == 0)
						{
							if (this.useFileNameAsObjectName && fileName != string.Empty)
							{
								array5[num4] = fileName;
							}
							else
							{
								array5[num4] = text.Substring(2, text.Length - 2);
							}
						}
						else if (c3 == 'v' && c4 == ' ')
						{
							array7 = text.Split(new char[]
							{
								' '
							});
							if (array7.Length != 4)
							{
								throw new Exception("Incorrect number of points while trying to read vertices:\n" + text + "\n");
							}
							array2[num6++] = new Vector3(-float.Parse(array7[1], CultureInfo.InvariantCulture), float.Parse(array7[2], CultureInfo.InvariantCulture), float.Parse(array7[3], CultureInfo.InvariantCulture));
						}
						else if (c3 == 'v' && c4 == 't')
						{
							array7 = text.Split(new char[]
							{
								' '
							});
							if (array7.Length > 4 || array7.Length < 3)
							{
								throw new Exception("Incorrect number of points while trying to read UV data:\n" + text + "\n");
							}
							array3[num7++] = new Vector2(float.Parse(array7[1], CultureInfo.InvariantCulture), float.Parse(array7[2], CultureInfo.InvariantCulture));
						}
						else if (this.useSuppliedNormals && c3 == 'v' && c4 == 'n')
						{
							array7 = text.Split(new char[]
							{
								' '
							});
							if (array7.Length != 4)
							{
								throw new Exception("Incorrect number of points while trying to read normals:\n" + text + "\n");
							}
							array4[num8++] = new Vector3(-float.Parse(array7[1], CultureInfo.InvariantCulture), float.Parse(array7[2], CultureInfo.InvariantCulture), float.Parse(array7[3], CultureInfo.InvariantCulture));
						}
						else if (c3 == 'f' && c4 == ' ')
						{
							array7 = text.Split(new char[]
							{
								' '
							});
							if (array7.Length >= 4 && array7.Length <= 5)
							{
								if (array7[1].Substring(0, 1) == "-")
								{
									for (int k = 1; k < array7.Length; k++)
									{
										string[] array9 = array7[k].Split(new char[]
										{
											'/'
										});
										array9[0] = (num6 - -int.Parse(array9[0]) + 1).ToString();
										if (array9.Length > 1)
										{
											if (array9[1] != string.Empty)
											{
												array9[1] = (num7 - -int.Parse(array9[1]) + 1).ToString();
											}
											if (array9.Length == 3)
											{
												array9[2] = (num8 - -int.Parse(array9[2]) + 1).ToString();
											}
										}
										array7[k] = string.Join("/", array9);
									}
								}
								for (int l = 1; l < 4; l++)
								{
									list2.Add(array7[l]);
								}
								if (array7.Length == 5)
								{
									flag3 = true;
									list2.Add(array7[1]);
									list2.Add(array7[3]);
									list2.Add(array7[4]);
								}
							}
							else
							{
								flag4 = true;
							}
							if (++num5 == list[num4 + 1])
							{
								array8[++num4] = list2.Count;
								num9 = 0;
								num10 = 0;
							}
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError(ex.Message);
			return null;
		}
		int num11;
		if (this.combineMultipleGroups && !this.useSubmeshesWhenCombining)
		{
			num11 = 1;
			array8[1] = list2.Count;
		}
		else
		{
			array8[num4 + 1] = list2.Count;
			num11 = array8.Length - 1;
		}
		int[] array10 = new int[list2.Count];
		int[] array11 = new int[list2.Count];
		int[] array12 = new int[list2.Count];
		int num12 = 3;
		for (int m = 0; m < list2.Count; m++)
		{
			string text2 = list2[m];
			array7 = text2.Split(new char[]
			{
				'/'
			});
			array10[m] = int.Parse(array7[0]) - 1;
			if (array7.Length > 1)
			{
				if (array7[1] != string.Empty)
				{
					array11[m] = int.Parse(array7[1]) - 1;
				}
				if (array7.Length == num12 && this.useSuppliedNormals)
				{
					array12[m] = int.Parse(array7[2]) - 1;
				}
			}
		}
		List<Vector3> list3 = new List<Vector3>(array2);
		if (num2 > 0)
		{
			this.SplitOnUVs(list2, array10, array11, list3, array3, array2, ref num6);
		}
		if (flag3 && !this.suppressWarnings)
		{
			Debug.LogWarning("At least one object uses quads...automatic triangle conversion is being used, which may not produce best results");
		}
		if (flag4 && !this.suppressWarnings)
		{
			Debug.LogWarning("Polygons which are not quads or triangles have been skipped");
		}
		if (num2 == 0 && !this.suppressWarnings)
		{
			Debug.LogWarning("At least one object does not seem to be UV mapped...any textures used will appear as a solid color");
		}
		if (num3 == 0 && !this.suppressWarnings)
		{
			Debug.LogWarning("No normal data found for at least one object...automatically computing normals instead");
		}
		if (num == 0 && list2.Count == 0)
		{
			Debug.LogError("No objects seem to be present...possibly the .obj file is damaged or could not be read");
			return null;
		}
		if (num == 0)
		{
			Debug.LogError("The .obj file does not contain any vertices");
			return null;
		}
		if (list2.Count == 0)
		{
			Debug.LogError("The .obj file does not contain any polygons");
			return null;
		}
		GameObject[] array13 = new GameObject[(!this.combineMultipleGroups) ? num11 : 1];
		for (int n = 0; n < array13.Length; n++)
		{
			array13[n] = new GameObject(array5[n], new Type[]
			{
				typeof(MeshFilter),
				typeof(MeshRenderer)
			});
		}
		GameObject gameObject = null;
		Mesh mesh = null;
		Vector3[] array14 = null;
		Vector2[] array15 = null;
		Vector3[] array16 = null;
		int[] array17 = null;
		bool flag5 = this.combineMultipleGroups && this.useSubmeshesWhenCombining && num11 > 1;
		Material[] array18 = null;
		if (flag5)
		{
			array18 = new Material[num11];
		}
		int num13 = 0;
		bool flag6 = false;
		for (int num14 = 0; num14 < num11; num14++)
		{
			if (!flag5 || (flag5 && num14 == 0))
			{
				gameObject = array13[num14];
				mesh = new Mesh();
				Dictionary<int, int> dictionary = new Dictionary<int, int>();
				List<Vector3> list4 = new List<Vector3>();
				int num15 = 0;
				int num16 = 0;
				int num17 = array8[num14];
				int num18 = array8[num14 + 1];
				if (flag5)
				{
					num17 = array8[0];
					num18 = array8[num11];
				}
				for (int num19 = num17; num19 < num18; num19++)
				{
					if (!dictionary.TryGetValue(array10[num19], out num16))
					{
						dictionary[array10[num19]] = num15++;
						list4.Add(list3[array10[num19]]);
					}
				}
				if (list4.Count > this.maxPoints)
				{
					Debug.LogError(string.Concat(new object[]
					{
						"The number of vertices in the object ",
						array5[num14],
						" exceeds the maximum allowable limit of ",
						this.maxPoints
					}));
					return null;
				}
				array14 = new Vector3[list4.Count];
				array15 = new Vector2[list4.Count];
				array16 = new Vector3[list4.Count];
				array17 = new int[num18 - num17];
				if (this.scaleFactor == Vector3.one && this.objRotation == Vector3.zero && this.objPosition == Vector3.zero)
				{
					for (int num20 = 0; num20 < list4.Count; num20++)
					{
						array14[num20] = list4[num20];
					}
				}
				else
				{
					base.transform.eulerAngles = this.objRotation;
					base.transform.position = this.objPosition;
					base.transform.localScale = this.scaleFactor;
					Matrix4x4 localToWorldMatrix = base.transform.localToWorldMatrix;
					for (int num21 = 0; num21 < list4.Count; num21++)
					{
						array14[num21] = localToWorldMatrix.MultiplyPoint3x4(list4[num21]);
					}
					base.transform.position = Vector3.zero;
					base.transform.rotation = Quaternion.identity;
					base.transform.localScale = Vector3.one;
				}
				if (num7 > 0 && num8 > 0 && this.useSuppliedNormals)
				{
					for (int num22 = num17; num22 < num18; num22++)
					{
						array15[dictionary[array10[num22]]] = array3[array11[num22]];
						array16[dictionary[array10[num22]]] = array4[array12[num22]].normalized;
					}
				}
				else
				{
					if (num7 > 0)
					{
						for (int num23 = num17; num23 < num18; num23++)
						{
							array15[dictionary[array10[num23]]] = array3[array11[num23]];
						}
					}
					if (num8 > 0 && this.useSuppliedNormals)
					{
						for (int num24 = num17; num24 < num18; num24++)
						{
							array16[dictionary[array10[num24]]] = array4[array12[num24]];
						}
					}
				}
				num15 = 0;
				for (int num25 = num17; num25 < num18; num25 += 3)
				{
					array17[num15] = dictionary[array10[num25]];
					array17[num15 + 1] = dictionary[array10[num25 + 2]];
					array17[num15 + 2] = dictionary[array10[num25 + 1]];
					num15 += 3;
				}
				mesh.vertices = array14;
				mesh.uv = array15;
				if (this.autoCenterOnOrigin)
				{
					Vector3 center = mesh.bounds.center;
					int num26 = array14.Length;
					for (int num27 = 0; num27 < num26; num27++)
					{
						array14[num27] -= center;
					}
					mesh.vertices = array14;
				}
				if (this.useSuppliedNormals)
				{
					mesh.normals = array16;
				}
				if (flag5)
				{
					mesh.subMeshCount = num11;
				}
			}
			if (flag5)
			{
				int num28 = array8[num14 + 1] - array8[num14];
				int[] array19 = new int[num28];
				Array.Copy(array17, array8[num14], array19, 0, num28);
				mesh.SetTriangles(array19, num14);
				if (array6[num14] != null)
				{
					if (useMtl && materials.ContainsKey(array6[num14]))
					{
						array18[num14] = materials[array6[num14]];
						if (materials[array6[num14]])
						{
						}
						flag6 = true;
						num13 = num14;
					}
				}
				else if (flag6)
				{
					array18[num14] = materials[array6[num13]];
				}
				else
				{
					array18[num14] = standardMaterial;
				}
			}
			else
			{
				mesh.triangles = array17;
			}
			if (!flag5 || (flag5 && num14 == num11 - 1))
			{
				if (num8 == 0 || !this.useSuppliedNormals)
				{
					mesh.RecalculateNormals();
					if (this.computeTangents)
					{
						array16 = mesh.normals;
					}
				}
				if (this.computeTangents)
				{
					Vector4[] tangents = new Vector4[array14.Length];
					this.CalculateTangents(array14, array16, array15, array17, tangents);
					mesh.tangents = tangents;
				}
				mesh.RecalculateBounds();
				gameObject.GetComponent<MeshFilter>().mesh = mesh;
				if (!flag5)
				{
					if (array6[num14] != null)
					{
						if (useMtl && materials.ContainsKey(array6[num14]))
						{
							gameObject.GetComponent<Renderer>().material = materials[array6[num14]];
							flag6 = true;
							num13 = num14;
						}
					}
					else if (flag6)
					{
						gameObject.GetComponent<Renderer>().material = materials[array6[num13]];
					}
					else
					{
						gameObject.GetComponent<Renderer>().material = standardMaterial;
					}
				}
				else
				{
					gameObject.GetComponent<Renderer>().materials = array18;
				}
			}
		}
		if (objData != null)
		{
			objData.SetDone();
			objData.gameObjects = array13;
			return null;
		}
		return array13;
	}

	private void CleanLine(ref string line)
	{
		while (line.IndexOf("  ") != -1)
		{
			line = line.Replace("  ", " ");
		}
		line = line.Trim();
	}

	private void CalculateTangents(Vector3[] vertices, Vector3[] normals, Vector2[] uv, int[] triangles, Vector4[] tangents)
	{
		Vector3[] array = new Vector3[vertices.Length];
		Vector3[] array2 = new Vector3[vertices.Length];
		int num = triangles.Length;
		int num2 = tangents.Length;
		for (int i = 0; i < num; i += 3)
		{
			int num3 = triangles[i];
			int num4 = triangles[i + 1];
			int num5 = triangles[i + 2];
			Vector3 vector = vertices[num3];
			Vector3 vector2 = vertices[num4];
			Vector3 vector3 = vertices[num5];
			Vector2 vector4 = uv[num3];
			Vector2 vector5 = uv[num4];
			Vector2 vector6 = uv[num5];
			float num6 = vector2.x - vector.x;
			float num7 = vector3.x - vector.x;
			float num8 = vector2.y - vector.y;
			float num9 = vector3.y - vector.y;
			float num10 = vector2.z - vector.z;
			float num11 = vector3.z - vector.z;
			float num12 = vector5.x - vector4.x;
			float num13 = vector6.x - vector4.x;
			float num14 = vector5.y - vector4.y;
			float num15 = vector6.y - vector4.y;
			float num16 = num12 * num15 - num13 * num14;
			float num17 = (num16 != 0f) ? (1f / num16) : 0f;
			Vector3 b;
			b.x = (num15 * num6 - num14 * num7) * num17;
			b.y = (num15 * num8 - num14 * num9) * num17;
			b.z = (num15 * num10 - num14 * num11) * num17;
			Vector3 b2;
			b2.x = (num12 * num7 - num13 * num6) * num17;
			b2.y = (num12 * num9 - num13 * num8) * num17;
			b2.z = (num12 * num11 - num13 * num10) * num17;
			array[num3] += b;
			array[num4] += b;
			array[num5] += b;
			array2[num3] += b2;
			array2[num4] += b2;
			array2[num5] += b2;
		}
		for (int j = 0; j < num2; j++)
		{
			Vector3 vector7 = normals[j];
			Vector3 vector8 = array[j];
			Vector3 normalized = (vector8 - vector7 * Vector3.Dot(vector7, vector8)).normalized;
			tangents[j] = new Vector4(normalized.x, normalized.y, normalized.z);
			tangents[j].w = ((Vector3.Dot(Vector3.Cross(vector7, vector8), array2[j]) >= 0f) ? 1f : -1f);
		}
	}

	private void SplitOnUVs(List<string> triData, int[] triVerts, int[] triUVs, List<Vector3> objVertList, Vector2[] objUVs, Vector3[] objVertices, ref int verticesCount)
	{
		Dictionary<int, Vector2> dictionary = new Dictionary<int, Vector2>();
		Dictionary<ObjReader.Int2, int> dictionary2 = new Dictionary<ObjReader.Int2, int>();
		ObjReader.Int2 key = new ObjReader.Int2();
		Vector2 zero = Vector2.zero;
		int num = 0;
		for (int i = 0; i < triData.Count; i++)
		{
			if (!dictionary.TryGetValue(triVerts[i], out zero))
			{
				dictionary[triVerts[i]] = objUVs[triUVs[i]];
			}
			else if (dictionary[triVerts[i]] != objUVs[triUVs[i]])
			{
				key = new ObjReader.Int2(triVerts[i], triUVs[i]);
				if (dictionary2.TryGetValue(key, out num))
				{
					triVerts[i] = num;
				}
				else
				{
					objVertList.Add(objVertices[triVerts[i]]);
					triVerts[i] = verticesCount++;
					dictionary[triVerts[i]] = objUVs[triUVs[i]];
					dictionary2[key] = triUVs[i];
				}
			}
		}
	}

	private string GetMTLFileName(ref string objFilePath, ref string objFile, ref string filePath)
	{
		filePath = objFilePath.Substring(0, objFilePath.LastIndexOf("/") + 1);
		string result = string.Empty;
		int num = objFile.IndexOf("mtllib");
		if (num != -1)
		{
			int num2 = objFile.IndexOf('\n', num);
			if (num2 != -1)
			{
				result = this.GetFileName(objFile.Substring(num, num2 - num), "mtllib");
			}
		}
		return result;
	}

	private string GetFileName(string line, string token)
	{
		string result = string.Empty;
		if (line.Length > token.Length + 2)
		{
			int num = token.Length + 1;
			result = line.Substring(num, line.Length - num).Replace("\r", string.Empty);
		}
		return result;
	}

	private bool IsTextureLine(string line)
	{
		return line.StartsWith("map_Kd") || line.StartsWith("map_bump") || line.StartsWith("bump");
	}

	private Dictionary<string, Texture2D> GetTextures(ref string mtlFile, out string[] lines, string filePath)
	{
		lines = new string[0];
		Dictionary<string, Texture2D> dictionary = new Dictionary<string, Texture2D>();
		lines = mtlFile.Split(new char[]
		{
			'\n'
		});
		for (int i = 0; i < lines.Length; i++)
		{
			string text = lines[i];
			this.CleanLine(ref text);
			lines[i] = text;
			if (text.Length >= 3 && text[0] != '#')
			{
				if (this.IsTextureLine(text) && filePath != string.Empty)
				{
					string fileName = this.GetFileName(text, this.GetToken(text));
					if (fileName != string.Empty)
					{
						string text2 = filePath + fileName;
						if (!File.Exists(text2))
						{
							throw new Exception("Texture file not found: " + text2);
						}
						Texture2D texture2D = new Texture2D(4, 4);
						texture2D.name = "OBJReader " + text2;
						texture2D.LoadImage(File.ReadAllBytes(text2));
						if (text.StartsWith("map_bump") || text.StartsWith("bump"))
						{
							this.ConvertToNormalmap(texture2D);
						}
						dictionary[fileName] = texture2D;
					}
				}
			}
		}
		return dictionary;
	}

	private string GetToken(string line)
	{
		string result = "map_Kd";
		if (line.StartsWith("map_bump"))
		{
			result = "map_bump";
		}
		else if (line.StartsWith("bump"))
		{
			result = "bump";
		}
		return result;
	}

	private void ConvertToNormalmap(Texture2D tex)
	{
		Color32[] pixels = tex.GetPixels32();
		for (int i = 0; i < pixels.Length; i++)
		{
			pixels[i].a = pixels[i].r;
			pixels[i].r = pixels[i].g;
			pixels[i].b = pixels[i].g;
		}
		tex.SetPixels32(pixels);
		tex.Apply();
	}

	private IEnumerator GetWWWFiles(string objFilePath, bool useMtl, Material standardMaterial, Material transparentMaterial, ObjReader.ObjData objData)
	{
		WWW www = new WWW(objFilePath);
		while (!www.isDone)
		{
			objData.SetProgress(www.progress * ((!useMtl) ? 1f : 0.5f));
			if (objData.cancel)
			{
				yield break;
			}
			yield return null;
		}
		if (www.error != null)
		{
			Debug.LogError("Error loading " + objFilePath + ": " + www.error);
			objData.SetDone();
			yield break;
		}
		string objFile = www.text;
		string filePath = string.Empty;
		Dictionary<string, Material> materials = null;
		if (useMtl)
		{
			string mtlFileName = this.GetMTLFileName(ref objFilePath, ref objFile, ref filePath);
			if (mtlFileName != string.Empty)
			{
				www = new WWW(filePath + mtlFileName);
				while (!www.isDone)
				{
					if (objData.cancel)
					{
						yield break;
					}
					yield return null;
				}
				if (www.error != null)
				{
					if (!this.useMTLFallback)
					{
						Debug.LogError(string.Concat(new string[]
						{
							"Error loading ",
							filePath,
							mtlFileName,
							": ",
							www.error
						}));
						objData.SetDone();
						yield break;
					}
					useMtl = false;
				}
				if (useMtl && www.text != string.Empty)
				{
					ObjReader.LinesRef linesRef = new ObjReader.LinesRef();
					Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
					ObjReader.BoolRef loadError = new ObjReader.BoolRef(false);
					yield return base.StartCoroutine(this.GetTexturesAsync(www.text, linesRef, filePath, textures, objData, loadError));
					if (loadError.b)
					{
						yield break;
					}
					materials = this.ParseMTL(linesRef.lines, standardMaterial, transparentMaterial, filePath, textures);
					if (materials == null)
					{
						useMtl = false;
					}
				}
			}
			else
			{
				useMtl = false;
			}
		}
		this.CreateObjects(ref objFile, useMtl, materials, standardMaterial, objData, Path.GetFileNameWithoutExtension(objFilePath));
		yield break;
	}

	private IEnumerator GetTexturesAsync(string mtlFile, ObjReader.LinesRef linesRef, string filePath, Dictionary<string, Texture2D> textures, ObjReader.ObjData objData, ObjReader.BoolRef loadError)
	{
		Texture2D diffuseTexture = null;
		string[] lines = mtlFile.Split(new char[]
		{
			'\n'
		});
		int numberOfTextures = 0;
		for (int j = 0; j < lines.Length; j++)
		{
			string text = lines[j];
			this.CleanLine(ref text);
			lines[j] = text;
			if (text.Length >= 7 && text[0] != '#')
			{
				if (this.IsTextureLine(text) && filePath != string.Empty)
				{
					numberOfTextures++;
				}
			}
		}
		float progress = 0.5f;
		for (int i = 0; i < lines.Length; i++)
		{
			if (lines[i].Length >= 7 && lines[i][0] != '#')
			{
				if (this.IsTextureLine(lines[i]) && filePath != string.Empty)
				{
					string textureFilePath = this.GetFileName(lines[i], this.GetToken(lines[i]));
					if (textureFilePath != string.Empty)
					{
						string completeFilePath = filePath + textureFilePath;
						WWW www = new WWW(completeFilePath);
						while (!www.isDone)
						{
							objData.SetProgress(progress + www.progress / (float)numberOfTextures * 0.5f);
							if (objData.cancel)
							{
								loadError.b = true;
								yield break;
							}
							yield return null;
						}
						if (www.error != null)
						{
							Debug.LogError("Error loading " + completeFilePath + ": " + www.error);
							loadError.b = true;
							objData.SetDone();
							yield break;
						}
						progress += 1f / (float)numberOfTextures * 0.5f;
						diffuseTexture = new Texture2D(4, 4);
						diffuseTexture.name = "OBJReader " + completeFilePath;
						www.LoadImageIntoTexture(diffuseTexture);
						if (lines[i].StartsWith("map_bump") || lines[i].StartsWith("bump"))
						{
							this.ConvertToNormalmap(diffuseTexture);
						}
						textures[textureFilePath] = diffuseTexture;
					}
				}
			}
		}
		linesRef.lines = lines;
		yield break;
	}

	private Dictionary<string, Material> ParseMTL(string[] lines, Material standardMaterial, Material transparentMaterial, string filePath, Dictionary<string, Texture2D> textures)
	{
		Dictionary<string, Material> dictionary = new Dictionary<string, Material>();
		try
		{
			string mtlName = string.Empty;
			float aR = 0f;
			float aG = 0f;
			float aB = 0f;
			float dR = 0f;
			float dG = 0f;
			float dB = 0f;
			float sR = 0f;
			float sG = 0f;
			float sB = 0f;
			float transparency = 1f;
			float specularHighlight = 0f;
			int num = 0;
			Texture2D diffuseTexture = null;
			Texture2D bumpTexture = null;
			foreach (string text in lines)
			{
				if (text.Length >= 3 && text[0] != '#')
				{
					if (text.StartsWith("newmtl"))
					{
						if (num++ > 0)
						{
							this.SetMaterial(dictionary, mtlName, aR, aG, aB, dR, dG, dB, sR, sG, sB, standardMaterial, transparentMaterial, transparency, specularHighlight, diffuseTexture, bumpTexture);
							aR = 0f;
							aG = 0f;
							aB = 0f;
							dR = 0f;
							dG = 0f;
							dB = 0f;
							sR = 0f;
							sG = 0f;
							sB = 0f;
							transparency = 1f;
							specularHighlight = 0f;
						}
						diffuseTexture = null;
						string[] array = text.Split(new char[]
						{
							' '
						});
						if (array.Length > 1)
						{
							mtlName = array[1];
						}
					}
					else if (text.StartsWith("map_Kd") && filePath != string.Empty)
					{
						string fileName = this.GetFileName(text, "map_Kd");
						if (fileName != string.Empty && textures.ContainsKey(fileName))
						{
							diffuseTexture = textures[fileName];
						}
					}
					else if ((text.StartsWith("map_bump") || text.StartsWith("bump")) && filePath != string.Empty)
					{
						string fileName2 = this.GetFileName(text, (!text.StartsWith("map_bump")) ? "bump" : "map_bump");
						if (fileName2 != string.Empty && textures.ContainsKey(fileName2))
						{
							bumpTexture = textures[fileName2];
						}
					}
					else
					{
						string a = text.Substring(0, 2).ToLower();
						if (a == "ka")
						{
							this.ParseKLine(ref text, ref aR, ref aG, ref aB);
						}
						else if (a == "kd")
						{
							this.ParseKLine(ref text, ref dR, ref dG, ref dB);
						}
						else if (a == "ks")
						{
							this.ParseKLine(ref text, ref sR, ref sG, ref sB);
						}
						else if (a == "d " || a == "tr")
						{
							string[] array2 = text.Split(new char[]
							{
								' '
							});
							if (array2.Length > 1)
							{
								if (array2[1] == "-halo")
								{
									if (array2.Length > 2)
									{
										transparency = float.Parse(array2[2], CultureInfo.InvariantCulture);
									}
								}
								else
								{
									transparency = float.Parse(array2[1], CultureInfo.InvariantCulture);
								}
							}
						}
						else if (a == "ns")
						{
							string[] array3 = text.Split(new char[]
							{
								' '
							});
							if (array3.Length > 1)
							{
								specularHighlight = float.Parse(array3[1], CultureInfo.InvariantCulture);
							}
						}
					}
				}
			}
			this.SetMaterial(dictionary, mtlName, aR, aG, aB, dR, dG, dB, sR, sG, sB, standardMaterial, transparentMaterial, transparency, specularHighlight, diffuseTexture, bumpTexture);
		}
		catch (Exception ex)
		{
			Debug.LogError(ex.Message);
			return null;
		}
		return dictionary;
	}

	private void SetMaterial(Dictionary<string, Material> mtlDictionary, string mtlName, float aR, float aG, float aB, float dR, float dG, float dB, float sR, float sG, float sB, Material standardMaterial, Material transparentMaterial, float transparency, float specularHighlight, Texture2D diffuseTexture, Texture2D bumpTexture)
	{
		Material material;
		if (transparency == 1f)
		{
			if (standardMaterial == null)
			{
				material = new Material(Shader.Find("VertexLit"));
			}
			else
			{
				material = UnityEngine.Object.Instantiate<Material>(standardMaterial);
			}
		}
		else if (transparentMaterial == null)
		{
			material = new Material(Shader.Find("Transparent/VertexLit"));
		}
		else
		{
			material = UnityEngine.Object.Instantiate<Material>(transparentMaterial);
		}
		if (material.HasProperty("_Emission") && !this.overrideAmbient)
		{
			material.SetColor("_Emission", new Color(aR, aG, aB, 1f));
		}
		if (material.HasProperty("_Color") && !this.overrideDiffuse)
		{
			material.SetColor("_Color", new Color(dR, dG, dB, transparency));
		}
		if (material.HasProperty("_SpecColor") && !this.overrideSpecular)
		{
			material.SetColor("_SpecColor", new Color(sR, sG, sB, 1f));
		}
		if (material.HasProperty("_Shininess"))
		{
			material.SetFloat("_Shininess", specularHighlight / 1000f);
		}
		if (material.HasProperty("_MainTex"))
		{
			material.mainTexture = diffuseTexture;
		}
		if (material.HasProperty("_BumpMap"))
		{
			material.SetTexture("_BumpMap", bumpTexture);
		}
		material.name = mtlName;
		mtlDictionary[mtlName] = material;
	}

	private void ParseKLine(ref string line, ref float r, ref float g, ref float b)
	{
		if (line.Contains(".rfl") && !this.suppressWarnings)
		{
			Debug.LogWarning(".rfl files not supported");
			return;
		}
		if (line.Contains("xyz") && !this.suppressWarnings)
		{
			Debug.LogWarning("CIEXYZ color not supported");
			return;
		}
		try
		{
			string[] array = line.Split(new char[]
			{
				' '
			});
			if (array.Length > 1)
			{
				r = float.Parse(array[1], CultureInfo.InvariantCulture);
			}
			if (array.Length > 3)
			{
				g = float.Parse(array[2], CultureInfo.InvariantCulture);
				b = float.Parse(array[3], CultureInfo.InvariantCulture);
			}
			else
			{
				g = r;
				b = r;
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("Incorrect number format when parsing MTL file: " + ex.Message);
		}
	}

	public int maxPoints = 30000;

	public bool combineMultipleGroups = true;

	public bool useSubmeshesWhenCombining = true;

	public bool useFileNameAsObjectName;

	public bool computeTangents;

	public bool useSuppliedNormals;

	public bool overrideDiffuse;

	public bool overrideSpecular;

	public bool overrideAmbient;

	public bool suppressWarnings;

	public bool useMTLFallback;

	public bool autoCenterOnOrigin;

	public Vector3 scaleFactor = new Vector3(1f, 1f, 1f);

	public Vector3 objRotation = new Vector3(0f, 0f, 0f);

	public Vector3 objPosition = new Vector3(0f, 0f, 0f);

	private static ObjReader _use;

	public class Int2
	{
		public Int2()
		{
			this.a = 0;
			this.b = 0;
		}

		public Int2(int a, int b)
		{
			this.a = a;
			this.b = b;
		}

		public int a;

		public int b;
	}

	public class ObjData
	{
		public bool isDone
		{
			get
			{
				return this._isDone;
			}
		}

		public float progress
		{
			get
			{
				return this._progress;
			}
		}

		public bool cancel
		{
			get
			{
				return this._cancel;
			}
		}

		public void SetDone()
		{
			this._isDone = true;
		}

		public void SetProgress(float p)
		{
			this._progress = p;
		}

		public void Cancel()
		{
			this._cancel = true;
		}

		private bool _isDone;

		private float _progress;

		public GameObject[] gameObjects;

		private bool _cancel;
	}

	public class LinesRef
	{
		public string[] lines;
	}

	public class BoolRef
	{
		public BoolRef(bool b)
		{
			this.b = b;
		}

		public bool b;
	}
}
