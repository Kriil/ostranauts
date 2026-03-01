using System;
using MonoMod;
using Ostranauts.UI.MegaToolTip;
using UnityEngine;
public class patch_GUIInventoryWindow : GUIInventoryWindow
{
	[MonoModReplace]
	public Vector3 WorldPosFromPair(PairXY where)
	{
		bool flag = this.type == 0;
		Vector3 result;
		if (flag)
		{
			Vector3 vector = (patch_ConsoleResolver.bInvokedInventory && GUIMegaToolTip.Selected != null) ? GUIMegaToolTip.Selected.tf.position : CrewSim.GetSelectedCrew().tf.position;
			vector.x = (float)(MathUtils.RoundToInt(vector.x) + where.x - 2);
			vector.y = (float)(MathUtils.RoundToInt(vector.y) + 2 - where.y);
			result = vector;
		}
		else
		{
			result = base.CO.tf.position;
		}
		return result;
	}
}
