using System;
using System.Collections.Generic;
using Ostranauts.Events;
using Ostranauts.UI.MegaToolTip.DataModules;
using Ostranauts.UI.MegaToolTip.Interfaces;
using UnityEngine;
using UnityEngine.Events;

namespace Ostranauts.UI.MegaToolTip
{
	public class ModuleHost : MonoBehaviour
	{
		public static bool ShowExpandedTooltip { get; private set; }

		private void Awake()
		{
			if (TooltipPreviewButton.OnPreviewButtonClicked == null)
			{
				TooltipPreviewButton.OnPreviewButtonClicked = new OnTooltipPreviewButtonClickedEvent();
			}
			TooltipPreviewButton.OnPreviewButtonClicked.AddListener(new UnityAction<CondOwner>(this.OnSelectionChanged));
			if (ModuleHost.ToggleShowMore == null)
			{
				ModuleHost.ToggleShowMore = new UnityEvent();
			}
			ModuleHost.ToggleShowMore.AddListener(new UnityAction(this.OnShowMorePressed));
			ModuleHost.anim = base.GetComponent<Animator>();
			ModuleHost.anim.updateMode = AnimatorUpdateMode.UnscaledTime;
			ModuleHost.ShowExpandedTooltip = false;
			this.Hide();
		}

		private void Update()
		{
			if (!ModuleHost.Opened || Time.unscaledTime - this.fTimeLastUpdate < 0.5f)
			{
				return;
			}
			this.fTimeLastUpdate = Time.unscaledTime;
			if (this.IsCOInvalid())
			{
				CrewSim.OnRightClick.Invoke(null);
				return;
			}
			ModuleHost.UpdateUI.Invoke();
		}

		private void OnSelectionChanged(CondOwner co)
		{
			this.SetData(co);
		}

		private void OnShowMorePressed()
		{
			ModuleHost.ShowExpandedTooltip = !ModuleHost.ShowExpandedTooltip;
			this.SetData(ModuleHost._co);
		}

		private void OnDestroy()
		{
			TooltipPreviewButton.OnPreviewButtonClicked.RemoveListener(new UnityAction<CondOwner>(this.OnSelectionChanged));
			ModuleHost.ToggleShowMore.RemoveListener(new UnityAction(this.OnShowMorePressed));
		}

		private bool IsCOInvalid()
		{
			if (ModuleHost._co == null)
			{
				if (string.IsNullOrEmpty(this._coStrId))
				{
					return true;
				}
				CondOwner data;
				if (DataHandler.mapCOs.TryGetValue(this._coStrId, out data))
				{
					this.SetData(data);
					return false;
				}
				this._coStrId = string.Empty;
				return true;
			}
			else
			{
				if (!CrewSim.inventoryGUI.IsInventoryVisible && !ModuleHost._co.Visible)
				{
					ModuleHost._co = null;
					return true;
				}
				return false;
			}
		}

		private void SetData(CondOwner co)
		{
			ModuleHost._co = co;
			this._coStrId = ((!(ModuleHost._co != null)) ? string.Empty : ModuleHost._co.strID);
			foreach (IDataModule dataModule in this._dataModules)
			{
				dataModule.Destroy();
			}
			this._dataModules.Clear();
			if (co == null)
			{
				this.Hide();
				return;
			}
			GameObject[] array = (!(co.strType == "Item")) ? this._personModulePrefabs : this._itemModulePrefabs;
			bool flag = false;
			for (int i = 0; i < array.Length; i++)
			{
				if (flag && !ModuleHost.ShowExpandedTooltip)
				{
					break;
				}
				GameObject original = array[i];
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(original, this._moduleContainer);
				IDataModule component = gameObject.GetComponent<IDataModule>();
				component.SetData(co);
				if (component.IsMarkedForDestroy())
				{
					component.Destroy();
				}
				else
				{
					this._dataModules.Add(component);
					if (gameObject.GetComponent<ToggleMoreModule>() != null)
					{
						flag = true;
					}
				}
			}
			this.Show();
			this.fTimeLastUpdate = Time.unscaledTime;
		}

		public void Show()
		{
			ModuleHost.anim.SetInteger("AnimState", 1);
		}

		public void Hide()
		{
			ModuleHost.anim.SetInteger("AnimState", 0);
		}

		public static bool Opened
		{
			get
			{
				return ModuleHost.anim.GetInteger("AnimState") == 1;
			}
		}

		public static void Rename(string name)
		{
			if (ModuleHost._co == null)
			{
				return;
			}
			ModuleHost._co.Rename(name);
		}

		public static UnityEvent UpdateUI = new UnityEvent();

		public static UnityEvent ToggleShowMore = new UnityEvent();

		[SerializeField]
		private GameObject[] _itemModulePrefabs;

		[SerializeField]
		private GameObject[] _personModulePrefabs;

		[SerializeField]
		private Transform _moduleContainer;

		private float fTimeLastUpdate;

		private static CondOwner _co;

		private string _coStrId = string.Empty;

		private List<IDataModule> _dataModules = new List<IDataModule>();

		private static Animator anim;
	}
}
