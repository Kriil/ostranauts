using System;

public class CondOwnerVisitorCondTrigger : CondOwnerVisitor
{
	public CondOwnerVisitorCondTrigger(CondOwnerVisitor a, CondTrigger b)
	{
		this.subVisitor = a;
		this.objCondTrig = b;
	}

	public static CondOwnerVisitor WrapVisitor(CondOwnerVisitor a, CondTrigger b)
	{
		if (b == null)
		{
			return a;
		}
		return new CondOwnerVisitorCondTrigger(a, b);
	}

	public override void Visit(CondOwner co)
	{
		if (this.objCondTrig.Triggered(co, null, false))
		{
			this.subVisitor.Visit(co);
		}
	}

	public CondTrigger objCondTrig;

	public CondOwnerVisitor subVisitor;
}
