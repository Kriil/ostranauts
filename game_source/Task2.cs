using System;
using System.Collections.Generic;
using UnityEngine;

public class Task2
{
	public override string ToString()
	{
		return string.Concat(new string[]
		{
			this.strName,
			": ",
			this.strDuty,
			"; ",
			this.strInteraction
		});
	}

	public GameObject GetConstructionSign()
	{
		return this.goConstructionSign;
	}

	public void SetConstructionSign(GameObject goSign)
	{
		this.goConstructionSign = goSign;
	}

	public bool Matches(string strIA, string strTarget)
	{
		return strIA != null && strTarget != null && strIA == this.strInteraction && strTarget == this.strTargetCOID;
	}

	public Interaction GetIA()
	{
		return this.iact;
	}

	public void SetIA(Interaction ia)
	{
		this.iact = ia;
		if (ia != null && ia.CTTestThem != null)
		{
			this.strCTTestThem = ia.CTTestThem.strName;
		}
	}

	public string GetIconName()
	{
		string text = null;
		Interaction interaction = DataHandler.GetInteraction(this.strInteraction, null, false);
		if (interaction != null)
		{
			text = interaction.strMapIcon;
		}
		if (text == null || text == string.Empty)
		{
			text = "IcoConstructionSign";
		}
		return text;
	}

	public void CopyFrom(Task2 taskFrom)
	{
		if (taskFrom == null)
		{
			return;
		}
		if (taskFrom.aOwnerIDs == null)
		{
			this.aOwnerIDs = null;
		}
		else
		{
			this.aOwnerIDs = new string[taskFrom.aOwnerIDs.Length];
			for (int i = 0; i < taskFrom.aOwnerIDs.Length; i++)
			{
				this.aOwnerIDs[i] = taskFrom.aOwnerIDs[i];
			}
		}
		this.bManual = taskFrom.bManual;
	}

	public Task2.Allowed GetOwnership(string strID)
	{
		if (this.aOwnerIDs == null || this.aOwnerIDs.Length == 0)
		{
			return Task2.Allowed.Allowed;
		}
		if (strID == null || Array.IndexOf<string>(this.aOwnerIDs, strID) < 0)
		{
			return Task2.Allowed.Forbidden;
		}
		return Task2.Allowed.Owned;
	}

	public void AddOwner(string strID)
	{
		if (strID == null)
		{
			return;
		}
		List<string> list;
		if (this.aOwnerIDs != null)
		{
			list = new List<string>(this.aOwnerIDs);
		}
		else
		{
			list = new List<string>();
		}
		if (list.IndexOf(strID) < 0)
		{
			list.Add(strID);
		}
		this.aOwnerIDs = list.ToArray();
	}

	public void RemoveOwner(string strID)
	{
		if (strID == null || this.aOwnerIDs == null || Array.IndexOf<string>(this.aOwnerIDs, strID) < 0)
		{
			return;
		}
		List<string> list = new List<string>(this.aOwnerIDs);
		list.Remove(strID);
		this.aOwnerIDs = list.ToArray();
	}

	public void ClearOwners()
	{
		if (this.aOwnerIDs == null || this.aOwnerIDs.Length == 0)
		{
			return;
		}
		this.aOwnerIDs = new string[0];
	}

	public void UpdateTint(float blendLoss)
	{
		GameObject constructionSign = this.GetConstructionSign();
		if (constructionSign == null)
		{
			return;
		}
		MeshRenderer component = constructionSign.GetComponent<MeshRenderer>();
		if (component == null)
		{
			return;
		}
		this.fTintBlend = Mathf.Clamp01(this.fTintBlend - blendLoss);
		Color value = Color.Lerp(Color.white, this.clrTint, this.fTintBlend);
		MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
		component.GetPropertyBlock(materialPropertyBlock);
		materialPropertyBlock.SetColor("_Color", value);
		component.SetPropertyBlock(materialPropertyBlock);
	}

	public string strName;

	public string strDuty;

	public string strTargetCOID;

	public string strInteraction;

	public string strStatus = string.Empty;

	public string strCTTestThem;

	public string[] aOwnerIDs;

	private GameObject goConstructionSign;

	public Color clrTint;

	public float fTintBlend;

	public int nTile;

	public string strTileShip;

	private Interaction iact;

	public double fLastCheck;

	public bool bManual;

	public enum Allowed
	{
		Owned,
		Allowed,
		Forbidden
	}
}
