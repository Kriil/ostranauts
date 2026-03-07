using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Core;
using UnityEngine;

namespace Ostranauts.UI.CrewBar
{
	public class GUICrewCardHost : MonoBehaviour
	{
		private void Update()
		{
			if (Time.unscaledTime - this._lastUpdateTimestamp < 2f || CrewSim.coPlayer == null)
			{
				return;
			}
			this._lastUpdateTimestamp = Time.unscaledTime;
			JsonCompany company = CrewSim.coPlayer.Company;
			if (company == null)
			{
				return;
			}
			int count = this._cards.Count;
			CondOwner selected = CrewSim.GetSelectedCrew();
			List<CondOwner> crewMembers = company.GetCrewMembers(null);
			this.SyncCrewCards((!crewMembers.Any((CondOwner x) => x.strID == selected.strID)) ? new List<CondOwner>
			{
				selected
			} : crewMembers);
			if (count != 0 && count < this._cards.Count)
			{
				if (this._cg.alpha < 1f)
				{
					MonoSingleton<GUICrewStatus>.Instance.ToggleCards(false);
				}
				else
				{
					this.Show(true);
				}
			}
		}

		private void SyncCrewCards(List<CondOwner> crew)
		{
			if (crew == null)
			{
				return;
			}
			List<string> list = this._cards.Keys.ToList<string>();
			foreach (CondOwner condOwner in crew)
			{
				if (list.Contains(condOwner.strID))
				{
					list.Remove(condOwner.strID);
					this._cards[condOwner.strID].Refresh();
				}
				else
				{
					this.AddCard(condOwner);
				}
			}
			foreach (string coId in list)
			{
				this.RemoveCrew(coId);
			}
			GUICrewCard guicrewCard = null;
			this._cards.TryGetValue(CrewSim.GetSelectedCrew().strID, out guicrewCard);
			Transform child = this._tfContent.GetChild(0);
			if (guicrewCard != null && guicrewCard.gameObject != child.gameObject)
			{
				Vector2 anchoredPosition = (child as RectTransform).anchoredPosition;
				child.SetParent(this._tfScrollRectContent);
				guicrewCard.transform.SetParent(this._tfContent);
				guicrewCard.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 104f);
				guicrewCard.GetComponent<RectTransform>().anchoredPosition = new Vector2(anchoredPosition.x, -104f);
			}
		}

		public void Show(bool show)
		{
			this._cg.alpha = (float)((!show) ? 0 : 1);
			this._cg.interactable = show;
			this._cg.blocksRaycasts = show;
			this._lblNoCrew.SetActive(this._cards.Count <= 1);
		}

		private void AddCard(CondOwner co)
		{
			GUICrewCard guicrewCard = null;
			if (!this._cards.TryGetValue(co.strID, out guicrewCard) || guicrewCard == null)
			{
				Transform parent = (!(CrewSim.GetSelectedCrew().strID == co.strID)) ? this._tfScrollRectContent : this._tfContent;
				guicrewCard = UnityEngine.Object.Instantiate<GUICrewCard>(this._guiCrewCardPrefab, parent);
				this._cards[co.strID] = guicrewCard;
			}
			guicrewCard.SetData(co);
		}

		private void RemoveCrew(string coId)
		{
			GUICrewCard guicrewCard = null;
			if (this._cards.TryGetValue(coId, out guicrewCard))
			{
				if (guicrewCard != null)
				{
					UnityEngine.Object.Destroy(guicrewCard.gameObject);
				}
				this._cards.Remove(coId);
			}
		}

		public void Refresh()
		{
			this._lastUpdateTimestamp = 0f;
		}

		[SerializeField]
		private GUICrewCard _guiCrewCardPrefab;

		[SerializeField]
		private Transform _tfContent;

		[SerializeField]
		private Transform _tfScrollRectContent;

		[SerializeField]
		private CanvasGroup _cg;

		[SerializeField]
		private GameObject _lblNoCrew;

		private Dictionary<string, GUICrewCard> _cards = new Dictionary<string, GUICrewCard>();

		private float _lastUpdateTimestamp;
	}
}
