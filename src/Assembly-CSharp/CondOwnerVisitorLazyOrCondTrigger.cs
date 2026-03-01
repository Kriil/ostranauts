using System;
using System.Collections.Generic;
using UnityEngine;

public class CondOwnerVisitorLazyOrCondTrigger : CondOwnerVisitor
{
	public CondOwnerVisitorLazyOrCondTrigger(CondOwnerVisitor a, List<CondTrigger> b)
	{
		if (b.Count == 0)
		{
			Debug.Log("ERROR: CondTriggers Array empty! instructions unclear...");
			Debug.Break();
		}
		this.subVisitor = a;
		this.aCondTrigs = b;
	}

	public override void Visit(CondOwner co)
	{
		foreach (CondTrigger condTrigger in this.aCondTrigs)
		{
			if (condTrigger.Triggered(co, null, true))
			{
				this.subVisitor.Visit(co);
				break;
			}
		}
	}

	public List<CondTrigger> aCondTrigs;

	public CondOwnerVisitor subVisitor;
}
