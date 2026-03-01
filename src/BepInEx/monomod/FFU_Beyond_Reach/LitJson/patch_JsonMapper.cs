using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using FFU_Beyond_Reach;
using MonoMod;
using Ostranauts.Utils.Models;
using UnityEngine;

namespace LitJson
{
	public class patch_JsonMapper : JsonMapper
	{
		[MonoModReplace]
		private static object ReadValue(Type inst_type, JsonReader reader)
		{
			reader.Read();
			bool flag = JsonMapper.bVerbose && reader.Value != null;
			if (flag)
			{
				Debug.Log(reader.Value.ToString());
			}
			bool flag2 = reader.Token == 5;
			object result;
			if (flag2)
			{
				result = null;
			}
			else
			{
				Type underlyingType = Nullable.GetUnderlyingType(inst_type);
				Type type = underlyingType ?? inst_type;
				bool flag3 = reader.Token == 12;
				if (flag3)
				{
					bool flag4 = inst_type.IsClass || underlyingType != null;
					if (!flag4)
					{
						throw new JsonException(string.Format("Can't assign null to an instance of type {0}", inst_type));
					}
					result = null;
				}
				else
				{
					bool flag5 = reader.Token == 8 || reader.Token == 6 || reader.Token == 9 || reader.Token == 7 || reader.Token == 10 || reader.Token == 11;
					if (flag5)
					{
						Type type2 = reader.Value.GetType();
						bool flag6 = type.IsAssignableFrom(type2);
						if (flag6)
						{
							result = reader.Value;
						}
						else
						{
							IDictionary<Type, ImporterFunc> dictionary;
							ImporterFunc importerFunc;
							bool flag7 = JsonMapper.custom_importers_table.TryGetValue(type2, out dictionary) && dictionary.TryGetValue(type, out importerFunc);
							if (flag7)
							{
								result = importerFunc.Invoke(reader.Value);
							}
							else
							{
								bool flag8 = JsonMapper.base_importers_table.TryGetValue(type2, out dictionary) && dictionary.TryGetValue(type, out importerFunc);
								if (flag8)
								{
									result = importerFunc.Invoke(reader.Value);
								}
								else
								{
									bool isEnum = type.IsEnum;
									if (isEnum)
									{
										result = Enum.ToObject(type, reader.Value);
									}
									else
									{
										MethodInfo convOp = JsonMapper.GetConvOp(type, type2);
										bool flag9 = convOp != null;
										if (!flag9)
										{
											throw new JsonException(string.Format("Can't assign value '{0}' (type {1}) to type {2}", reader.Value, type2, inst_type));
										}
										result = convOp.Invoke(null, new object[]
										{
											reader.Value
										});
									}
								}
							}
						}
					}
					else
					{
						object obj = null;
						bool flag10 = reader.Token == 4;
						if (flag10)
						{
							JsonMapper.AddArrayMetadata(inst_type);
							ArrayMetadata arrayMetadata = JsonMapper.array_metadata[inst_type];
							bool flag11 = !arrayMetadata.IsArray && !arrayMetadata.IsList;
							if (flag11)
							{
								throw new JsonException(string.Format("Type {0} can't act as an array", inst_type));
							}
							bool flag12 = !arrayMetadata.IsArray;
							IList list;
							Type elementType;
							if (flag12)
							{
								list = (IList)Activator.CreateInstance(inst_type);
								elementType = arrayMetadata.ElementType;
							}
							else
							{
								list = new ArrayList();
								elementType = inst_type.GetElementType();
							}
							for (;;)
							{
								object obj2 = patch_JsonMapper.ReadValue(elementType, reader);
								bool flag13 = obj2 == null && reader.Token == 5;
								if (flag13)
								{
									break;
								}
								list.Add(obj2);
							}
							bool isArray = arrayMetadata.IsArray;
							if (isArray)
							{
								int count = list.Count;
								obj = Array.CreateInstance(elementType, count);
								for (int i = 0; i < count; i++)
								{
									((Array)obj).SetValue(list[i], i);
								}
							}
							else
							{
								obj = list;
							}
						}
						else
						{
							bool flag14 = reader.Token == 1;
							if (flag14)
							{
								JsonMapper.AddObjectMetadata(type);
								ObjectMetadata objectMetadata = JsonMapper.object_metadata[type];
								obj = Activator.CreateInstance(type);
								string text;
								for (;;)
								{
									reader.Read();
									bool flag15 = reader.Token == 3;
									if (flag15)
									{
										break;
									}
									text = (string)reader.Value;
									PropertyMetadata propertyMetadata;
									bool flag16 = objectMetadata.Properties.TryGetValue(text, out propertyMetadata);
									if (flag16)
									{
										bool isField = propertyMetadata.IsField;
										if (isField)
										{
											FieldInfo fieldInfo = (FieldInfo)propertyMetadata.Info;
											bool flag17 = !fieldInfo.IsLiteral;
											if (flag17)
											{
												fieldInfo.SetValue(obj, patch_JsonMapper.ReadValue(propertyMetadata.Type, reader));
											}
											else
											{
												patch_JsonMapper.ReadValue(propertyMetadata.Type, reader);
											}
										}
										else
										{
											PropertyInfo propertyInfo = (PropertyInfo)propertyMetadata.Info;
											bool canWrite = propertyInfo.CanWrite;
											if (canWrite)
											{
												propertyInfo.SetValue(obj, patch_JsonMapper.ReadValue(propertyMetadata.Type, reader), null);
											}
											else
											{
												patch_JsonMapper.ReadValue(propertyMetadata.Type, reader);
											}
										}
									}
									else
									{
										bool flag18 = !objectMetadata.IsDictionary;
										if (flag18)
										{
											bool flag19 = !reader.SkipNonMembers;
											if (flag19)
											{
												goto Block_36;
											}
											JsonMapper.ReadSkip(reader);
										}
										else
										{
											((IDictionary)obj).Add(text, patch_JsonMapper.ReadValue(objectMetadata.ElementType, reader));
										}
									}
								}
								goto IL_48B;
								Block_36:
								throw new JsonException(string.Format("The type {0} doesn't have the property '{1}'", inst_type, text));
							}
						}
						IL_48B:
						result = obj;
					}
				}
			}
			return result;
		}
		[MonoModReplace]
		private static void WriteValue(object obj, JsonWriter writer, bool writer_is_private, int depth)
		{
			bool flag = depth > JsonMapper.max_nesting_depth;
			if (flag)
			{
				throw new JsonException(string.Format("Max allowed object depth reached while trying to export from type {0}", obj.GetType()));
			}
			bool flag2 = obj == null;
			if (flag2)
			{
				writer.Write(null);
			}
			else
			{
				bool flag3 = obj is IJsonWrapper;
				if (flag3)
				{
					if (writer_is_private)
					{
						writer.TextWriter.Write(((IJsonWrapper)obj).ToJson());
					}
					else
					{
						((IJsonWrapper)obj).ToJson(writer);
					}
				}
				else
				{
					bool flag4 = obj is Vector2;
					if (flag4)
					{
						writer.Write((Vector2)obj);
					}
					else
					{
						bool flag5 = obj is Vector3;
						if (flag5)
						{
							writer.Write((Vector3)obj);
						}
						else
						{
							bool flag6 = obj is Vector4;
							if (flag6)
							{
								writer.Write((Vector4)obj);
							}
							else
							{
								bool flag7 = obj is Quaternion;
								if (flag7)
								{
									writer.Write((Quaternion)obj);
								}
								else
								{
									bool flag8 = obj is Matrix4x4;
									if (flag8)
									{
										writer.Write((Matrix4x4)obj);
									}
									else
									{
										bool flag9 = obj is Ray;
										if (flag9)
										{
											writer.Write((Ray)obj);
										}
										else
										{
											bool flag10 = obj is RaycastHit;
											if (flag10)
											{
												writer.Write((RaycastHit)obj);
											}
											else
											{
												bool flag11 = obj is Color;
												if (flag11)
												{
													writer.Write((Color)obj);
												}
												else
												{
													bool flag12 = obj is Point;
													if (flag12)
													{
														writer.WritePoint((Point)obj);
													}
													else
													{
														bool flag13 = obj is string;
														if (flag13)
														{
															writer.Write((string)obj);
														}
														else
														{
															bool flag14 = obj is float;
															if (flag14)
															{
																writer.Write((float)obj);
															}
															else
															{
																bool flag15 = obj is double;
																if (flag15)
																{
																	writer.Write((double)obj);
																}
																else
																{
																	bool flag16 = obj is int;
																	if (flag16)
																	{
																		writer.Write((int)obj);
																	}
																	else
																	{
																		bool flag17 = obj is bool;
																		if (flag17)
																		{
																			writer.Write((bool)obj);
																		}
																		else
																		{
																			bool flag18 = obj is long;
																			if (flag18)
																			{
																				writer.Write((long)obj);
																			}
																			else
																			{
																				bool flag19 = obj is Array;
																				if (flag19)
																				{
																					writer.WriteArrayStart();
																					foreach (object obj2 in ((Array)obj))
																					{
																						patch_JsonMapper.WriteValue(obj2, writer, writer_is_private, depth + 1);
																					}
																					writer.WriteArrayEnd();
																				}
																				else
																				{
																					bool flag20 = obj is IList;
																					if (flag20)
																					{
																						writer.WriteArrayStart();
																						foreach (object obj3 in ((IList)obj))
																						{
																							patch_JsonMapper.WriteValue(obj3, writer, writer_is_private, depth + 1);
																						}
																						writer.WriteArrayEnd();
																					}
																					else
																					{
																						bool flag21 = obj is IDictionary;
																						if (flag21)
																						{
																							writer.WriteObjectStart();
																							foreach (object obj4 in ((IDictionary)obj))
																							{
																								DictionaryEntry dictionaryEntry = (DictionaryEntry)obj4;
																								writer.WritePropertyName((string)dictionaryEntry.Key);
																								patch_JsonMapper.WriteValue(dictionaryEntry.Value, writer, writer_is_private, depth + 1);
																							}
																							writer.WriteObjectEnd();
																						}
																						else
																						{
																							Type type = obj.GetType();
																							bool flag22 = JsonMapper.custom_exporters_table.ContainsKey(type);
																							if (flag22)
																							{
																								ExporterFunc exporterFunc = JsonMapper.custom_exporters_table[type];
																								exporterFunc.Invoke(obj, writer);
																							}
																							else
																							{
																								bool flag23 = JsonMapper.base_exporters_table.ContainsKey(type);
																								if (flag23)
																								{
																									ExporterFunc exporterFunc2 = JsonMapper.base_exporters_table[type];
																									exporterFunc2.Invoke(obj, writer);
																								}
																								else
																								{
																									bool flag24 = obj is Enum;
																									if (flag24)
																									{
																										Type underlyingType = Enum.GetUnderlyingType(type);
																										bool flag25 = underlyingType == typeof(long) || underlyingType == typeof(uint) || underlyingType == typeof(ulong);
																										if (flag25)
																										{
																											writer.Write((ulong)obj);
																										}
																										else
																										{
																											writer.Write((int)obj);
																										}
																									}
																									else
																									{
																										JsonMapper.AddTypeProperties(type);
																										IList<PropertyMetadata> list = JsonMapper.type_properties[type];
																										writer.WriteObjectStart();
																										foreach (PropertyMetadata propertyMetadata in list)
																										{
																											bool isField = propertyMetadata.IsField;
																											if (isField)
																											{
																												FieldInfo fieldInfo = (FieldInfo)propertyMetadata.Info;
																												object value = fieldInfo.GetValue(obj);
																												bool flag26 = value != null && (!(value is Array) || ((Array)value).Length != 0) && (!(value is IList) || ((IList)value).Count != 0) && (!(value is IDictionary) || ((IDictionary)value).Count != 0);
																												if (flag26)
																												{
																													writer.WritePropertyName(propertyMetadata.Info.Name);
																													patch_JsonMapper.WriteValue(value, writer, writer_is_private, depth + 1);
																												}
																											}
																											else
																											{
																												PropertyInfo propertyInfo = (PropertyInfo)propertyMetadata.Info;
																												object obj5 = null;
																												try
																												{
																													obj5 = propertyInfo.GetValue(obj, null);
																												}
																												catch
																												{
																													bool jsonLogging = FFU_BR_Defs.JsonLogging;
																													if (jsonLogging)
																													{
																														Type declaringType = propertyInfo.DeclaringType;
																														string text = ((declaringType != null) ? declaringType.FullName : null) ?? "<unknown_type>";
																														string text2 = propertyInfo.Name ?? "<unnamed>";
																														MethodInfo getMethod = propertyInfo.GetGetMethod();
																														string text3 = ((getMethod != null) ? getMethod.Name : null) ?? "<no_getter>";
																														string text4;
																														if (obj == null)
																														{
																															text4 = null;
																														}
																														else
																														{
																															Type type2 = obj.GetType();
																															text4 = ((type2 != null) ? type2.FullName : null);
																														}
																														string text5 = text4 ?? "<root_unknown>";
																														Debug.Log(string.Concat(new string[]
																														{
																															"#Parser# Failed to Write: ",
																															text,
																															".",
																															text2,
																															" (getter: ",
																															text3,
																															") while serializing ",
																															text5
																														}));
																													}
																													continue;
																												}
																												bool flag27 = obj5 != null && (!(obj5 is Array) || ((Array)obj5).Length != 0) && (!(obj5 is IList) || ((IList)obj5).Count != 0) && (!(obj5 is IDictionary) || ((IDictionary)obj5).Count != 0) && propertyInfo.CanRead;
																												if (flag27)
																												{
																													writer.WritePropertyName(propertyMetadata.Info.Name);
																													patch_JsonMapper.WriteValue(obj5, writer, writer_is_private, depth + 1);
																												}
																											}
																										}
																										writer.WriteObjectEnd();
																									}
																								}
																							}
																						}
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}
}
