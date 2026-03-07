using System;
using System.Collections.Generic;

public class CondOwnerVisitorAddToHashSet : CondOwnerVisitor
{
	public CondOwnerVisitorAddToHashSet()
	{
		this.aHashSet = new HashSet<CondOwner>();
	}

	public override void Visit(CondOwner co)
	{
		this.aHashSet.Add(co);
	}

	public HashSet<CondOwner> aHashSet;
}
