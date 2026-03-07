using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Ostranauts.GUIInventory
{
	public class GUIInventoryList : MonoBehaviour
	{
		private void Awake()
		{
			this._btnClose.onClick.AddListener(new UnityAction(this.CloseWindow));
		}

		public void SetData(IEnumerable<CondOwner> cos)
		{
			if (cos == null)
			{
				return;
			}
			foreach (CondOwner condOwner in cos)
			{
				GUIInventoryListElement guiinventoryListElement;
				if (this._dictsocialMoves.TryGetValue(condOwner.strName, out guiinventoryListElement))
				{
					guiinventoryListElement.IncreaseCount();
				}
				else if (!condOwner.HasCond("IsHiddenInv"))
				{
					Texture image = DataHandler.LoadPNG(condOwner.strPortraitImg + ".png", false, false);
					GUIInventoryListElement component = UnityEngine.Object.Instantiate<GameObject>(this._prefabInventoryListElement, this._scrollViewContainer).GetComponent<GUIInventoryListElement>();
					component.SetData(condOwner.FriendlyName, image);
					this._dictsocialMoves.Add(condOwner.strName, component);
				}
			}
		}

		private void CloseWindow()
		{
			base.gameObject.SetActive(false);
		}

		[SerializeField]
		private Transform _scrollViewContainer;

		[SerializeField]
		private Button _btnClose;

		[SerializeField]
		private GameObject _prefabInventoryListElement;

		private readonly Dictionary<string, GUIInventoryListElement> _dictsocialMoves = new Dictionary<string, GUIInventoryListElement>();
	}
}
