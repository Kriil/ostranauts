using System;
using UnityEngine;
using XNode;

[Node.NodeWidthAttribute(268)]
[Node.NodeTintAttribute("#336633")]
public class CTNode : Node
{
	public override object GetValue(NodePort port)
	{
		return null;
	}

	[Node.InputAttribute(Node.ShowBackingValue.Unconnected, Node.ConnectionType.Multiple, Node.TypeConstraint.None, false)]
	public string[] m_Input;

	public string strName;

	[TextArea]
	public string strReqs;

	[TextArea]
	public string strForbids;

	[TextArea]
	public string strTriggers;

	public bool bAND;

	public float fChance = 1f;

	[Node.OutputAttribute(Node.ShowBackingValue.Never, Node.ConnectionType.Multiple, Node.TypeConstraint.None, false)]
	public string[] aNext;
}
