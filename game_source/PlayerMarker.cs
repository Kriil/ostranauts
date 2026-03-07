using System;
using UnityEngine;

public class PlayerMarker : MonoBehaviour
{
	public void SetShift()
	{
		if (this.co == null)
		{
			return;
		}
		if (this.co.HasCond("IsAIManual"))
		{
			this.strMode = "M";
		}
		else if (this.co.jsShiftLast == null || this.co.jsShiftLast.nID == 0)
		{
			this.strMode = "F";
		}
		else if (this.co.jsShiftLast.nID == 1)
		{
			this.strMode = "Z";
		}
		else if (this.co.jsShiftLast.nID == 2)
		{
			this.strMode = "W";
		}
		Renderer component = base.GetComponent<Renderer>();
		component.sharedMaterial = DataHandler.GetMaterial(component, "CrewIcon" + this.strRole + this.strMode, "blank", "blank", "blank");
	}

	private void Update()
	{
		if (this.co == null)
		{
			UnityEngine.Object.Destroy(this);
			return;
		}
		this.tf.rotation = Quaternion.identity;
		this.tf.position = this.co.tf.position + this.offset;
	}

	public static void AddMarker(CondOwner coIn)
	{
	}

	private CondOwner co;

	private string strRole = "1";

	private string strMode = "F";

	private Transform tf;

	private Vector3 offset = new Vector3(-0.75f, 0.75f);
}
