using System;

public class CondOwnerVisitorSystemAction : CondOwnerVisitor
{
	public CondOwnerVisitorSystemAction(Action<CondOwner> a)
	{
		this.systemAction = a;
	}

	public override void Visit(CondOwner co)
	{
		this.systemAction(co);
	}

	public Action<CondOwner> systemAction;
}
