using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RadialContextMenuObject : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IEventSystemHandler
{
	public void Setup(Interaction newInteraction)
	{
		base.name = "Context Menu Interaction (active)";
		this.canvasGroup.blocksRaycasts = true;
		this.interactionSymbol.alpha = 1f;
		this.image.color = ContextMenuPool.clrLive;
		this.image.texture = ContextMenuPool.bmpLive;
		if (newInteraction != null)
		{
			this.interaction = newInteraction;
			this.text.text = newInteraction.strActionGroup + ": " + newInteraction.strTitle;
			this.RecursiveResize();
		}
		this.pool.AddToActive(base.transform);
		base.StartCoroutine("FadeIn", 0.15f);
	}

	public void Setup(CondOwner newCondOwner)
	{
		base.name = "Context Menu CO (active)";
		this.image.color = ContextMenuPool.clrDead;
		this.image.texture = ContextMenuPool.bmpDead;
		if (newCondOwner.HasCond("IsHuman"))
		{
			this.humanSymbol.alpha = 1f;
		}
		else
		{
			this.itemSymbol.alpha = 1f;
		}
		if (newCondOwner != null)
		{
			this.condOwner = newCondOwner;
			this.text.text = newCondOwner.ShortName;
			this.RecursiveResize();
		}
		this.canvasGroup.blocksRaycasts = true;
		this.pool.AddToActive(base.transform);
		base.StartCoroutine("FadeIn", 0.15f);
	}

	public void RecursiveResize()
	{
	}

	private void RecursiveShrinkText()
	{
		if (this.text.preferredWidth > this.text.GetComponent<RectTransform>().rect.width)
		{
			this.text.fontSize -= 3f;
		}
		if (this.text.fontSize < 10f)
		{
			return;
		}
		if (this.text.preferredWidth > this.text.GetComponent<RectTransform>().rect.width)
		{
			this.RecursiveShrinkText();
		}
	}

	public IEnumerator FadeIn(float duration)
	{
		float startOpacity = this.canvasGroup.alpha;
		float endOpacity = 1f;
		float t = 0f;
		while (t < duration)
		{
			t += Time.deltaTime;
			float blend = Mathf.Clamp01(t / duration);
			this.canvasGroup.alpha = Mathf.Lerp(startOpacity, endOpacity, blend);
			yield return null;
		}
		yield break;
	}

	public IEnumerator FadeOut(float duration)
	{
		float startOpacity = this.canvasGroup.alpha;
		float endOpacity = 0f;
		float t = 0f;
		while (t < duration)
		{
			t += Time.deltaTime;
			float blend = Mathf.Clamp01(t / duration);
			this.canvasGroup.alpha = Mathf.Lerp(startOpacity, endOpacity, blend);
			yield return null;
		}
		base.transform.position = Vector3.zero;
		base.gameObject.SetActive(false);
		yield break;
	}

	public void Reset()
	{
		this.image.color = ContextMenuPool.clrDead;
		this.text.text = string.Empty;
		this.interactions.Clear();
		this.interaction = null;
		this.canvasGroup.blocksRaycasts = false;
		this.humanSymbol.alpha = 0f;
		this.itemSymbol.alpha = 0f;
		this.interactionSymbol.alpha = 0f;
		this.text.fontSize = 30f;
		this.text.color = ContextMenuPool.clrDead;
		this.condOwner = null;
		base.name = "Context Menu Object (pooled)";
		base.StartCoroutine("FadeOut", 0.15f);
	}

	public void OnPointerEnter(PointerEventData pointer)
	{
		this._isMouseOverUI = true;
	}

	public void OnPointerClick(PointerEventData pointer)
	{
		if (this.condOwner != null)
		{
			this.pool.SetCOTracking(this.condOwner);
			this.pool.MoveToCondOwnerPosition();
			base.transform.localPosition = Vector2.zero;
			float num = (float)this.interactions.Count;
			if (this.interactions.Count > 8)
			{
				num = 8f;
			}
			int num2 = 1;
			for (int i = 0; i < this.interactions.Count; i++)
			{
				if (i > 7)
				{
					num = 16f;
					num2 = 2;
				}
				if (this.pool.inactivePool.Count > 0)
				{
					Transform menuObject = this.pool.GetMenuObject();
					this.activeChildren.Add(menuObject.gameObject);
					float num3 = (float)i / num;
					Vector3 vector = new Vector3(Mathf.Sin(-num3 * 360f * 0.017453292f), Mathf.Cos(-num3 * 360f * 0.017453292f));
					vector *= 200f * (float)num2;
					vector = new Vector2(vector.x * 1.2f, vector.y * 6f / 16f);
					menuObject.localPosition = Vector3.zero + vector;
					menuObject.GetComponent<RadialContextMenuObject>().Setup(this.interactions[i]);
					CanvasManager.SetAnchorsToCorners(menuObject);
				}
			}
			this.pool.ResetOthers(base.transform);
		}
		else if (this.interaction == null)
		{
			CrewSim.objInstance.LowerContextMenu();
			return;
		}
		if (this.interaction == null)
		{
			return;
		}
		RadialContextMenuObject.ProcessInteraction(this.interaction);
		this.pool.Reset();
	}

	public static void ProcessInteraction(GUIQuickActionButton qab)
	{
		if (qab.IA == null)
		{
			return;
		}
		AudioManager.am.PlayAudioEmitter("ShipUIBtnPDAClick02", false, false);
		RadialContextMenuObject.ProcessInteraction(qab.IA);
	}

	public static void ProcessInteraction(Interaction interaction)
	{
		if (interaction == null)
		{
			return;
		}
		if (interaction.strName == "ACTCancelTaskThem" && interaction.objThem != null)
		{
			CrewSim.objInstance.workManager.RemoveTask(interaction.objThem.strID);
			Placeholder component = interaction.objThem.GetComponent<Placeholder>();
			if (component != null)
			{
				component.Cancel(interaction.objUs);
			}
		}
		else if (interaction.strName == "ACTResumeTaskThem" && interaction.objThem != null)
		{
			CrewSim.objInstance.workManager.ResumeTask(interaction.objUs, interaction.objThem.strID);
		}
		else if (interaction.objThem != null && interaction.objThem.HasCond("IsPlaceholder"))
		{
			Debug.Log("Placeholder");
		}
		else if (interaction.objUs != null && interaction.strName == "AIModeDisable")
		{
			if (CrewSim.GetSelectedCrew() == interaction.objUs)
			{
				CrewSim.AIManual(true);
			}
			else
			{
				interaction.objUs.SetCondAmount("IsAIManual", 1.0, 0.0);
				PlayerMarker.AddMarker(interaction.objUs);
			}
		}
		else if (interaction.objUs != null && interaction.strName == "AIModeEnable")
		{
			if (CrewSim.GetSelectedCrew() == interaction.objUs)
			{
				CrewSim.AIManual(false);
			}
			else
			{
				interaction.objUs.ZeroCondAmount("IsAIManual");
				PlayerMarker.AddMarker(interaction.objUs);
			}
		}
		else if (interaction.objUs != null && interaction.objThem != null && interaction.strName == "Walk")
		{
			Transform transform = interaction.objThem.transform;
			interaction.objUs.AIIssueOrder(null, null, true, interaction.objUs.ship.GetTileAtWorldCoords1(transform.position.x, transform.position.y, true, true), 0f, 0f);
			CrewSim.AIManual(true);
			CrewSim.objInstance.workManager.IdleRemove(interaction.objUs);
		}
		else if (interaction.objThem != null && interaction.strStartInstall != null)
		{
			JsonCOOverlay cooverlay = DataHandler.GetCOOverlay(interaction.objThem.strCODef);
			if (cooverlay != null)
			{
				string modeSwitch = cooverlay.GetModeSwitch(interaction.strStartInstall);
				if (modeSwitch != null)
				{
					interaction.strStartInstall = modeSwitch;
				}
			}
			JsonInstallable jsonInstallable = Installables.GetJsonInstallable(interaction.strStartInstall);
			jsonInstallable.strPersistentCO = interaction.objThem.strID;
			CrewSim.objInstance.StartPaintingJob(jsonInstallable);
		}
		else if (interaction.objUs != null)
		{
			interaction.bVerboseTrigger = true;
			if (interaction.strName != null)
			{
				string strName = interaction.strName;
			}
			if (Array.IndexOf<string>(JsonCompanyRules.aDutiesNew, interaction.strDuty) >= 0)
			{
				int hourFromS = MathUtils.GetHourFromS(StarSystem.fEpoch);
				if (interaction.objUs.Company != null && interaction.objUs.Company.GetShift(hourFromS, interaction.objUs).nID == 2)
				{
					CrewSim.objInstance.workManager.ClaimTaskDirect(interaction);
				}
				else
				{
					Task2 task = new Task2();
					task.strDuty = interaction.strDuty;
					task.strInteraction = interaction.strName;
					task.strName = interaction.strTitle;
					if (interaction.objThem != null)
					{
						task.strTargetCOID = interaction.objThem.strID;
					}
					task.SetIA(interaction);
					task.AddOwner(interaction.objUs.strID);
					CrewSim.objInstance.workManager.AddTask(task, 1);
					interaction.objUs.LogMessage(interaction.objUs.FriendlyName + DataHandler.GetString("SHIFT_WARN_NONWORK", false), "Bad", interaction.objUs.strID);
				}
			}
			else if (interaction.objThem != null)
			{
				bool bChanceSkip = CondTrigger.bChanceSkip;
				CondTrigger.bChanceSkip = true;
				bool flag = interaction.Triggered(interaction.objUs, interaction.objThem, false, false, false, true, null);
				CondTrigger.bChanceSkip = bChanceSkip;
				if (flag)
				{
					interaction.objUs.AIIssueOrder(interaction.objThem, interaction, true, null, 0f, 0f);
					CrewSim.AIManual(true);
					CrewSim.objInstance.workManager.IdleRemove(interaction.objUs);
				}
				else
				{
					string strMsg = interaction.FailReasons(true, true, false);
					interaction.objUs.LogMessage(strMsg, "Bad", interaction.objUs.strName);
				}
			}
		}
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		this._isMouseOverUI = false;
	}

	private void Update()
	{
	}

	public TextMeshProUGUI text;

	public Interaction interaction;

	public CondOwner condOwner;

	public List<Interaction> interactions = new List<Interaction>();

	public ContextMenuPool pool;

	public CanvasGroup canvasGroup;

	public CanvasGroup humanSymbol;

	public CanvasGroup itemSymbol;

	public CanvasGroup interactionSymbol;

	public RawImage image;

	public RawImage humanImage;

	public RawImage itemImage;

	public RawImage interactionImage;

	public List<GameObject> activeChildren = new List<GameObject>();

	public float interactionTimer;

	private bool _isMouseOverUI;
}
