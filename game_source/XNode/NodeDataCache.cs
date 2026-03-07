using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Serialization;

namespace XNode
{
	public static class NodeDataCache
	{
		private static bool Initialized
		{
			get
			{
				return NodeDataCache.portDataCache != null;
			}
		}

		public static void UpdatePorts(Node node, Dictionary<string, NodePort> ports)
		{
			if (!NodeDataCache.Initialized)
			{
				NodeDataCache.BuildCache();
			}
			Dictionary<string, NodePort> dictionary = new Dictionary<string, NodePort>();
			Dictionary<string, List<NodePort>> dictionary2 = new Dictionary<string, List<NodePort>>();
			Type type = node.GetType();
			Dictionary<string, string> dictionary3 = null;
			if (NodeDataCache.formerlySerializedAsCache != null)
			{
				NodeDataCache.formerlySerializedAsCache.TryGetValue(type, out dictionary3);
			}
			List<NodePort> list = new List<NodePort>();
			List<NodePort> list2;
			if (NodeDataCache.portDataCache.TryGetValue(type, out list2))
			{
				for (int i = 0; i < list2.Count; i++)
				{
					dictionary.Add(list2[i].fieldName, NodeDataCache.portDataCache[type][i]);
				}
			}
			foreach (NodePort nodePort in ports.Values.ToList<NodePort>())
			{
				NodePort nodePort2;
				if (dictionary.TryGetValue(nodePort.fieldName, out nodePort2))
				{
					if (nodePort.IsDynamic || nodePort.direction != nodePort2.direction || nodePort.connectionType != nodePort2.connectionType || nodePort.typeConstraint != nodePort2.typeConstraint)
					{
						if (!nodePort.IsDynamic && nodePort.direction == nodePort2.direction)
						{
							dictionary2.Add(nodePort.fieldName, nodePort.GetConnections());
						}
						nodePort.ClearConnections();
						ports.Remove(nodePort.fieldName);
					}
					else
					{
						nodePort.ValueType = nodePort2.ValueType;
					}
				}
				else if (nodePort.IsStatic)
				{
					string key = null;
					if (dictionary3 != null && dictionary3.TryGetValue(nodePort.fieldName, out key))
					{
						dictionary2.Add(key, nodePort.GetConnections());
					}
					nodePort.ClearConnections();
					ports.Remove(nodePort.fieldName);
				}
				else if (NodeDataCache.IsDynamicListPort(nodePort))
				{
					list.Add(nodePort);
				}
			}
			foreach (NodePort nodePort3 in dictionary.Values)
			{
				if (!ports.ContainsKey(nodePort3.fieldName))
				{
					NodePort nodePort4 = new NodePort(nodePort3, node);
					List<NodePort> list3;
					if (dictionary2.TryGetValue(nodePort3.fieldName, out list3))
					{
						for (int j = 0; j < list3.Count; j++)
						{
							NodePort nodePort5 = list3[j];
							if (nodePort5 != null)
							{
								if (nodePort4.CanConnectTo(nodePort5))
								{
									nodePort4.Connect(nodePort5);
								}
							}
						}
					}
					ports.Add(nodePort3.fieldName, nodePort4);
				}
			}
			foreach (NodePort nodePort6 in list)
			{
				string key2 = nodePort6.fieldName.Split(new char[]
				{
					' '
				})[0];
				NodePort nodePort7 = dictionary[key2];
				nodePort6.ValueType = NodeDataCache.GetBackingValueType(nodePort7.ValueType);
				nodePort6.direction = nodePort7.direction;
				nodePort6.connectionType = nodePort7.connectionType;
				nodePort6.typeConstraint = nodePort7.typeConstraint;
			}
		}

		private static Type GetBackingValueType(Type portValType)
		{
			if (portValType.HasElementType)
			{
				return portValType.GetElementType();
			}
			if (portValType.IsGenericType && portValType.GetGenericTypeDefinition() == typeof(List<>))
			{
				return portValType.GetGenericArguments()[0];
			}
			return portValType;
		}

		private static bool IsDynamicListPort(NodePort port)
		{
			string[] array = port.fieldName.Split(new char[]
			{
				' '
			});
			if (array.Length != 2)
			{
				return false;
			}
			FieldInfo field = port.node.GetType().GetField(array[0]);
			if (field == null)
			{
				return false;
			}
			object[] customAttributes = field.GetCustomAttributes(true);
			return customAttributes.Any(delegate(object x)
			{
				Node.InputAttribute inputAttribute = x as Node.InputAttribute;
				Node.OutputAttribute outputAttribute = x as Node.OutputAttribute;
				return (inputAttribute != null && inputAttribute.dynamicPortList) || (outputAttribute != null && outputAttribute.dynamicPortList);
			});
		}

		private static void BuildCache()
		{
			NodeDataCache.portDataCache = new NodeDataCache.PortDataCache();
			Type baseType = typeof(Node);
			List<Type> list = new List<Type>();
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			Assembly[] array = assemblies;
			int i = 0;
			while (i < array.Length)
			{
				Assembly assembly = array[i];
				string text = assembly.GetName().Name;
				int num = text.IndexOf('.');
				if (num != -1)
				{
					text = text.Substring(0, num);
				}
				if (text == null)
				{
					goto IL_D4;
				}
				if (!(text == "UnityEditor") && !(text == "UnityEngine") && !(text == "System") && !(text == "mscorlib") && !(text == "Microsoft"))
				{
					goto IL_D4;
				}
				IL_FB:
				i++;
				continue;
				IL_D4:
				list.AddRange((from t in assembly.GetTypes()
				where !t.IsAbstract && baseType.IsAssignableFrom(t)
				select t).ToArray<Type>());
				goto IL_FB;
			}
			for (int j = 0; j < list.Count; j++)
			{
				NodeDataCache.CachePorts(list[j]);
			}
		}

		public static List<FieldInfo> GetNodeFields(Type nodeType)
		{
			List<FieldInfo> list = new List<FieldInfo>(nodeType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
			Type type = nodeType;
			while ((type = type.BaseType) != typeof(Node))
			{
				FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
				for (int i = 0; i < fields.Length; i++)
				{
					FieldInfo parentField = fields[i];
					if (list.TrueForAll((FieldInfo x) => x.Name != parentField.Name))
					{
						list.Add(parentField);
					}
				}
			}
			return list;
		}

		private static void CachePorts(Type nodeType)
		{
			List<FieldInfo> nodeFields = NodeDataCache.GetNodeFields(nodeType);
			for (int i = 0; i < nodeFields.Count; i++)
			{
				object[] customAttributes = nodeFields[i].GetCustomAttributes(true);
				Node.InputAttribute inputAttribute = customAttributes.FirstOrDefault((object x) => x is Node.InputAttribute) as Node.InputAttribute;
				Node.OutputAttribute outputAttribute = customAttributes.FirstOrDefault((object x) => x is Node.OutputAttribute) as Node.OutputAttribute;
				FormerlySerializedAsAttribute formerlySerializedAsAttribute = customAttributes.FirstOrDefault((object x) => x is FormerlySerializedAsAttribute) as FormerlySerializedAsAttribute;
				if (inputAttribute != null || outputAttribute != null)
				{
					if (inputAttribute != null && outputAttribute != null)
					{
						Debug.LogError(string.Concat(new string[]
						{
							"Field ",
							nodeFields[i].Name,
							" of type ",
							nodeType.FullName,
							" cannot be both input and output."
						}));
					}
					else
					{
						if (!NodeDataCache.portDataCache.ContainsKey(nodeType))
						{
							NodeDataCache.portDataCache.Add(nodeType, new List<NodePort>());
						}
						NodeDataCache.portDataCache[nodeType].Add(new NodePort(nodeFields[i]));
					}
					if (formerlySerializedAsAttribute != null)
					{
						if (NodeDataCache.formerlySerializedAsCache == null)
						{
							NodeDataCache.formerlySerializedAsCache = new Dictionary<Type, Dictionary<string, string>>();
						}
						if (!NodeDataCache.formerlySerializedAsCache.ContainsKey(nodeType))
						{
							NodeDataCache.formerlySerializedAsCache.Add(nodeType, new Dictionary<string, string>());
						}
						if (NodeDataCache.formerlySerializedAsCache[nodeType].ContainsKey(formerlySerializedAsAttribute.oldName))
						{
							Debug.LogError("Another FormerlySerializedAs with value '" + formerlySerializedAsAttribute.oldName + "' already exist on this node.");
						}
						else
						{
							NodeDataCache.formerlySerializedAsCache[nodeType].Add(formerlySerializedAsAttribute.oldName, nodeFields[i].Name);
						}
					}
				}
			}
		}

		private static NodeDataCache.PortDataCache portDataCache;

		private static Dictionary<Type, Dictionary<string, string>> formerlySerializedAsCache;

		[Serializable]
		private class PortDataCache : Dictionary<Type, List<NodePort>>, ISerializationCallbackReceiver
		{
			public void OnBeforeSerialize()
			{
				this.keys.Clear();
				this.values.Clear();
				foreach (KeyValuePair<Type, List<NodePort>> keyValuePair in this)
				{
					this.keys.Add(keyValuePair.Key);
					this.values.Add(keyValuePair.Value);
				}
			}

			public void OnAfterDeserialize()
			{
				base.Clear();
				if (this.keys.Count != this.values.Count)
				{
					throw new Exception(string.Format("there are {0} keys and {1} values after deserialization. Make sure that both key and value types are serializable.", new object[0]));
				}
				for (int i = 0; i < this.keys.Count; i++)
				{
					base.Add(this.keys[i], this.values[i]);
				}
			}

			[SerializeField]
			private List<Type> keys = new List<Type>();

			[SerializeField]
			private List<List<NodePort>> values = new List<List<NodePort>>();
		}
	}
}
