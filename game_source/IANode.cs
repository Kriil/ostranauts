using System;
using UnityEngine;
using XNode;

[Node.NodeWidthAttribute(500)]
[Node.NodeTintAttribute("#3333aa")]
public class IANode : Node
{
	public override object GetValue(NodePort port)
	{
		return null;
	}

	[Node.InputAttribute(Node.ShowBackingValue.Unconnected, Node.ConnectionType.Multiple, Node.TypeConstraint.None, false)]
	public string[] m_Input;

	public string strName;

	public string strTitle;

	[TextArea]
	public string strDesc;

	public MoveType strMoveType;

	[Space(16f)]
	public Loots loots;

	[Space(16f)]
	public PSpecTest pspecTest;

	[Space(16f)]
	[Node.OutputAttribute(Node.ShowBackingValue.Never, Node.ConnectionType.Multiple, Node.TypeConstraint.None, false)]
	public string[] aInverse;
}
