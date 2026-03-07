using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ContextMenuPool : MonoBehaviour
{
	public bool IsRaised
	{
		get
		{
			return this.activePool != null && this.activePool.Count > 0;
		}
	}

	private void Awake()
	{
		this.scaler = base.transform.parent.GetComponent<CanvasScaler>();
		base.transform.position = Vector3.zero;
		ContextMenuPool.bmpLive = DataHandler.LoadPNG("GUIContextMenuBackground.png", false, false);
		ContextMenuPool.bmpDead = DataHandler.LoadPNG("GUIContextMenuBackground01.png", false, false);
		Texture2D texture = DataHandler.LoadPNG("GUIContextMenuHumanSymbol.png", false, false);
		Texture2D texture2 = DataHandler.LoadPNG("GUIContextMenuHammerSymbol.png", false, false);
		Texture2D texture3 = DataHandler.LoadPNG("GUIContextMenuArrowSymbol.png", false, false);
		ContextMenuPool.clrDead = DataHandler.GetColor("ContextDead");
		ContextMenuPool.clrLive = DataHandler.GetColor("ContextLive");
		this.RadialContextMenuPrefab = (Resources.Load("pnlContextMenu02") as GameObject);
		for (int i = 0; i < 50; i++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.RadialContextMenuPrefab, base.transform);
			RadialContextMenuObject component = gameObject.GetComponent<RadialContextMenuObject>();
			component.pool = this;
			this.inactivePool.Add(gameObject.transform);
			gameObject.transform.position = Vector3.zero;
			gameObject.name = "Context Menu Item (pooled)";
			gameObject.GetComponent<RadialContextMenuObject>().Reset();
			component.image.texture = ContextMenuPool.bmpLive;
			component.humanImage.texture = texture;
			component.humanImage.color = ContextMenuPool.clrDead;
			component.itemImage.texture = texture2;
			component.itemImage.color = ContextMenuPool.clrDead;
			component.interactionImage.texture = texture3;
			component.interactionImage.color = ContextMenuPool.clrLive;
			component.gameObject.SetActive(false);
		}
	}

	public void AddToActive(Transform contextMenuObject)
	{
		this.activePool.Add(contextMenuObject);
	}

	public Transform GetMenuObject()
	{
		Transform transform = null;
		if (this.inactivePool.Count > 0)
		{
			transform = this.inactivePool[0];
			this.inactivePool.RemoveAt(0);
		}
		if (transform == null)
		{
			Debug.Log("Ran out of menu pool - should probably actually implement the ability to add more pool objects, eh Michael?");
			return null;
		}
		transform.gameObject.SetActive(true);
		return transform;
	}

	public void SetCOTracking(CondOwner co)
	{
		this.co = co;
		this.COTracking = co.tf;
	}

	public void Reset()
	{
		for (int i = this.activePool.Count - 1; i >= 0; i--)
		{
			this.inactivePool.Add(this.activePool[i]);
			this.activePool[i].GetComponent<RadialContextMenuObject>().Reset();
			this.activePool.RemoveAt(i);
		}
		this.co = null;
		this.COTracking = null;
	}

	public void ResetOthers(Transform t)
	{
		for (int i = this.activePool.Count - 1; i >= 0; i--)
		{
			if (this.activePool[i] != t)
			{
				RadialContextMenuObject component = this.activePool[i].GetComponent<RadialContextMenuObject>();
				if (component.condOwner != null || (component.interaction != null && component.interaction.strName == "Walk"))
				{
					this.inactivePool.Add(this.activePool[i]);
					component.Reset();
					this.activePool.RemoveAt(i);
				}
			}
		}
	}

	public void MoveToCondOwnerPosition()
	{
		if (this.COTracking != null && this.co != null)
		{
			if (GUIInventory.instance.Selected != null && this.co == GUIInventory.instance.Selected.CO)
			{
				this.Reset();
				return;
			}
			float num = CrewSim.objInstance.camMain.aspect / (this.scaler.referenceResolution.x / this.scaler.referenceResolution.y);
			float num2 = this.scaler.referenceResolution.x * num / (float)Screen.width;
			float num3 = this.scaler.referenceResolution.y / (float)Screen.height;
			Vector3 vector = Vector3.zero;
			bool flag = false;
			if (GUIInventory.instance.IsOpen)
			{
				foreach (GUIInventoryWindow guiinventoryWindow in GUIInventory.instance.activeWindows)
				{
					if (guiinventoryWindow.COGO.ContainsKey(this.co.strID) && guiinventoryWindow.COGO[this.co.strID].transform.parent != null && guiinventoryWindow.COGO[this.co.strID].transform.parent.parent != null && guiinventoryWindow.COGO[this.co.strID].transform.parent.parent.parent != null && guiinventoryWindow.COGO[this.co.strID].transform.parent.parent.parent == GUIInventory.instance.transform)
					{
						flag = true;
						vector = guiinventoryWindow.COGO[this.co.strID].transform.parent.parent.parent.InverseTransformPoint(guiinventoryWindow.COGO[this.co.strID].transform.position);
					}
				}
				if (GUIInventory.instance.PaperDollManager.mapCOIDsToGO.ContainsKey(this.co.strID) && GUIInventory.instance.PaperDollManager.mapCOIDsToGO[this.co.strID] != null)
				{
					Transform transform = GUIInventory.instance.PaperDollManager.mapCOIDsToGO[this.co.strID].transform;
					vector = transform.parent.parent.parent.InverseTransformPoint(transform.position);
					flag = true;
				}
			}
			if (!flag)
			{
				vector = CrewSim.objInstance.camMain.WorldToScreenPoint(this.COTracking.transform.position);
				vector = new Vector3(vector.x * num2, vector.y * num3);
				vector -= new Vector3((float)(Screen.width / 2) * num2, (float)(Screen.height / 2) * num3);
			}
			base.transform.localPosition = vector;
		}
	}

	public CondOwner co;

	public GameObject RadialContextMenuPrefab;

	public List<Transform> inactivePool = new List<Transform>();

	private readonly List<Transform> activePool = new List<Transform>();

	public Transform COTracking;

	public CanvasScaler scaler;

	public static Texture2D bmpLive;

	public static Texture2D bmpDead;

	public static Color clrDead;

	public static Color clrLive;
}
