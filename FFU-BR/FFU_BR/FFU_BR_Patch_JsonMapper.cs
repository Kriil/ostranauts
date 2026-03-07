using FFU_Beyond_Reach;
using MonoMod;
using Ostranauts.Utils.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LitJson {
    public partial class patch_JsonMapper : JsonMapper {
        [MonoModReplace] private static object ReadValue(Type inst_type, JsonReader reader) {
            reader.Read();
            if (bVerbose && reader.Value != null) {
                Debug.Log(reader.Value.ToString());
            }
            if (reader.Token == JsonToken.ArrayEnd) {
                return null;
            }
            Type underlyingType = Nullable.GetUnderlyingType(inst_type);
            Type type = underlyingType ?? inst_type;
            if (reader.Token == JsonToken.Null) {
                if (inst_type.IsClass || underlyingType != null) {
                    return null;
                }
                throw new JsonException($"Can't assign null to an instance of type {inst_type}");
            }
            if (reader.Token == JsonToken.Double || reader.Token == JsonToken.Int || reader.Token == JsonToken.Single || reader.Token == JsonToken.Long || reader.Token == JsonToken.String || reader.Token == JsonToken.Boolean) {
                Type type2 = reader.Value.GetType();
                if (type.IsAssignableFrom(type2)) {
                    return reader.Value;
                }
                if (custom_importers_table.TryGetValue(type2, out var value) && value.TryGetValue(type, out var value2)) {
                    return value2(reader.Value);
                }
                if (base_importers_table.TryGetValue(type2, out value) && value.TryGetValue(type, out value2)) {
                    return value2(reader.Value);
                }
                if (type.IsEnum) {
                    return Enum.ToObject(type, reader.Value);
                }
                MethodInfo convOp = GetConvOp(type, type2);
                if (convOp != null) {
                    return convOp.Invoke(null, [reader.Value]);
                }
                throw new JsonException($"Can't assign value '{reader.Value}' (type {type2}) to type {inst_type}");
            }
            object obj = null;
            if (reader.Token == JsonToken.ArrayStart) {
                AddArrayMetadata(inst_type);
                ArrayMetadata arrayMetadata = array_metadata[inst_type];
                if (!arrayMetadata.IsArray && !arrayMetadata.IsList) {
                    throw new JsonException($"Type {inst_type} can't act as an array");
                }
                IList list;
                Type elementType;
                if (!arrayMetadata.IsArray) {
                    list = (IList)Activator.CreateInstance(inst_type);
                    elementType = arrayMetadata.ElementType;
                } else {
                    list = new ArrayList();
                    elementType = inst_type.GetElementType();
                }
                while (true) {
                    object obj2 = ReadValue(elementType, reader);
                    if (obj2 == null && reader.Token == JsonToken.ArrayEnd) {
                        break;
                    }
                    list.Add(obj2);
                }
                if (arrayMetadata.IsArray) {
                    int count = list.Count;
                    obj = Array.CreateInstance(elementType, count);
                    for (int i = 0; i < count; i++) {
                        ((Array)obj).SetValue(list[i], i);
                    }
                } else {
                    obj = list;
                }
            } else if (reader.Token == JsonToken.ObjectStart) {
                AddObjectMetadata(type);
                ObjectMetadata objectMetadata = object_metadata[type];
                obj = Activator.CreateInstance(type);
                while (true) {
                    reader.Read();
                    if (reader.Token == JsonToken.ObjectEnd) {
                        break;
                    }
                    string text = (string)reader.Value;
                    if (objectMetadata.Properties.TryGetValue(text, out var value3)) {
                        if (value3.IsField) {
                            // Check if field is writable
                            FieldInfo fieldInfo = (FieldInfo)value3.Info;
                            if (!fieldInfo.IsLiteral) {
                                fieldInfo.SetValue(obj, ReadValue(value3.Type, reader));
                            } else {
                                ReadValue(value3.Type, reader);
                            }
                            // Switch to the next entry
                            continue;
                        }
                        PropertyInfo propertyInfo = (PropertyInfo)value3.Info;
                        if (propertyInfo.CanWrite) {
                            propertyInfo.SetValue(obj, ReadValue(value3.Type, reader), null);
                        } else {
                            ReadValue(value3.Type, reader);
                        }
                    } else if (!objectMetadata.IsDictionary) {
                        if (!reader.SkipNonMembers) {
                            throw new JsonException($"The type {inst_type} doesn't have the property '{text}'");
                        }
                        ReadSkip(reader);
                    } else {
                        ((IDictionary)obj).Add(text, ReadValue(objectMetadata.ElementType, reader));
                    }
                }
            }
            return obj;
        }
        [MonoModReplace] private static void WriteValue(object obj, JsonWriter writer, bool writer_is_private, int depth) {
            if (depth > max_nesting_depth) {
                throw new JsonException($"Max allowed object depth reached while trying to export from type {obj.GetType()}");
            }
            if (obj == null) {
                writer.Write(null);
                return;
            }
            if (obj is IJsonWrapper) {
                if (writer_is_private) {
                    writer.TextWriter.Write(((IJsonWrapper)obj).ToJson());
                } else {
                    ((IJsonWrapper)obj).ToJson(writer);
                }
                return;
            }
            if (obj is Vector2) {
                writer.Write((Vector2)obj);
                return;
            }
            if (obj is Vector3) {
                writer.Write((Vector3)obj);
                return;
            }
            if (obj is Vector4) {
                writer.Write((Vector4)obj);
                return;
            }
            if (obj is Quaternion) {
                writer.Write((Quaternion)obj);
                return;
            }
            if (obj is Matrix4x4) {
                writer.Write((Matrix4x4)obj);
                return;
            }
            if (obj is Ray) {
                writer.Write((Ray)obj);
                return;
            }
            if (obj is RaycastHit) {
                writer.Write((RaycastHit)obj);
                return;
            }
            if (obj is Color) {
                writer.Write((Color)obj);
                return;
            }
            if (obj is Point) {
                writer.WritePoint((Point)obj);
                return;
            }
            if (obj is string) {
                writer.Write((string)obj);
                return;
            }
            if (obj is float) {
                writer.Write((float)obj);
                return;
            }
            if (obj is double) {
                writer.Write((double)obj);
                return;
            }
            if (obj is int) {
                writer.Write((int)obj);
                return;
            }
            if (obj is bool) {
                writer.Write((bool)obj);
                return;
            }
            if (obj is long) {
                writer.Write((long)obj);
                return;
            }
            if (obj is Array) {
                writer.WriteArrayStart();
                foreach (object item in (Array)obj) {
                    WriteValue(item, writer, writer_is_private, depth + 1);
                }
                writer.WriteArrayEnd();
                return;
            }
            if (obj is IList) {
                writer.WriteArrayStart();
                foreach (object item2 in (IList)obj) {
                    WriteValue(item2, writer, writer_is_private, depth + 1);
                }
                writer.WriteArrayEnd();
                return;
            }
            if (obj is IDictionary) {
                writer.WriteObjectStart();
                foreach (DictionaryEntry item3 in (IDictionary)obj) {
                    writer.WritePropertyName((string)item3.Key);
                    WriteValue(item3.Value, writer, writer_is_private, depth + 1);
                }
                writer.WriteObjectEnd();
                return;
            }
            Type type = obj.GetType();
            if (custom_exporters_table.ContainsKey(type)) {
                ExporterFunc exporterFunc = custom_exporters_table[type];
                exporterFunc(obj, writer);
                return;
            }
            if (base_exporters_table.ContainsKey(type)) {
                ExporterFunc exporterFunc2 = base_exporters_table[type];
                exporterFunc2(obj, writer);
                return;
            }
            if (obj is Enum) {
                Type underlyingType = Enum.GetUnderlyingType(type);
                if (underlyingType == typeof(long) || underlyingType == typeof(uint) || underlyingType == typeof(ulong)) {
                    writer.Write((ulong)obj);
                } else {
                    writer.Write((int)obj);
                }
                return;
            }
            AddTypeProperties(type);
            IList<PropertyMetadata> list = type_properties[type];
            writer.WriteObjectStart();
            foreach (PropertyMetadata item4 in list) {
                if (item4.IsField) {
                    FieldInfo fieldInfo = (FieldInfo)item4.Info;
                    object value = fieldInfo.GetValue(obj);
                    if (value != null && (!(value is Array) || ((Array)value).Length != 0) && (!(value is IList) || ((IList)value).Count != 0) && (!(value is IDictionary) || ((IDictionary)value).Count != 0)) {
                        writer.WritePropertyName(item4.Info.Name);
                        WriteValue(value, writer, writer_is_private, depth + 1);
                    }
                } else {
                    PropertyInfo propertyInfo = (PropertyInfo)item4.Info;
                    object value2 = null;
                    try {
                        value2 = propertyInfo.GetValue(obj, null);
                    } catch {
                        if (FFU_BR_Defs.JsonLogging) {
                            string owner = propertyInfo.DeclaringType?.FullName ?? "<unknown_type>";
                            string prop = propertyInfo.Name ?? "<unnamed>";
                            string getter = propertyInfo.GetGetMethod()?.Name ?? "<no_getter>";
                            string root = obj?.GetType()?.FullName ?? "<root_unknown>";
                            Debug.Log($"#Parser# Failed to Write: {owner}.{prop} (getter: {getter}) while serializing {root}");
                        }
                        continue;
                    }
                    if (value2 != null && (!(value2 is Array) || ((Array)value2).Length != 0) && (!(value2 is IList) || ((IList)value2).Count != 0) && (!(value2 is IDictionary) || ((IDictionary)value2).Count != 0) && propertyInfo.CanRead) {
                        writer.WritePropertyName(item4.Info.Name);
                        WriteValue(value2, writer, writer_is_private, depth + 1);
                    }
                }
            }
            writer.WriteObjectEnd();
        }
    }
}

// Reference Output: ILSpy v9.1.0.7988 / C# 12.0 / 2022.8

/* LitJson.JsonMapper.ReadValue
private static object ReadValue(Type inst_type, JsonReader reader)
{
	reader.Read();
	if (bVerbose && reader.Value != null)
	{
		Debug.Log(reader.Value.ToString());
	}
	if (reader.Token == JsonToken.ArrayEnd)
	{
		return null;
	}
	Type underlyingType = Nullable.GetUnderlyingType(inst_type);
	Type type = underlyingType ?? inst_type;
	if (reader.Token == JsonToken.Null)
	{
		if (inst_type.IsClass || underlyingType != null)
		{
			return null;
		}
		throw new JsonException($"Can't assign null to an instance of type {inst_type}");
	}
	if (reader.Token == JsonToken.Double || reader.Token == JsonToken.Int || reader.Token == JsonToken.Single || reader.Token == JsonToken.Long || reader.Token == JsonToken.String || reader.Token == JsonToken.Boolean)
	{
		Type type2 = reader.Value.GetType();
		if (type.IsAssignableFrom(type2))
		{
			return reader.Value;
		}
		if (custom_importers_table.TryGetValue(type2, out var value) && value.TryGetValue(type, out var value2))
		{
			return value2(reader.Value);
		}
		if (base_importers_table.TryGetValue(type2, out value) && value.TryGetValue(type, out value2))
		{
			return value2(reader.Value);
		}
		if (type.IsEnum)
		{
			return Enum.ToObject(type, reader.Value);
		}
		MethodInfo convOp = GetConvOp(type, type2);
		if (convOp != null)
		{
			return convOp.Invoke(null, new object[1] { reader.Value });
		}
		throw new JsonException($"Can't assign value '{reader.Value}' (type {type2}) to type {inst_type}");
	}
	object obj = null;
	if (reader.Token == JsonToken.ArrayStart)
	{
		AddArrayMetadata(inst_type);
		ArrayMetadata arrayMetadata = array_metadata[inst_type];
		if (!arrayMetadata.IsArray && !arrayMetadata.IsList)
		{
			throw new JsonException($"Type {inst_type} can't act as an array");
		}
		IList list;
		Type elementType;
		if (!arrayMetadata.IsArray)
		{
			list = (IList)Activator.CreateInstance(inst_type);
			elementType = arrayMetadata.ElementType;
		}
		else
		{
			list = new ArrayList();
			elementType = inst_type.GetElementType();
		}
		while (true)
		{
			object obj2 = ReadValue(elementType, reader);
			if (obj2 == null && reader.Token == JsonToken.ArrayEnd)
			{
				break;
			}
			list.Add(obj2);
		}
		if (arrayMetadata.IsArray)
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
	else if (reader.Token == JsonToken.ObjectStart)
	{
		AddObjectMetadata(type);
		ObjectMetadata objectMetadata = object_metadata[type];
		obj = Activator.CreateInstance(type);
		while (true)
		{
			reader.Read();
			if (reader.Token == JsonToken.ObjectEnd)
			{
				break;
			}
			string text = (string)reader.Value;
			if (objectMetadata.Properties.TryGetValue(text, out var value3))
			{
				if (value3.IsField)
				{
					((FieldInfo)value3.Info).SetValue(obj, ReadValue(value3.Type, reader));
					continue;
				}
				PropertyInfo propertyInfo = (PropertyInfo)value3.Info;
				if (propertyInfo.CanWrite)
				{
					propertyInfo.SetValue(obj, ReadValue(value3.Type, reader), null);
				}
				else
				{
					ReadValue(value3.Type, reader);
				}
			}
			else if (!objectMetadata.IsDictionary)
			{
				if (!reader.SkipNonMembers)
				{
					throw new JsonException($"The type {inst_type} doesn't have the property '{text}'");
				}
				ReadSkip(reader);
			}
			else
			{
				((IDictionary)obj).Add(text, ReadValue(objectMetadata.ElementType, reader));
			}
		}
	}
	return obj;
}
*/

/* LitJson.JsonMapper.WriteValue
private static void WriteValue(object obj, JsonWriter writer, bool writer_is_private, int depth)
{
	if (depth > max_nesting_depth)
	{
		throw new JsonException($"Max allowed object depth reached while trying to export from type {obj.GetType()}");
	}
	if (obj == null)
	{
		writer.Write(null);
		return;
	}
	if (obj is IJsonWrapper)
	{
		if (writer_is_private)
		{
			writer.TextWriter.Write(((IJsonWrapper)obj).ToJson());
		}
		else
		{
			((IJsonWrapper)obj).ToJson(writer);
		}
		return;
	}
	if (obj is Vector2)
	{
		writer.Write((Vector2)obj);
		return;
	}
	if (obj is Vector3)
	{
		writer.Write((Vector3)obj);
		return;
	}
	if (obj is Vector4)
	{
		writer.Write((Vector4)obj);
		return;
	}
	if (obj is Quaternion)
	{
		writer.Write((Quaternion)obj);
		return;
	}
	if (obj is Matrix4x4)
	{
		writer.Write((Matrix4x4)obj);
		return;
	}
	if (obj is Ray)
	{
		writer.Write((Ray)obj);
		return;
	}
	if (obj is RaycastHit)
	{
		writer.Write((RaycastHit)obj);
		return;
	}
	if (obj is Color)
	{
		writer.Write((Color)obj);
		return;
	}
	if (obj is Point)
	{
		writer.WritePoint((Point)obj);
		return;
	}
	if (obj is string)
	{
		writer.Write((string)obj);
		return;
	}
	if (obj is float)
	{
		writer.Write((float)obj);
		return;
	}
	if (obj is double)
	{
		writer.Write((double)obj);
		return;
	}
	if (obj is int)
	{
		writer.Write((int)obj);
		return;
	}
	if (obj is bool)
	{
		writer.Write((bool)obj);
		return;
	}
	if (obj is long)
	{
		writer.Write((long)obj);
		return;
	}
	if (obj is Array)
	{
		writer.WriteArrayStart();
		foreach (object item in (Array)obj)
		{
			WriteValue(item, writer, writer_is_private, depth + 1);
		}
		writer.WriteArrayEnd();
		return;
	}
	if (obj is IList)
	{
		writer.WriteArrayStart();
		foreach (object item2 in (IList)obj)
		{
			WriteValue(item2, writer, writer_is_private, depth + 1);
		}
		writer.WriteArrayEnd();
		return;
	}
	if (obj is IDictionary)
	{
		writer.WriteObjectStart();
		foreach (DictionaryEntry item3 in (IDictionary)obj)
		{
			writer.WritePropertyName((string)item3.Key);
			WriteValue(item3.Value, writer, writer_is_private, depth + 1);
		}
		writer.WriteObjectEnd();
		return;
	}
	Type type = obj.GetType();
	if (custom_exporters_table.ContainsKey(type))
	{
		ExporterFunc exporterFunc = custom_exporters_table[type];
		exporterFunc(obj, writer);
		return;
	}
	if (base_exporters_table.ContainsKey(type))
	{
		ExporterFunc exporterFunc2 = base_exporters_table[type];
		exporterFunc2(obj, writer);
		return;
	}
	if (obj is Enum)
	{
		Type underlyingType = Enum.GetUnderlyingType(type);
		if (underlyingType == typeof(long) || underlyingType == typeof(uint) || underlyingType == typeof(ulong))
		{
			writer.Write((ulong)obj);
		}
		else
		{
			writer.Write((int)obj);
		}
		return;
	}
	AddTypeProperties(type);
	IList<PropertyMetadata> list = type_properties[type];
	writer.WriteObjectStart();
	foreach (PropertyMetadata item4 in list)
	{
		if (item4.IsField)
		{
			FieldInfo fieldInfo = (FieldInfo)item4.Info;
			object value = fieldInfo.GetValue(obj);
			if (value != null && (!(value is Array) || ((Array)value).Length != 0) && (!(value is IList) || ((IList)value).Count != 0) && (!(value is IDictionary) || ((IDictionary)value).Count != 0))
			{
				writer.WritePropertyName(item4.Info.Name);
				WriteValue(value, writer, writer_is_private, depth + 1);
			}
		}
		else
		{
			PropertyInfo propertyInfo = (PropertyInfo)item4.Info;
			object value2 = propertyInfo.GetValue(obj, null);
			if (value2 != null && (!(value2 is Array) || ((Array)value2).Length != 0) && (!(value2 is IList) || ((IList)value2).Count != 0) && (!(value2 is IDictionary) || ((IDictionary)value2).Count != 0) && propertyInfo.CanRead)
			{
				writer.WritePropertyName(item4.Info.Name);
				WriteValue(value2, writer, writer_is_private, depth + 1);
			}
		}
	}
	writer.WriteObjectEnd();
}
*/