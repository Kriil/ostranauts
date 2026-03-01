using System;

public class CondOwnerVisitorCatchUp : CondOwnerVisitor
{
	public override void Visit(CondOwner co)
	{
		co.CatchUp();
	}
}
