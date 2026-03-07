using System;
using Ostranauts.Events;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.Objectives
{
	public class ObjectiveHighlighter : MonoBehaviour
	{
		private void Awake()
		{
			if (ObjectivePanel.OnHighlightObjective == null)
			{
				ObjectivePanel.OnHighlightObjective = new OnObjectiveUpdatedEvent();
			}
		}

		private void Start()
		{
			this._backgroundImgCG.alpha = 0f;
			this._cgArrow.alpha = 0f;
			this._cgNotFound.alpha = 0f;
			ObjectivePanel.OnHighlightObjective.AddListener(new UnityAction<Objective>(this.Highlight));
		}

		private void OnDestroy()
		{
			ObjectivePanel.OnHighlightObjective.RemoveListener(new UnityAction<Objective>(this.Highlight));
		}

		private void Highlight(Objective obj)
		{
			if (obj == null)
			{
				return;
			}
			this._trackedObj = obj;
			this._triggered = true;
		}

		private void Update()
		{
			this._cgArrow.alpha = 0f;
			this._cgNotFound.alpha = 0f;
			if (!this._triggered)
			{
				return;
			}
			CondOwner cofocus = this._trackedObj.COFocus;
			if (cofocus != null)
			{
				if (!cofocus.HighlightObjective)
				{
					foreach (string key in this._trackedObj.aLinkedCOs)
					{
						if (DataHandler.mapCOs.TryGetValue(key, out cofocus) && cofocus.HighlightObjective)
						{
							break;
						}
					}
				}
				if (cofocus && cofocus.HighlightObjective)
				{
					if (cofocus.ship != null && cofocus.ship.LoadState < Ship.Loaded.Edit)
					{
						this._cgNotFound.alpha = 1f;
					}
					else if (cofocus != CrewSim.coPlayer)
					{
						this._cgArrow.alpha = 1f;
						Vector3 a = this._canvasScaler.WorldToCanvasSpace(cofocus.tf.position);
						Vector3 vector = new Vector3(-25f, 52f, 0f);
						Vector3 vector2 = a - vector;
						float num = Mathf.InverseLerp(300f, 200f, vector2.magnitude);
						bool flag = vector2.magnitude > 175f;
						float target = 0f;
						if (flag)
						{
							target = 1f;
						}
						this.diff = Mathf.SmoothDamp(this.diff, target, ref this.changeVel, 0.1f);
						float d = 50f;
						if (cofocus.Item)
						{
							d = (float)(cofocus.Item.rend.material.mainTexture.height + 25);
						}
						Vector3 target2 = (!flag) ? (a + Vector3.up * d) : vector;
						this._cgArrow.transform.localPosition = Vector3.SmoothDamp(this._cgArrow.transform.localPosition, target2, ref this.arrowVel, 0.1f);
						Vector3 vector3 = a - this._cgArrow.transform.localPosition;
						float angle = Mathf.Atan2(-vector3.x, vector3.y) * 57.295776f;
						this._cgArrow.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
					}
				}
			}
			if (this._trackedObj != null && this._trackedObj.Highlight && !CrewSim.bRaiseUI)
			{
				this._backgroundImgCG.alpha = 0.95f;
				CrewSim.objInstance.camHighlight.gameObject.SetActive(true);
				CrewSim.objInstance.camHighlight.orthographicSize = CrewSim.objInstance.camMain.orthographicSize;
			}
			else
			{
				this._trackedObj = null;
				this._backgroundImgCG.alpha = 0f;
				CrewSim.objInstance.camHighlight.gameObject.SetActive(false);
				this._triggered = false;
			}
		}

		[SerializeField]
		private CanvasGroup _backgroundImgCG;

		[SerializeField]
		private CanvasGroup _cgNotFound;

		[SerializeField]
		private CanvasGroup _cgArrow;

		[SerializeField]
		private CanvasScaler _canvasScaler;

		[SerializeField]
		private TMP_Text _txtNotFound;

		private Objective _trackedObj;

		private bool _triggered;

		private float changeVel;

		private float diff = 1f;

		private Vector3 arrowVel;
	}
}
