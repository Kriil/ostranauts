using System;

public class CondOwnerVisitorZeroCond : CondOwnerVisitor
{
	public override void Visit(CondOwner co)
	{
		co.ZeroCondAmount(this.strCond);
	}

	public string strCond;
}
