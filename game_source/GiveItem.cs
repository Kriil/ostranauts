using System;

public class GiveItem : DevCommand
{
	public GiveItem()
	{
		this.keyword = "give";
		DevConsole.commands.Add(this.keyword, this);
	}

	public override void Execute(string input)
	{
		CondOwner condOwner = DataHandler.GetCondOwner(input.Split(new char[]
		{
			' '
		})[1]);
		int num = 0;
		if (input.Split(new char[]
		{
			' '
		}).Length > 2)
		{
			num = int.Parse(input.Split(new char[]
			{
				' '
			})[2].ToString());
		}
		if (condOwner != null)
		{
			CrewSim.coPlayer.AddCO(condOwner, false, true, true);
			if (num > 0)
			{
				for (int i = 1; i < num; i++)
				{
					CondOwner condOwner2 = DataHandler.GetCondOwner(input.Split(new char[]
					{
						' '
					})[1]);
					CrewSim.coPlayer.AddCO(condOwner2, false, true, true);
				}
			}
		}
	}
}
