using System;
using System.Collections.Generic;
using UnityEngine;

namespace XNode
{
	[Serializable]
	public abstract class NodeGraph : ScriptableObject
	{
		public T AddNode<T>() where T : Node
		{
			return this.AddNode(typeof(T)) as T;
		}

		public virtual Node AddNode(Type type)
		{
			Node.graphHotfix = this;
			Node node = ScriptableObject.CreateInstance(type) as Node;
			node.graph = this;
			this.nodes.Add(node);
			return node;
		}

		public virtual Node CopyNode(Node original)
		{
			Node.graphHotfix = this;
			Node node = UnityEngine.Object.Instantiate<Node>(original);
			node.graph = this;
			node.ClearConnections();
			this.nodes.Add(node);
			return node;
		}

		public virtual void RemoveNode(Node node)
		{
			node.ClearConnections();
			this.nodes.Remove(node);
			if (Application.isPlaying)
			{
				UnityEngine.Object.Destroy(node);
			}
		}

		public virtual void Clear()
		{
			if (Application.isPlaying)
			{
				for (int i = 0; i < this.nodes.Count; i++)
				{
					UnityEngine.Object.Destroy(this.nodes[i]);
				}
			}
			this.nodes.Clear();
		}

		public virtual NodeGraph Copy()
		{
			NodeGraph nodeGraph = UnityEngine.Object.Instantiate<NodeGraph>(this);
			for (int i = 0; i < this.nodes.Count; i++)
			{
				if (!(this.nodes[i] == null))
				{
					Node.graphHotfix = nodeGraph;
					Node node = UnityEngine.Object.Instantiate<Node>(this.nodes[i]);
					node.graph = nodeGraph;
					nodeGraph.nodes[i] = node;
				}
			}
			for (int j = 0; j < nodeGraph.nodes.Count; j++)
			{
				if (!(nodeGraph.nodes[j] == null))
				{
					foreach (NodePort nodePort in nodeGraph.nodes[j].Ports)
					{
						nodePort.Redirect(this.nodes, nodeGraph.nodes);
					}
				}
			}
			return nodeGraph;
		}

		protected virtual void OnDestroy()
		{
			this.Clear();
		}

		[SerializeField]
		public List<Node> nodes = new List<Node>();

		[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
		public class RequireNodeAttribute : Attribute
		{
			public RequireNodeAttribute(Type type)
			{
				this.type0 = type;
				this.type1 = null;
				this.type2 = null;
			}

			public RequireNodeAttribute(Type type, Type type2)
			{
				this.type0 = type;
				this.type1 = type2;
				this.type2 = null;
			}

			public RequireNodeAttribute(Type type, Type type2, Type type3)
			{
				this.type0 = type;
				this.type1 = type2;
				this.type2 = type3;
			}

			public bool Requires(Type type)
			{
				return type != null && (type == this.type0 || type == this.type1 || type == this.type2);
			}

			public Type type0;

			public Type type1;

			public Type type2;
		}
	}
}
