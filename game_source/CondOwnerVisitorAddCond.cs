using System;

public class CondOwnerVisitorAddCond : CondOwnerVisitor
{
	public override void Visit(CondOwner co)
	{
		co.AddCondAmount(this.strCond, this.fAmount, 0.0, 0f);
	}

	public string strCond;

	public double fAmount;
}
