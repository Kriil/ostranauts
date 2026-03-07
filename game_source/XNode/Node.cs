using System;
using System.Collections.Generic;
using UnityEngine;

namespace XNode
{
	[Serializable]
	public abstract class Node : ScriptableObject
	{
		[Obsolete("Use DynamicPorts instead")]
		public IEnumerable<NodePort> InstancePorts
		{
			get
			{
				return this.DynamicPorts;
			}
		}

		[Obsolete("Use DynamicOutputs instead")]
		public IEnumerable<NodePort> InstanceOutputs
		{
			get
			{
				return this.DynamicOutputs;
			}
		}

		[Obsolete("Use DynamicInputs instead")]
		public IEnumerable<NodePort> InstanceInputs
		{
			get
			{
				return this.DynamicInputs;
			}
		}

		[Obsolete("Use AddDynamicInput instead")]
		public NodePort AddInstanceInput(Type type, Node.ConnectionType connectionType = Node.ConnectionType.Multiple, Node.TypeConstraint typeConstraint = Node.TypeConstraint.None, string fieldName = null)
		{
			return this.AddDynamicInput(type, connectionType, typeConstraint, fieldName);
		}

		[Obsolete("Use AddDynamicOutput instead")]
		public NodePort AddInstanceOutput(Type type, Node.ConnectionType connectionType = Node.ConnectionType.Multiple, Node.TypeConstraint typeConstraint = Node.TypeConstraint.None, string fieldName = null)
		{
			return this.AddDynamicOutput(type, connectionType, typeConstraint, fieldName);
		}

		[Obsolete("Use AddDynamicPort instead")]
		private NodePort AddInstancePort(Type type, NodePort.IO direction, Node.ConnectionType connectionType = Node.ConnectionType.Multiple, Node.TypeConstraint typeConstraint = Node.TypeConstraint.None, string fieldName = null)
		{
			return this.AddDynamicPort(type, direction, connectionType, typeConstraint, fieldName);
		}

		[Obsolete("Use RemoveDynamicPort instead")]
		public void RemoveInstancePort(string fieldName)
		{
			this.RemoveDynamicPort(fieldName);
		}

		[Obsolete("Use RemoveDynamicPort instead")]
		public void RemoveInstancePort(NodePort port)
		{
			this.RemoveDynamicPort(port);
		}

		[Obsolete("Use ClearDynamicPorts instead")]
		public void ClearInstancePorts()
		{
			this.ClearDynamicPorts();
		}

		public IEnumerable<NodePort> Ports
		{
			get
			{
				foreach (NodePort port in this.ports.Values)
				{
					yield return port;
				}
				yield break;
			}
		}

		public IEnumerable<NodePort> Outputs
		{
			get
			{
				foreach (NodePort port in this.Ports)
				{
					if (port.IsOutput)
					{
						yield return port;
					}
				}
				yield break;
			}
		}

		public IEnumerable<NodePort> Inputs
		{
			get
			{
				foreach (NodePort port in this.Ports)
				{
					if (port.IsInput)
					{
						yield return port;
					}
				}
				yield break;
			}
		}

		public IEnumerable<NodePort> DynamicPorts
		{
			get
			{
				foreach (NodePort port in this.Ports)
				{
					if (port.IsDynamic)
					{
						yield return port;
					}
				}
				yield break;
			}
		}

		public IEnumerable<NodePort> DynamicOutputs
		{
			get
			{
				foreach (NodePort port in this.Ports)
				{
					if (port.IsDynamic && port.IsOutput)
					{
						yield return port;
					}
				}
				yield break;
			}
		}

		public IEnumerable<NodePort> DynamicInputs
		{
			get
			{
				foreach (NodePort port in this.Ports)
				{
					if (port.IsDynamic && port.IsInput)
					{
						yield return port;
					}
				}
				yield break;
			}
		}

		protected void OnEnable()
		{
			if (Node.graphHotfix != null)
			{
				this.graph = Node.graphHotfix;
			}
			Node.graphHotfix = null;
			this.UpdatePorts();
			this.Init();
		}

		public void UpdatePorts()
		{
			NodeDataCache.UpdatePorts(this, this.ports);
		}

		protected virtual void Init()
		{
		}

		public void VerifyConnections()
		{
			foreach (NodePort nodePort in this.Ports)
			{
				nodePort.VerifyConnections();
			}
		}

		public NodePort AddDynamicInput(Type type, Node.ConnectionType connectionType = Node.ConnectionType.Multiple, Node.TypeConstraint typeConstraint = Node.TypeConstraint.None, string fieldName = null)
		{
			return this.AddDynamicPort(type, NodePort.IO.Input, connectionType, typeConstraint, fieldName);
		}

		public NodePort AddDynamicOutput(Type type, Node.ConnectionType connectionType = Node.ConnectionType.Multiple, Node.TypeConstraint typeConstraint = Node.TypeConstraint.None, string fieldName = null)
		{
			return this.AddDynamicPort(type, NodePort.IO.Output, connectionType, typeConstraint, fieldName);
		}

		private NodePort AddDynamicPort(Type type, NodePort.IO direction, Node.ConnectionType connectionType = Node.ConnectionType.Multiple, Node.TypeConstraint typeConstraint = Node.TypeConstraint.None, string fieldName = null)
		{
			if (fieldName == null)
			{
				fieldName = "dynamicInput_0";
				int num = 0;
				while (this.HasPort(fieldName))
				{
					fieldName = "dynamicInput_" + ++num;
				}
			}
			else if (this.HasPort(fieldName))
			{
				Debug.LogWarning("Port '" + fieldName + "' already exists in " + base.name, this);
				return this.ports[fieldName];
			}
			NodePort nodePort = new NodePort(fieldName, type, direction, connectionType, typeConstraint, this);
			this.ports.Add(fieldName, nodePort);
			return nodePort;
		}

		public void RemoveDynamicPort(string fieldName)
		{
			if (this.GetPort(fieldName) == null)
			{
				throw new ArgumentException("port " + fieldName + " doesn't exist");
			}
			this.RemoveDynamicPort(this.GetPort(fieldName));
		}

		public void RemoveDynamicPort(NodePort port)
		{
			if (port == null)
			{
				throw new ArgumentNullException("port");
			}
			if (port.IsStatic)
			{
				throw new ArgumentException("cannot remove static port");
			}
			port.ClearConnections();
			this.ports.Remove(port.fieldName);
		}

		[ContextMenu("Clear Dynamic Ports")]
		public void ClearDynamicPorts()
		{
			List<NodePort> list = new List<NodePort>(this.DynamicPorts);
			foreach (NodePort port in list)
			{
				this.RemoveDynamicPort(port);
			}
		}

		public NodePort GetOutputPort(string fieldName)
		{
			NodePort port = this.GetPort(fieldName);
			if (port == null || port.direction != NodePort.IO.Output)
			{
				return null;
			}
			return port;
		}

		public NodePort GetInputPort(string fieldName)
		{
			NodePort port = this.GetPort(fieldName);
			if (port == null || port.direction != NodePort.IO.Input)
			{
				return null;
			}
			return port;
		}

		public NodePort GetPort(string fieldName)
		{
			NodePort result;
			if (this.ports.TryGetValue(fieldName, out result))
			{
				return result;
			}
			return null;
		}

		public bool HasPort(string fieldName)
		{
			return this.ports.ContainsKey(fieldName);
		}

		public T GetInputValue<T>(string fieldName, T fallback = default(T))
		{
			NodePort port = this.GetPort(fieldName);
			if (port != null && port.IsConnected)
			{
				return port.GetInputValue<T>();
			}
			return fallback;
		}

		public T[] GetInputValues<T>(string fieldName, params T[] fallback)
		{
			NodePort port = this.GetPort(fieldName);
			if (port != null && port.IsConnected)
			{
				return port.GetInputValues<T>();
			}
			return fallback;
		}

		public virtual object GetValue(NodePort port)
		{
			Debug.LogWarning("No GetValue(NodePort port) override defined for " + base.GetType());
			return null;
		}

		public virtual void OnCreateConnection(NodePort from, NodePort to)
		{
		}

		public virtual void OnRemoveConnection(NodePort port)
		{
		}

		public void ClearConnections()
		{
			foreach (NodePort nodePort in this.Ports)
			{
				nodePort.ClearConnections();
			}
		}

		[SerializeField]
		public NodeGraph graph;

		[SerializeField]
		public Vector2 position;

		[SerializeField]
		private Node.NodePortDictionary ports = new Node.NodePortDictionary();

		public static NodeGraph graphHotfix;

		public enum ShowBackingValue
		{
			Never,
			Unconnected,
			Always
		}

		public enum ConnectionType
		{
			Multiple,
			Override
		}

		public enum TypeConstraint
		{
			None,
			Inherited,
			Strict,
			InheritedInverse,
			InheritedAny
		}

		[AttributeUsage(AttributeTargets.Field)]
		public class InputAttribute : Attribute
		{
			public InputAttribute(Node.ShowBackingValue backingValue = Node.ShowBackingValue.Unconnected, Node.ConnectionType connectionType = Node.ConnectionType.Multiple, Node.TypeConstraint typeConstraint = Node.TypeConstraint.None, bool dynamicPortList = false)
			{
				this.backingValue = backingValue;
				this.connectionType = connectionType;
				this.dynamicPortList = dynamicPortList;
				this.typeConstraint = typeConstraint;
			}

			[Obsolete("Use dynamicPortList instead")]
			public bool instancePortList
			{
				get
				{
					return this.dynamicPortList;
				}
				set
				{
					this.dynamicPortList = value;
				}
			}

			public Node.ShowBackingValue backingValue;

			public Node.ConnectionType connectionType;

			public bool dynamicPortList;

			public Node.TypeConstraint typeConstraint;
		}

		[AttributeUsage(AttributeTargets.Field)]
		public class OutputAttribute : Attribute
		{
			public OutputAttribute(Node.ShowBackingValue backingValue = Node.ShowBackingValue.Never, Node.ConnectionType connectionType = Node.ConnectionType.Multiple, Node.TypeConstraint typeConstraint = Node.TypeConstraint.None, bool dynamicPortList = false)
			{
				this.backingValue = backingValue;
				this.connectionType = connectionType;
				this.dynamicPortList = dynamicPortList;
				this.typeConstraint = typeConstraint;
			}

			[Obsolete("Use constructor with TypeConstraint")]
			public OutputAttribute(Node.ShowBackingValue backingValue, Node.ConnectionType connectionType, bool dynamicPortList) : this(backingValue, connectionType, Node.TypeConstraint.None, dynamicPortList)
			{
			}

			[Obsolete("Use dynamicPortList instead")]
			public bool instancePortList
			{
				get
				{
					return this.dynamicPortList;
				}
				set
				{
					this.dynamicPortList = value;
				}
			}

			public Node.ShowBackingValue backingValue;

			public Node.ConnectionType connectionType;

			public bool dynamicPortList;

			public Node.TypeConstraint typeConstraint;
		}

		[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
		public class CreateNodeMenuAttribute : Attribute
		{
			public CreateNodeMenuAttribute(string menuName)
			{
				this.menuName = menuName;
				this.order = 0;
			}

			public CreateNodeMenuAttribute(string menuName, int order)
			{
				this.menuName = menuName;
				this.order = order;
			}

			public string menuName;

			public int order;
		}

		[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
		public class DisallowMultipleNodesAttribute : Attribute
		{
			public DisallowMultipleNodesAttribute(int max = 1)
			{
				this.max = max;
			}

			public int max;
		}

		[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
		public class NodeTintAttribute : Attribute
		{
			public NodeTintAttribute(float r, float g, float b)
			{
				this.color = new Color(r, g, b);
			}

			public NodeTintAttribute(string hex)
			{
				ColorUtility.TryParseHtmlString(hex, out this.color);
			}

			public NodeTintAttribute(byte r, byte g, byte b)
			{
				this.color = new Color32(r, g, b, byte.MaxValue);
			}

			public Color color;
		}

		[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
		public class NodeWidthAttribute : Attribute
		{
			public NodeWidthAttribute(int width)
			{
				this.width = width;
			}

			public int width;
		}

		[Serializable]
		private class NodePortDictionary : Dictionary<string, NodePort>, ISerializationCallbackReceiver
		{
			public void OnBeforeSerialize()
			{
				this.keys.Clear();
				this.values.Clear();
				foreach (KeyValuePair<string, NodePort> keyValuePair in this)
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
					throw new Exception(string.Concat(new object[]
					{
						"there are ",
						this.keys.Count,
						" keys and ",
						this.values.Count,
						" values after deserialization. Make sure that both key and value types are serializable."
					}));
				}
				for (int i = 0; i < this.keys.Count; i++)
				{
					base.Add(this.keys[i], this.values[i]);
				}
			}

			[SerializeField]
			private List<string> keys = new List<string>();

			[SerializeField]
			private List<NodePort> values = new List<NodePort>();
		}
	}
}
