using System;

namespace XNode
{
	public class SceneGraph<T> : SceneGraph where T : NodeGraph
	{
		public new T graph
		{
			get
			{
				return this.graph as T;
			}
			set
			{
				this.graph = value;
			}
		}
	}
}
