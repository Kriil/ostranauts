using System;
using UnityEngine;
using XNode;

[Node.NodeWidthAttribute(500)]
public class BBGNode : Node
{
	public override object GetValue(NodePort port)
	{
		if (port.fieldName == "fAmountOut")
		{
			return base.GetInputValue<float>("fAmountIn", this.fAmountIn);
		}
		if (port.fieldName == "fAmountIn")
		{
			return base.GetInputValue<float>("fAmountOut", this.fAmountOut);
		}
		return null;
	}

	public string aPrevious;

	public string aInverse;

	public string strName;

	public string strTitle;

	[TextArea]
	public string strDesc;

	public string strContextLootUs;

	public string strContextLootThem;

	[Node.InputAttribute(Node.ShowBackingValue.Unconnected, Node.ConnectionType.Multiple, Node.TypeConstraint.None, false)]
	public float fAmountIn;

	[Node.OutputAttribute(Node.ShowBackingValue.Never, Node.ConnectionType.Multiple, Node.TypeConstraint.None, false)]
	public float fAmountOut;

	[BBGList]
	public string CTUs;

	[BBGList]
	public string CTThem;
}
