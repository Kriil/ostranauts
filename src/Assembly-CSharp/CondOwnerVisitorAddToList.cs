using System;
using System.Collections.Generic;

public class CondOwnerVisitorAddToList : CondOwnerVisitor
{
	public CondOwnerVisitorAddToList()
	{
		this.aList = new List<CondOwner>();
	}

	public override void Visit(CondOwner co)
	{
		this.aList.Add(co);
	}

	public List<CondOwner> aList;
}
