using System;
using UnityEngine;

public class GiveItemDrop : DevCommand
{
	public GiveItemDrop()
	{
		this.keyword = "givedrop";
		DevConsole.commands.Add(this.keyword, this);
	}

	public override void Execute(string input)
	{
		CondOwner condOwner = DataHandler.GetCondOwner(input.Split(new char[]
		{
			' '
		})[1]);
		if (condOwner != null)
		{
			CrewSim.shipCurrentLoaded.AddCO(condOwner, true);
			condOwner.tf.position = new Vector3(CrewSim.coPlayer.tf.position.x, CrewSim.coPlayer.tf.position.y, condOwner.tf.position.z);
		}
	}
}
