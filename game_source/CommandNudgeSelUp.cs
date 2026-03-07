using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CommandNudgeSelUp : Command
{
	public CommandNudgeSelUp()
	{
		this.defaultCombo = new List<KeyCode>();
		this.defaultCombo.Add(KeyCode.UpArrow);
		this.commandDisplayLabel = "Nudge Selection Up:";
	}

	public override void Execute()
	{
		if (CanvasManager.instance == null)
		{
			return;
		}
		if (!CrewSim.bShipEdit || CrewSim.aSelected.Count == 0)
		{
			return;
		}
		if (CrewSim.Typing)
		{
			return;
		}
		if (!base.Down)
		{
			return;
		}
		CondOwner[] array = new CondOwner[CrewSim.aSelected.Count];
		CrewSim.aSelected.CopyTo(array);
		CommandNudgeSelUp.Nudge(array, new Vector3(0f, 1f, 0f));
	}

	public static void Nudge(CondOwner[] aCOs, Vector3 vDir)
	{
		if (aCOs.Length == 0 || vDir == Vector3.zero)
		{
			return;
		}
		if (CommandNudgeSelUp._ctNotTile == null)
		{
			CommandNudgeSelUp._ctNotTile = DataHandler.GetCondTrigger("TIsNotShipTile");
		}
		CrewSim.Paused = true;
		foreach (CondOwner condOwner in aCOs)
		{
			if (!(condOwner == null))
			{
				if (!(condOwner.objCOParent != null) && condOwner.slotNow == null && !(condOwner.coStackHead != null))
				{
					CrewSim.shipCurrentLoaded.RemoveCO(condOwner, true);
					Vector3 vector = condOwner.tf.position;
					vector += vDir;
					condOwner.tf.position = vector;
				}
			}
		}
		foreach (CondOwner condOwner2 in aCOs)
		{
			if (!(condOwner2 == null))
			{
				if (!(condOwner2.objCOParent != null) && condOwner2.slotNow == null && !(condOwner2.coStackHead != null))
				{
					Vector2 vector2 = new Vector2(condOwner2.tf.position.x - ((float)condOwner2.Item.nWidthInTiles / 2f - 0.5f) * 1f, condOwner2.tf.position.y + ((float)condOwner2.Item.nHeightInTiles / 2f - 0.5f) * 1f);
					for (int k = 0; k < condOwner2.Item.nHeightInTiles; k++)
					{
						for (int l = 0; l < condOwner2.Item.nWidthInTiles; l++)
						{
							Vector2 vPos = new Vector2(vector2.x + (float)l, vector2.y - (float)k);
							List<CondOwner> list = new List<CondOwner>();
							CrewSim.shipCurrentLoaded.GetCOsAtWorldCoords1(vPos, CommandNudgeSelUp._ctNotTile, false, true, list);
							foreach (CondOwner condOwner3 in list)
							{
								if (!(condOwner3 == null))
								{
									condOwner3.RemoveFromCurrentHome(false);
									condOwner3.Destroy();
								}
							}
						}
					}
				}
			}
		}
		foreach (CondOwner condOwner4 in aCOs)
		{
			if (!(condOwner4 == null))
			{
				if (!(condOwner4.objCOParent != null) && condOwner4.slotNow == null && !(condOwner4.coStackHead != null))
				{
					CrewSim.shipCurrentLoaded.AddCO(condOwner4, true);
				}
			}
		}
	}

	private static CondTrigger _ctNotTile;
}
