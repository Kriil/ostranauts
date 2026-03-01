using System;
using Ostranauts.Core.Models;
using Ostranauts.UI.MegaToolTip.DataModules.SubElements;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.UI.MegaToolTip.DataModules
{
	public class GasModule : ModuleBase
	{
		private new void Awake()
		{
			base.Awake();
			this._rectTransform = base.GetComponent<RectTransform>();
			this._gases = new GasModule.Gas[]
			{
				new GasModule.Gas
				{
					Name = "O2",
					Description = "Amount of Oxygen gas in mass (Kg) and pressure (KPa)",
					Color = DataHandler.GetColor("O2Green")
				},
				new GasModule.Gas
				{
					Name = "N2",
					Description = "Amount of Nitrogen gas in mass (Kg) and pressure (KPa)",
					Color = DataHandler.GetColor("N2Blue")
				},
				new GasModule.Gas
				{
					Name = "NH3",
					Description = "Amount of Ammonia gas in mass (Kg) and pressure (KPa)",
					Color = DataHandler.GetColor("NH3Beige")
				},
				new GasModule.Gas
				{
					Name = "CH4",
					Description = "Amount of Ammonia gas in mass (Kg) and pressure (KPa)",
					Color = DataHandler.GetColor("CH4BlueGreen")
				},
				new GasModule.Gas
				{
					Name = "CO2",
					Description = "Amount of Carbon Dioxide gas in mass (Kg) and pressure (KPa)",
					Color = DataHandler.GetColor("CO2White")
				},
				new GasModule.Gas
				{
					Name = "H2SO4",
					Description = "Amount of sulfiric acid in mass (Kg) and pressure (KPa)",
					Color = DataHandler.GetColor("H2SO4Yellow")
				}
			};
		}

		protected override void OnUpdateUI()
		{
			if (StarSystem.fEpoch - this._timeLastUpdate < 1.0 || this._co == null)
			{
				return;
			}
			this._timeLastUpdate = StarSystem.fEpoch;
			bool flag = false;
			int num = 0;
			foreach (GasModule.Gas gas in this._gases)
			{
				if (gas.IsPresentOnCO(this._co))
				{
					num++;
					if (gas.NumbElement != null)
					{
						gas.NumbElement.UpdateElement();
					}
					else
					{
						gas.NumbElement = this.CreateGasNumberElement(gas, false);
						flag = true;
					}
				}
				else if (gas.NumbElement != null)
				{
					UnityEngine.Object.Destroy(gas.NumbElement.gameObject);
					gas.NumbElement = null;
					flag = true;
				}
			}
			if (num > 0 && this._pressureElement != null)
			{
				this._pressureElement.UpdateElement();
			}
			if (flag)
			{
				if (num == 0 && this._pressureElement != null)
				{
					UnityEngine.Object.Destroy(this._pressureElement.gameObject);
					this._pressureElement = null;
				}
				else if (this._pressureElement == null)
				{
					this._pressureElement = this.CreatePressureNumberElement();
				}
				if (this._pressureElement != null)
				{
					this._pressureElement.transform.SetAsLastSibling();
				}
				LayoutRebuilder.ForceRebuildLayoutImmediate(this._rectTransform);
				LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform.parent.GetComponent<RectTransform>());
			}
		}

		public override void SetData(CondOwner co)
		{
			if (co == null || co.mapConds == null)
			{
				this._IsMarkedForDestroy = true;
				return;
			}
			this._co = co;
			int num = 0;
			foreach (GasModule.Gas gas in this._gases)
			{
				if (gas.IsPresentOnCO(co))
				{
					gas.NumbElement = this.CreateGasNumberElement(gas, true);
					num++;
				}
			}
			if (num >= 1 && co.HasCond("StatGasPressure", false))
			{
				this._pressureElement = this.CreatePressureNumberElement();
				num++;
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(this._rectTransform);
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform.parent.GetComponent<RectTransform>());
			if (num == 0 && !co.HasCond("IsRoom", false))
			{
				this._IsMarkedForDestroy = true;
				return;
			}
		}

		private NumbElement CreatePressureNumberElement()
		{
			NumbElement component = UnityEngine.Object.Instantiate<GameObject>(this._numberElement, this._tfNumbContainer.transform).GetComponent<NumbElement>();
			component.SetData(DataHandler.GetString("GUI_MTT_GAS_KPA", false), new Tuple<string, string>("StatGasPressure", "StatGasPressureMax"), new Func<string, string, string>(this.UpdatePressureString), "Overall pressure in the container", DataHandler.GetColor("PressureYellow"));
			LayoutRebuilder.ForceRebuildLayoutImmediate(this._rectTransform);
			component.ForceMeshUpdate();
			return component;
		}

		private NumbElement CreateGasNumberElement(GasModule.Gas gas, bool updateLayout = true)
		{
			NumbElement component = UnityEngine.Object.Instantiate<GameObject>(this._numberElement, this._tfNumbContainer.transform).GetComponent<NumbElement>();
			component.SetData(DataHandler.GetString("GUI_MTT_GAS_" + gas.Name, false), new Tuple<string, string>(gas.CondPP, gas.CondMol), new Func<string, string, string>(this.UpdateGasString), gas.Description, gas.Color);
			if (updateLayout)
			{
				LayoutRebuilder.ForceRebuildLayoutImmediate(this._rectTransform);
				component.ForceMeshUpdate();
			}
			return component;
		}

		private string UpdateGasString(string cond1, string cond2)
		{
			string str = string.Empty;
			if (!string.IsNullOrEmpty(cond2))
			{
				str = this.GetGasOrVoidString(cond2, this._co.GetCondAmount(cond2)) + " |  ";
			}
			return str + this._co.GetCondAmount(cond1).ToString("N1") + " KPa";
		}

		private string UpdatePressureString(string cond1, string cond2)
		{
			string str = string.Empty;
			double condAmount = this._co.GetCondAmount(cond1);
			if (!string.IsNullOrEmpty(cond2))
			{
				double condAmount2 = this._co.GetCondAmount(cond2, false);
				if (condAmount2 > 0.0)
				{
					str = (condAmount / condAmount2 * 100.0).ToString("F1") + "% | ";
				}
			}
			return str + condAmount.ToString("N1") + " KPa";
		}

		private string GetGasOrVoidString(string condName, double condAmount)
		{
			int length = "StatGasMol".Length;
			string strGas = (condName.Length < length) ? condName : condName.Substring(length);
			double gasMass = GasContainer.GetGasMass(strGas, condAmount);
			return (gasMass <= 1E+50) ? (gasMass.ToString("N2") + " Kg") : string.Empty;
		}

		[SerializeField]
		private Transform _tfNumbContainer;

		[SerializeField]
		private GameObject _numberElement;

		private RectTransform _rectTransform;

		private CondOwner _co;

		private double _timeLastUpdate;

		private NumbElement _pressureElement;

		private GasModule.Gas[] _gases;

		private class Gas
		{
			public string CondPP
			{
				get
				{
					return "StatGasPp" + this.Name;
				}
			}

			public string CondMol
			{
				get
				{
					return "StatGasMol" + this.Name;
				}
			}

			public bool IsPresentOnCO(CondOwner co)
			{
				return co.HasCond(this.CondPP) || co.HasCond(this.CondMol);
			}

			public string Name;

			public string Description;

			public Color Color;

			public NumbElement NumbElement;
		}
	}
}
