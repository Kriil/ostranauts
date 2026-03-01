using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Events;
using UnityEngine;
using UnityEngine.Events;

namespace Ostranauts.UI.MegaToolTip
{
	public class GUIMegaToolTip : MonoBehaviour
	{
		private void Awake()
		{
			this._fadeOut.gameObject.SetActive(false);
			if (this._cgParent == null)
			{
				this._cgParent = base.GetComponentInParent<CanvasGroup>();
			}
			this._cgParent.alpha = 0f;
		}

		private void Start()
		{
			if (TooltipPreviewButton.OnPreviewButtonClicked == null)
			{
				TooltipPreviewButton.OnPreviewButtonClicked = new OnTooltipPreviewButtonClickedEvent();
			}
			CrewSim.OnRightClick.AddListener(new UnityAction<List<CondOwner>>(this.OnSelectionUpdated));
		}

		private void OnDestroy()
		{
			CrewSim.OnRightClick.RemoveListener(new UnityAction<List<CondOwner>>(this.OnSelectionUpdated));
		}

		private void OnSelectionUpdated(List<CondOwner> clickedCOs)
		{
			this._cgParent.alpha = (float)((clickedCOs != null) ? 1 : 0);
			if (clickedCOs == null || GUIMegaToolTip._selectedCOsDict.Count == 0 || this.HasSelectionChanged(clickedCOs))
			{
				this.ClearDict();
				GUIMegaToolTip.Selected = this.BuildButtonList(clickedCOs);
				CrewSim.objInstance.LowerContextMenu();
			}
			else
			{
				string[] array = GUIMegaToolTip._selectedCOsDict.Keys.ToArray<string>();
				int num = Array.IndexOf<string>(array, GUIMegaToolTip._selectedCO.strID);
				string selectedID = (num + 1 < array.Length) ? array[num + 1] : array[0];
				GUIMegaToolTip.Selected = clickedCOs.FirstOrDefault((CondOwner x) => x.strID == selectedID);
			}
			TooltipPreviewButton.OnPreviewButtonClicked.Invoke(GUIMegaToolTip.Selected);
		}

		private CondOwner BuildButtonList(List<CondOwner> clickedCOs)
		{
			if (clickedCOs == null)
			{
				return null;
			}
			RectTransform component = this._buttonContainer.GetComponent<RectTransform>();
			component.anchoredPosition = new Vector2(component.anchoredPosition.x, 0f);
			int num = 0;
			int num2 = 0;
			foreach (CondOwner condOwner in clickedCOs)
			{
				num++;
				TooltipPreviewButton component2 = UnityEngine.Object.Instantiate<GameObject>(this._selectionButtonPrefab, this._buttonContainer).GetComponent<TooltipPreviewButton>();
				component2.SetData(condOwner);
				if (num == 6)
				{
					num2 = component2.LabelName.Length;
				}
				GUIMegaToolTip._selectedCOsDict[condOwner.strID] = component2.gameObject;
			}
			if (clickedCOs.Count > 5)
			{
				float x = Mathf.Max(0f, 0.7f - (float)num2 * 0.052f);
				this._fadeOut.anchorMin = new Vector2(x, this._fadeOut.anchorMin.y);
				this._fadeOut.anchoredPosition = new Vector2(0f, this._fadeOut.anchoredPosition.y);
				this._fadeOut.gameObject.SetActive(true);
			}
			return clickedCOs.FirstOrDefault<CondOwner>();
		}

		private bool HasSelectionChanged(List<CondOwner> condOwners)
		{
			if (GUIMegaToolTip._selectedCOsDict.Count != condOwners.Count<CondOwner>())
			{
				return true;
			}
			using (List<CondOwner>.Enumerator enumerator = condOwners.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					CondOwner co = enumerator.Current;
					if (!GUIMegaToolTip._selectedCOsDict.Any((KeyValuePair<string, GameObject> kvp) => kvp.Key == co.strID))
					{
						return true;
					}
				}
			}
			return false;
		}

		private void ClearDict()
		{
			foreach (KeyValuePair<string, GameObject> keyValuePair in GUIMegaToolTip._selectedCOsDict)
			{
				UnityEngine.Object.Destroy(keyValuePair.Value);
			}
			this._fadeOut.gameObject.SetActive(false);
			GUIMegaToolTip._selectedCOsDict.Clear();
		}

		public static CondOwner Selected
		{
			get
			{
				return GUIMegaToolTip._selectedCO;
			}
			set
			{
				if (value == null || GUIMegaToolTip._selectedCOsDict == null)
				{
					if (GUIMegaToolTip._selectedCO != null)
					{
						GUIMegaToolTip._selectedCO.SetHighlight(0f);
					}
					GUIMegaToolTip._selectedCO = value;
					return;
				}
				bool flag = GUIMegaToolTip._selectedCOsDict.Any((KeyValuePair<string, GameObject> kvp) => kvp.Key == value.strID);
				if (flag)
				{
					if (GUIMegaToolTip._selectedCO != null)
					{
						GUIMegaToolTip._selectedCO.SetHighlight(0f);
					}
					GUIMegaToolTip._selectedCO = value;
					GUIMegaToolTip._selectedCO.SetHighlight(1f);
				}
			}
		}

		[SerializeField]
		private GameObject _selectionButtonPrefab;

		[SerializeField]
		private Transform _buttonContainer;

		[SerializeField]
		private RectTransform _fadeOut;

		private CanvasGroup _cgParent;

		private static Dictionary<string, GameObject> _selectedCOsDict = new Dictionary<string, GameObject>();

		private static CondOwner _selectedCO;
	}
}
