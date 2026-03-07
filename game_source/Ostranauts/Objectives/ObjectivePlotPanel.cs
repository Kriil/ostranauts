using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Ostranauts.Objectives
{
	public class ObjectivePlotPanel : ObjectivePanel, IPointerEnterHandler, IPointerExitHandler, IEventSystemHandler
	{
		protected override void Awake()
		{
			base.Awake();
		}

		private void Start()
		{
		}

		private void Update()
		{
			if (this.bMouseOver)
			{
				bool flag = base.CheckLinks(this._txtDescription);
				if (!flag)
				{
					IEnumerator enumerator = base.transform.GetEnumerator();
					try
					{
						while (enumerator.MoveNext())
						{
							object obj = enumerator.Current;
							Transform transform = (Transform)obj;
							TMP_Text componentInChildren = transform.GetComponentInChildren<TMP_Text>();
							if (!(componentInChildren == null) && !(componentInChildren == this._txtTitle) && !(componentInChildren == this._txtDescription))
							{
								flag = base.CheckLinks(componentInChildren);
								if (flag)
								{
									break;
								}
							}
						}
					}
					finally
					{
						IDisposable disposable;
						if ((disposable = (enumerator as IDisposable)) != null)
						{
							disposable.Dispose();
						}
					}
				}
				if (!flag && this.strLastHighlightCO != null)
				{
					AudioManager.am.PlayAudioEmitter("UIObjectiveLinkMouseover", false, false);
					this._objective.Highlight = true;
					this.strLastHighlightCO = null;
				}
				base.MouseInput();
			}
			base.HighlightBorder();
			base.SyncCGState();
		}

		private void OnDestroy()
		{
			ObjectivesHost.OnObjectiveSingleAnimationFinishedEvent.RemoveListener(new UnityAction<ObjectivePanel>(base.DelayedVisibilitySinglePanel));
			ObjectivesHost.OnObjectiveAnimationFinished.RemoveListener(new UnityAction(this.DelayedVisibility));
		}

		public void SetData(Objective obj, bool showClose = true, bool bSkipList = false)
		{
			this._objective = obj;
			JsonPlotSave jsonPlotSave = PlotManager.GetActivePlot(obj.strPlotName);
			if (jsonPlotSave == null)
			{
				jsonPlotSave = PlotManager.GetOldPlot(obj.strPlotName);
			}
			if (jsonPlotSave == null)
			{
				Debug.LogError("Error: Null JsonPlotSave on objective: " + obj.strDisplayName);
				return;
			}
			JsonPlot plot = DataHandler.GetPlot(jsonPlotSave.strPlotName);
			this._txtTitle.text = plot.FriendlyName;
			string text = jsonPlotSave.GetCurrentPhaseTitle(this._objective.COFocus, "<color=#FFCC00>", "</color>");
			if (text == null)
			{
				text = obj.strDisplayDesc;
			}
			this._txtDescription.text = text;
			string str = (!(obj.COFocus != null)) ? string.Empty : obj.COFocus.strID;
			this._txtDescription.GetComponent<LinkOpener>().strDefaultURL = "coid:" + str;
			if (!bSkipList)
			{
				if (jsonPlotSave.aCompletedBeats == null)
				{
					jsonPlotSave.aCompletedBeats = new string[0];
				}
				foreach (string str2 in jsonPlotSave.aCompletedBeats)
				{
					GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this._rowTemplate, base.transform);
					TMP_Text componentInChildren = gameObject.GetComponentInChildren<TMP_Text>();
					componentInChildren.text = "<s>" + str2 + "</s>";
					componentInChildren.GetComponent<LinkOpener>().strDefaultURL = "coid:" + str;
				}
				if (!string.IsNullOrEmpty(jsonPlotSave.strCurrentBeat) && (jsonPlotSave.strCurrentBeat != text || jsonPlotSave.aCompletedBeats.Length > 0))
				{
					GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(this._rowTemplate, base.transform);
					TMP_Text componentInChildren2 = gameObject2.GetComponentInChildren<TMP_Text>();
					if (jsonPlotSave.nPhase >= plot.aPhases.Length)
					{
						componentInChildren2.text = "<s>" + jsonPlotSave.strCurrentBeat + "</s>";
					}
					else
					{
						componentInChildren2.text = jsonPlotSave.strCurrentBeat;
					}
					componentInChildren2.GetComponent<LinkOpener>().strDefaultURL = "coid:" + str;
				}
			}
			base.SetVisibility(showClose, false);
		}

		public void SetData(Objective obj, UnityAction closeButtonCallback)
		{
			base.SetData(obj, closeButtonCallback, false);
			this.SetData(obj, true, false);
		}

		public void DelayedVisibility()
		{
			if (this._cg != null)
			{
				this._cg.alpha = 1f;
			}
			ObjectivesHost.OnObjectiveAnimationFinished.RemoveListener(new UnityAction(this.DelayedVisibility));
		}

		[Header("Required Parameters")]
		[SerializeField]
		private GameObject _rowTemplate;
	}
}
