using System;
using System.Collections;
using Ostranauts.Events;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.UI.ShipEdit
{
	public class GUIShipEditStageEntry : MonoBehaviour
	{
		private void Awake()
		{
			if (GUIShipEditStageEntry.OnPreviewClicked == null)
			{
				GUIShipEditStageEntry.OnPreviewClicked = new OnShipEditStageEvent();
			}
			GUIShipEditStageEntry.OnPreviewClicked.AddListener(new UnityAction<int>(this.Highlight));
			this.btnSave.onClick.AddListener(new UnityAction(this.OnSaveClicked));
			this.btnDelete.onClick.AddListener(new UnityAction(this.OnDeleteClicked));
			this.btnPreview.onClick.AddListener(new UnityAction(this.OnPreview));
			this.tboxProgress.onSelect.AddListener(delegate(string A_0)
			{
				CrewSim.StartTyping();
			});
			this.tboxProgress.onDeselect.AddListener(delegate(string A_0)
			{
				CrewSim.EndTyping();
			});
			GUIEnterExitHandler component = this.tboxProgress.GetComponent<GUIEnterExitHandler>();
			if (component != null)
			{
				component.SetDelegates(delegate
				{
					CrewSim.Typing = true;
				}, delegate
				{
					CrewSim.Typing = false;
				});
			}
		}

		private void OnDestroy()
		{
			if (GUIShipEditStageEntry.OnPreviewClicked != null)
			{
				GUIShipEditStageEntry.OnPreviewClicked.RemoveListener(new UnityAction<int>(this.Highlight));
			}
		}

		public void SetData(int highestExistingValue)
		{
			this.goSaved.SetActive(false);
			this.goNew.SetActive(true);
			int num = highestExistingValue - 10;
			num = ((num >= 0) ? num : 0);
			this.tboxProgress.text = num.ToString("N0");
		}

		public void SetData(string name, JsonShipConstructionTemplate jTemplate, bool delayImage = false)
		{
			if (jTemplate == null)
			{
				return;
			}
			string text = (jTemplate.nProgress != 100) ? ("_" + jTemplate.nProgress.ToString("N0")) : string.Empty;
			string imgName = string.Concat(new string[]
			{
				"ships/",
				name,
				"/",
				name,
				text,
				".png"
			});
			base.StartCoroutine(this.DelayedImgLoad(imgName, delayImage));
			this.goSaved.SetActive(true);
			this.goNew.SetActive(false);
			this.txtProgress.text = "Construction Stage: " + jTemplate.nProgress.ToString("N0");
			if (jTemplate.nProgress == 100)
			{
				this.btnDelete.gameObject.SetActive(false);
			}
			if (jTemplate.aItems != null)
			{
				this.txtPartCount.text = "Parts Count: " + jTemplate.aItems.Length;
			}
			if (CrewSim.jsonShip != null && CrewSim.jsonShip.nConstructionProgress == jTemplate.nProgress)
			{
				Image component = this.goSaved.GetComponent<Image>();
				component.color = Color.white;
			}
			else
			{
				Image component2 = this.goSaved.GetComponent<Image>();
				component2.color = new Color(0f, 0f, 0f, 0f);
			}
			this._jTemplate = jTemplate;
		}

		private void Highlight(int stage)
		{
			if (this._jTemplate == null || this._jTemplate.nProgress != stage)
			{
				Image component = this.goSaved.GetComponent<Image>();
				component.color = new Color(0f, 0f, 0f, 0f);
			}
			else
			{
				Image component2 = this.goSaved.GetComponent<Image>();
				component2.color = Color.white;
			}
		}

		private void OnSaveClicked()
		{
			int num;
			if (int.TryParse(this.tboxProgress.text, out num) && num < 100 && num >= 0)
			{
				GUIShipEdit.OnShipEditStageSaveEvent.Invoke(num);
			}
		}

		private void OnPreview()
		{
			CrewSim.jsonShip.nConstructionProgress = ((this._jTemplate != null) ? this._jTemplate.nProgress : 100);
			string strRegID = CrewSim.shipCurrentLoaded.strRegID;
			if (strRegID != null)
			{
				CrewSim.system.dictShips.Remove(strRegID);
			}
			GUIShipEditStageEntry.OnPreviewClicked.Invoke(CrewSim.jsonShip.nConstructionProgress);
			if (CrewSim.jsonShip.nConstructionProgress == 100)
			{
				CrewSim.jsonShip = DataHandler.GetShip(CrewSim.jsonShip.strName);
			}
			CrewSim.objInstance.StartShipEdit();
		}

		private void OnDeleteClicked()
		{
			if (this._jTemplate == null)
			{
				return;
			}
			GUIShipEdit.OnShipEditStageDeleteEvent.Invoke(this._jTemplate.nProgress);
		}

		private IEnumerator DelayedImgLoad(string imgName, bool delay)
		{
			float delayTime = (!delay) ? 0f : 0.5f;
			yield return new WaitForSeconds(delayTime);
			Texture2D text = DataHandler.LoadPNG(imgName, false, true);
			this.previewImage.texture = text;
			yield break;
		}

		private static OnShipEditStageEvent OnPreviewClicked;

		[SerializeField]
		private GameObject goSaved;

		[SerializeField]
		private GameObject goNew;

		[SerializeField]
		private Button btnDelete;

		[SerializeField]
		private Button btnSave;

		[SerializeField]
		private Button btnPreview;

		[SerializeField]
		private TMP_Text txtPartCount;

		[SerializeField]
		private TMP_Text txtProgress;

		[SerializeField]
		private TMP_InputField tboxProgress;

		[SerializeField]
		public RawImage previewImage;

		private JsonShipConstructionTemplate _jTemplate;
	}
}
