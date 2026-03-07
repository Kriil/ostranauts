using System;
using System.Collections.Generic;
using Ostranauts.Core;
using Ostranauts.UI.MegaToolTip;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Hover item-list overlay. Likely aggregates CondOwners under the cursor and
// builds the stacked tooltip/atmosphere summary used in the main ship view.
public class GUIItemList : MonoSingleton<GUIItemList>
{
	// Unity setup: caches the parent canvas sorting order used by pooled tooltip rows.
	private new void Awake()
	{
		base.Awake();
		if (this.parentCanvasRef != null)
		{
			this.m_sortingOrder = this.parentCanvasRef.sortingOrder;
		}
	}

	// Prebuilds a pool of item tooltip rows so hover updates avoid repeated instantiation.
	private void Start()
	{
		for (int i = 0; i < this.m_maxItems; i++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(this.prfb_GuiItemTooltip3, this.tf_Pool);
			this.m_items.Add(gameObject.GetComponent<GUIItemToolTip>());
			gameObject.SetActive(false);
		}
	}

	// Per-frame hover scan: gathers mouse-over CondOwners, handles exterior/atmo
	// fallback, and refreshes the visible tooltip stack.
	private void Update()
	{
		if (!DataHandler.bLoaded)
		{
			return;
		}
		if (!this.m_init)
		{
			this.Init();
		}
		this.m_aCOs.Clear();
		this.m_N2Danger = false;
		this.m_NH3Danger = false;
		this.m_CH4Danger = false;
		this.m_O2Danger = false;
		this.m_CO2Danger = false;
		this.m_H2SO4Danger = false;
		this.m_TempDanger = false;
		this.m_TempNone = false;
		bool flag = EventSystem.current.IsPointerOverGameObject();
		if (this._showAsyncShip || ((CrewSim.CanvasManager.State == CanvasManager.GUIState.NORMAL || CrewSim.CanvasManager.State == CanvasManager.GUIState.SHIPEDIT) && !flag))
		{
			if (CrewSim.shipCurrentLoaded == null)
			{
				return;
			}
			this.m_position = Input.mousePosition;
			float num = 1080f / (float)Screen.height;
			this.rectTf.anchoredPosition = new Vector2(Input.mousePosition.x * num + this.m_offset.x, Input.mousePosition.y * num + this.m_offset.y);
			if (this._showAsyncShip)
			{
				this.m_position = this.CalculatedMousePosition;
			}
			this.m_aCOs.AddRange(CrewSim.objInstance.GetMouseOverCOsExternal(new Vector3?(this.m_position)));
			if (this.txt_pressureList.gameObject.activeSelf == this._showAsyncShip)
			{
				this.txt_pressureList.gameObject.SetActive(!this._showAsyncShip);
			}
			if (!this._showAsyncShip && this.m_aCOs.Count == 0 && this._exteriorCO != null)
			{
				this.m_aCOs.Add(this._exteriorCO);
			}
		}
		else
		{
			if (this.txt_itemList.gameObject.activeSelf)
			{
				this.txt_itemList.text = string.Empty;
				this.txt_itemList.gameObject.SetActive(false);
			}
			if (this.txt_pressureList.gameObject.activeSelf)
			{
				this.txt_pressureList.text = string.Empty;
				this.txt_pressureList.gameObject.SetActive(false);
			}
			if (this.m_items[0] != null && this.m_items[0].gameObject.activeSelf)
			{
				for (int i = 0; i < this.m_items.Count; i++)
				{
					if (i < this.m_aCOs.Count && this.m_display && this.m_items[i] != null)
					{
						this.m_items[i].gameObject.SetActive(false);
					}
				}
			}
		}
		if (this.m_aCOs.Count > 0)
		{
			for (int j = this.m_aCOs.Count - 1; j >= 0; j--)
			{
				if (this.m_aCOs[j] == null || this.m_aCOs[j].strName == "TIL")
				{
					this.m_aCOs.RemoveAt(j);
				}
				else if (this.m_aCOs[j].HasCond("IsAirtight") || this.m_aCOs[j].strName == "Exterior")
				{
					if (this.m_aCOs[j].strName == "Exterior")
					{
						this._exteriorCO = this.m_aCOs[j];
					}
					this.m_N2Pressure = this.m_aCOs[j].GetCondAmount("StatGasPpN2");
					this.m_N2Danger = !this.m_aCOs[j].HasCond("DcGasPpN2");
					this.m_O2Pressure = this.m_aCOs[j].GetCondAmount("StatGasPpO2");
					this.m_O2Danger = !this.m_aCOs[j].HasCond("DcGasPpO2");
					this.m_CO2Pressure = this.m_aCOs[j].GetCondAmount("StatGasPpCO2");
					this.m_CO2Danger = !this.m_aCOs[j].HasCond("DcGasPpCO2");
					this.m_H2SO4Pressure = this.m_aCOs[j].GetCondAmount("StatGasPpH2SO4");
					this.m_H2SO4Danger = !this.m_aCOs[j].HasCond("DcGasPpH2SO4");
					this.m_CH4Pressure = this.m_aCOs[j].GetCondAmount("StatGasPpCH4");
					this.m_CH4Danger = !this.m_aCOs[j].HasCond("DcGasPpCH4");
					this.m_NH3Pressure = this.m_aCOs[j].GetCondAmount("StatGasPpNH3");
					this.m_NH3Danger = !this.m_aCOs[j].HasCond("DcGasPpNH3");
					this.m_TempNone = !this.m_aCOs[j].HasCond("StatGasTemp");
					this.m_RoomTemp = this.m_aCOs[j].GetCondAmount("StatGasTemp");
					this.m_TempDanger = !this.m_aCOs[j].HasCond("DcGasTemp02");
					if (this.m_aCOs[j].HasCond("IsRoom"))
					{
						this.m_aCOs.RemoveAt(j);
					}
				}
			}
		}
		else
		{
			this.m_CH4Pressure = 0.0;
			this.m_CH4Danger = false;
			this.m_NH3Pressure = 0.0;
			this.m_NH3Danger = false;
			this.m_N2Pressure = 0.0;
			this.m_N2Danger = false;
			this.m_O2Pressure = 0.0;
			this.m_O2Danger = false;
			this.m_CO2Pressure = 0.0;
			this.m_CO2Danger = false;
			this.m_H2SO4Pressure = 0.0;
			this.m_H2SO4Danger = false;
			this.m_TempDanger = false;
		}
		if (GUIActionKeySelector.commandToggleGasVis.Down)
		{
			this.m_ForceValues = !this.m_ForceValues;
		}
		if (!this.m_display)
		{
			if (!this.txt_itemList.gameObject.activeSelf)
			{
				this.txt_itemList.gameObject.SetActive(true);
			}
			this._itemList = "\n";
			for (int k = 0; k < this.m_aCOs.Count; k++)
			{
				if (!(this.m_aCOs[k] == null))
				{
					bool flag2 = false;
					if (this.m_aCOs[k] == GUIMegaToolTip.Selected)
					{
						this._itemList = this._itemList + "<color=#" + ColorUtility.ToHtmlStringRGB(DataHandler.GetColor("CursorSelected")) + ">[";
						flag2 = true;
					}
					if (this.m_aCOs[k].HasCond("IsHuman"))
					{
						this._itemList += this.m_aCOs[k].strName;
					}
					else if (this.m_aCOs[k].HasCond("IsDamaged"))
					{
						this._itemList = this._itemList + this.m_aCOs[k].ShortName + "~";
					}
					else
					{
						this._itemList += this.m_aCOs[k].ShortName;
					}
					if (flag2)
					{
						this._itemList += "]</color>";
					}
					this._itemList += "\n";
				}
			}
			if (this.txt_itemList.text != this._itemList)
			{
				this.txt_itemList.text = this._itemList;
			}
		}
		else
		{
			if (this.txt_itemList.gameObject.activeSelf)
			{
				this.txt_itemList.gameObject.SetActive(false);
			}
			this.tf_DamageContainer.anchoredPosition = this.rectTf.anchoredPosition;
			for (int l = 0; l < this.m_items.Count; l++)
			{
				if (!(this.m_items[l] == null))
				{
					if (l < this.m_aCOs.Count && this.m_display)
					{
						if (this.m_items[l].transform.parent != this.tf_DamageContainer)
						{
							this.m_items[l].transform.SetParent(this.tf_DamageContainer);
						}
						this.m_items[l].gameObject.SetActive(true);
						this.m_items[l].SetCondOwner(this.m_aCOs[l]);
					}
					else
					{
						this.m_items[l].gameObject.SetActive(false);
						if (this.m_items[l].transform.parent != this.tf_Pool)
						{
							this.m_items[l].transform.SetParent(this.tf_Pool);
						}
					}
				}
			}
		}
		this.m_positionLast -= this.m_position;
		this.labels = (this.m_positionLast.magnitude > this.m_minSpeed * Time.unscaledDeltaTime);
		this.m_positionLast = this.m_position;
		this._pressureList = string.Empty;
		if (this.labels)
		{
			if (this.m_N2Danger || this.m_ForceValues)
			{
				this._pressureList = this._pressureList + "\n<color=" + this.m_N2ColorHash + ">N2</color>\n";
			}
			if (this.m_O2Danger || this.m_ForceValues)
			{
				this._pressureList = this._pressureList + "<color=" + this.m_O2ColorHash + ">O2</color>\n";
			}
			if (this.m_CO2Danger || this.m_ForceValues)
			{
				this._pressureList = this._pressureList + "<color=" + this.m_CO2ColorHash + ">CO2</color>\n";
			}
			if (this.m_H2SO4Danger || this.m_ForceValues)
			{
				this._pressureList = this._pressureList + "<color=" + this.m_H2SO4ColorHash + ">H2SO4</color>\n";
			}
			if (this.m_NH3Danger || this.m_ForceValues)
			{
				this._pressureList = this._pressureList + "<color=" + this.m_NH3ColorHash + ">NH3</color>\n";
			}
			if (this.m_CH4Danger || this.m_ForceValues)
			{
				this._pressureList = this._pressureList + "<color=" + this.m_CH4ColorHash + ">CH4</color>\n";
			}
			if (this.m_TempDanger || this.m_ForceValues)
			{
				this._pressureList = this._pressureList + "<color=" + this.m_TempColorHash + ">TEMP</color>";
			}
		}
		else
		{
			if (this.m_N2Danger || this.m_ForceValues)
			{
				string pressureList = this._pressureList;
				this._pressureList = string.Concat(new string[]
				{
					pressureList,
					"\n<color=",
					this.m_N2ColorHash,
					">",
					this.m_N2Pressure.ToString("n2"),
					" KPa</color>\n"
				});
			}
			if (this.m_O2Danger || this.m_ForceValues)
			{
				string pressureList = this._pressureList;
				this._pressureList = string.Concat(new string[]
				{
					pressureList,
					"<color=",
					this.m_O2ColorHash,
					">",
					this.m_O2Pressure.ToString("n2"),
					" KPa</color>\n"
				});
			}
			if (this.m_CO2Danger || this.m_ForceValues)
			{
				string pressureList = this._pressureList;
				this._pressureList = string.Concat(new string[]
				{
					pressureList,
					"<color=",
					this.m_CO2ColorHash,
					"> ",
					this.m_CO2Pressure.ToString("n2"),
					" KPa</color>\n"
				});
			}
			if (this.m_H2SO4Danger || this.m_ForceValues)
			{
				string pressureList = this._pressureList;
				this._pressureList = string.Concat(new string[]
				{
					pressureList,
					"<color=",
					this.m_H2SO4ColorHash,
					"> ",
					this.m_H2SO4Pressure.ToString("n2"),
					" KPa</color>\n"
				});
			}
			if (this.m_NH3Danger || this.m_ForceValues)
			{
				string pressureList = this._pressureList;
				this._pressureList = string.Concat(new string[]
				{
					pressureList,
					"<color=",
					this.m_NH3ColorHash,
					">",
					this.m_NH3Pressure.ToString("n2"),
					" KPa</color>\n"
				});
			}
			if (this.m_CH4Danger || this.m_ForceValues)
			{
				string pressureList = this._pressureList;
				this._pressureList = string.Concat(new string[]
				{
					pressureList,
					"<color=",
					this.m_CH4ColorHash,
					">",
					this.m_CH4Pressure.ToString("n2"),
					" KPa</color>\n"
				});
			}
			if (this.m_TempNone)
			{
				this._pressureList = this._pressureList + "<color=" + this.m_TempColorHash + "> NONE</color>";
			}
			else if (this.m_TempDanger || this.m_ForceValues)
			{
				string pressureList = this._pressureList;
				this._pressureList = string.Concat(new string[]
				{
					pressureList,
					"<color=",
					this.m_TempColorHash,
					"> ",
					MathUtils.GetTemperatureString(this.m_RoomTemp),
					"</color>\n"
				});
			}
		}
		if (this.txt_pressureList.text != this._pressureList)
		{
			this.txt_pressureList.text = this._pressureList;
		}
	}

	public void ToggleDmg(bool bShow)
	{
		if (this.m_display == bShow)
		{
			return;
		}
		this.m_display = bShow;
		if (this.m_display)
		{
			this.cg.alpha = 1f;
			this.cg_DamageContainer.alpha = 1f;
			foreach (Ship ship in CrewSim.GetAllLoadedShips())
			{
				ship.VisualizeOverlays(true);
			}
		}
		else
		{
			this.cg_DamageContainer.alpha = 0f;
			foreach (Ship ship2 in CrewSim.GetAllLoadedShips())
			{
				ship2.VisualizeOverlays(true);
			}
			for (int i = 0; i < this.m_items.Count; i++)
			{
				if (this.m_items[i] != null)
				{
					this.m_items[i].gameObject.SetActive(false);
				}
			}
		}
	}

	private void Init()
	{
		if (this.m_init)
		{
			return;
		}
		if (CrewSim.objInstance == null)
		{
			return;
		}
		Color color = DataHandler.GetColor("N2Blue");
		this.m_N2ColorHash = "#" + ColorUtility.ToHtmlStringRGB(color);
		color = DataHandler.GetColor("O2Green");
		this.m_O2ColorHash = "#" + ColorUtility.ToHtmlStringRGB(color);
		color = DataHandler.GetColor("CO2White");
		this.m_CO2ColorHash = "#" + ColorUtility.ToHtmlStringRGB(color);
		color = DataHandler.GetColor("H2SO4Yellow");
		this.m_H2SO4ColorHash = "#" + ColorUtility.ToHtmlStringRGB(color);
		color = DataHandler.GetColor("CH4BlueGreen");
		this.m_CH4ColorHash = "#" + ColorUtility.ToHtmlStringRGB(color);
		color = DataHandler.GetColor("NH3Beige");
		this.m_NH3ColorHash = "#" + ColorUtility.ToHtmlStringRGB(color);
		color = DataHandler.GetColor("TempRed");
		this.m_TempColorHash = "#" + ColorUtility.ToHtmlStringRGB(color);
		this.m_init = true;
	}

	public void ShowAsyncShipTooltip()
	{
		if (this.parentCanvasRef != null)
		{
			this.parentCanvasRef.sortingOrder = 900;
		}
		this._showAsyncShip = true;
	}

	public void DisableAsyncMode()
	{
		if (this.parentCanvasRef != null)
		{
			this.parentCanvasRef.sortingOrder = this.m_sortingOrder;
		}
		this._showAsyncShip = false;
	}

	[SerializeField]
	private CanvasGroup cg;

	[SerializeField]
	private CanvasGroup cg_DamageContainer;

	[SerializeField]
	private RectTransform rectTf;

	[SerializeField]
	private Canvas parentCanvasRef;

	[SerializeField]
	private GameObject prfb_GuiItemTooltip3;

	[SerializeField]
	private RectTransform tf_DamageContainer;

	[SerializeField]
	private RectTransform tf_Pool;

	[SerializeField]
	private TextMeshProUGUI txt_itemList;

	[SerializeField]
	private TextMeshProUGUI txt_pressureList;

	public bool m_display;

	public bool m_init;

	public int m_maxItems = 10;

	public Vector2 m_offset = new Vector2(5f, -5f);

	public Vector3 m_position = new Vector3(0f, 0f, 0f);

	public Vector3 m_positionLast = new Vector3(0f, 0f, 0f);

	public float m_minSpeed = 5f;

	private readonly List<GUIItemToolTip> m_items = new List<GUIItemToolTip>();

	private readonly List<CondOwner> m_aCOs = new List<CondOwner>();

	private double m_NH3Pressure;

	private double m_CH4Pressure;

	private double m_N2Pressure;

	private double m_O2Pressure;

	private double m_CO2Pressure;

	private double m_H2SO4Pressure;

	private double m_RoomTemp;

	private string m_N2ColorHash = "0000ff";

	private string m_NH3ColorHash = "00ffff";

	private string m_CH4ColorHash = "ff00ff";

	private string m_O2ColorHash = "00ff00";

	private string m_CO2ColorHash = "ffffff";

	private string m_H2SO4ColorHash = "ffffff";

	private string m_TempColorHash = "ff0000";

	private int m_sortingOrder;

	public bool m_ForceValues;

	private bool m_N2Danger;

	private bool m_NH3Danger;

	private bool m_CH4Danger;

	private bool m_O2Danger;

	private bool m_CO2Danger;

	private bool m_H2SO4Danger;

	private bool m_TempDanger;

	private bool m_TempNone;

	private bool labels = true;

	[NonSerialized]
	public Vector3 CalculatedMousePosition = Vector3.zero;

	private bool _showAsyncShip;

	private CondOwner _exteriorCO;

	private string _itemList = string.Empty;

	private string _pressureList = string.Empty;
}
