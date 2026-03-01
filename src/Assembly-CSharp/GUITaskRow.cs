using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GUITaskRow : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler
{
	public void OnPointerClick(PointerEventData eventData)
	{
		CondOwner condOwner = null;
		if (this.task != null && this.task.strTargetCOID != null && DataHandler.mapCOs.TryGetValue(this.task.strTargetCOID, out condOwner) && condOwner != null)
		{
			CrewSim.objInstance.CamCenter(condOwner);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		CondOwner condOwner = null;
		if (this.task != null && this.task.strTargetCOID != null && DataHandler.mapCOs.TryGetValue(this.task.strTargetCOID, out condOwner) && condOwner != null)
		{
			condOwner.Highlight = true;
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		CondOwner condOwner = null;
		if (this.task != null && this.task.strTargetCOID != null && DataHandler.mapCOs.TryGetValue(this.task.strTargetCOID, out condOwner) && condOwner != null)
		{
			condOwner.Highlight = false;
		}
	}

	public bool SetTask(Task2 value)
	{
		if (value == null)
		{
			return false;
		}
		this.task = value;
		CondOwner condOwner = null;
		JsonCondOwnerSave jsonCondOwnerSave = null;
		string strFileName = this.task.GetIconName();
		string text = string.Empty;
		string text2 = string.Empty;
		if (DataHandler.mapCOs.TryGetValue(this.task.strTargetCOID, out condOwner) && condOwner != null)
		{
			condOwner.UpdateAppearance();
			Item component = condOwner.GetComponent<Item>();
			if (component != null)
			{
				strFileName = component.ImgOverride + ".png";
			}
			else if (condOwner.HasCond("IsHuman"))
			{
				strFileName = "BblIntimidate.png";
			}
			text = condOwner.FriendlyName;
			text2 = condOwner.ship.strRegID;
		}
		else
		{
			if (!DataHandler.dictCOSaves.TryGetValue(this.task.strTargetCOID, out jsonCondOwnerSave) || jsonCondOwnerSave == null)
			{
				CrewSim.objInstance.workManager.RemoveTask(this.task);
				return false;
			}
			strFileName = jsonCondOwnerSave.strIMGPreview + ".png";
			text = jsonCondOwnerSave.strFriendlyName;
			text2 = jsonCondOwnerSave.strRegIDLast;
		}
		Texture2D texture2D = DataHandler.LoadPNG(strFileName, false, false);
		base.transform.Find("bmpImage").GetComponent<Image>().sprite = Sprite.Create(texture2D, new Rect(0f, 0f, (float)texture2D.width, (float)texture2D.height), Vector2.zero);
		base.transform.Find("btnName/txt").GetComponent<TMP_Text>().text = text;
		base.transform.Find("txtShip").GetComponent<TMP_Text>().text = text2;
		base.transform.Find("txtDuty").GetComponent<TMP_Text>().text = this.task.strDuty;
		base.transform.Find("btnDelete").GetComponent<Button>().onClick.AddListener(delegate()
		{
			this.DeleteTask();
		});
		string text3 = "Unknown";
		Interaction interaction = DataHandler.GetInteraction(this.task.strInteraction, null, false);
		if (interaction != null)
		{
			text3 = interaction.strTitle;
		}
		base.transform.Find("txtTask").GetComponent<TMP_Text>().text = text3;
		string text4 = this.task.strStatus;
		if (text4 == string.Empty)
		{
			text4 = "Unclaimed";
		}
		base.transform.Find("txtStatus").GetComponent<TMP_Text>().text = text4;
		this.Task = this.task;
		return true;
	}

	private void DeleteTask()
	{
		if (this.task == null)
		{
			return;
		}
		CondOwner condOwner = null;
		if (DataHandler.mapCOs.TryGetValue(this.task.strTargetCOID, out condOwner))
		{
			condOwner.Highlight = false;
			Placeholder component = condOwner.GetComponent<Placeholder>();
			if (component != null)
			{
				component.Cancel(null);
			}
		}
		CrewSim.objInstance.workManager.RemoveTask(this.task);
	}

	public Task2 Task
	{
		get
		{
			return this.task;
		}
		private set
		{
			if (value == null)
			{
				return;
			}
			this.task = value;
		}
	}

	private Task2 task;
}
