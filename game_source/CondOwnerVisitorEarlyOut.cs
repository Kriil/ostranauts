using System;

public class CondOwnerVisitorEarlyOut : CondOwnerVisitor
{
	public CondOwnerVisitorEarlyOut()
	{
	}

	public CondOwnerVisitorEarlyOut(CondOwnerVisitor a, CondTrigger b)
	{
		this.subVisitor = a;
		this.CondTrigger = b;
	}

	public override void Visit(CondOwner co)
	{
		this.CO = co;
	}

	public static CondOwnerVisitor WrapVisitor(CondOwnerVisitor a, CondTrigger b)
	{
		if (b == null)
		{
			return a;
		}
		return new CondOwnerVisitorEarlyOut(a, b);
	}

	public CondOwner CO;

	public CondOwnerVisitor subVisitor;

	public CondTrigger CondTrigger;
}
