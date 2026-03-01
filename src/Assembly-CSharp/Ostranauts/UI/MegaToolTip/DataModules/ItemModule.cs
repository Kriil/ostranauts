using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Ostranauts.UI.MegaToolTip.DataModules
{
	public class ItemModule : ModuleBase
	{
		private new void Awake()
		{
			base.Awake();
		}

		public override void SetData(CondOwner co)
		{
			this._txtFullName.text = co.strNameFriendly;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(co.strDesc);
			stringBuilder.AppendLine();
			stringBuilder.Append("Factions: ");
			bool flag = false;
			foreach (string strName in co.GetAllFactions())
			{
				JsonFaction faction = CrewSim.system.GetFaction(strName);
				if (faction != null && !(faction.strName == co.strID))
				{
					if (flag)
					{
						stringBuilder.Append(", ");
					}
					stringBuilder.Append(faction.strNameFriendly);
					flag = true;
				}
			}
			if (!flag)
			{
				stringBuilder.Append("n/a");
			}
			this._txtDescription.text = stringBuilder.ToString();
			Texture texture = this.GetTexture(co);
			if (texture != null)
			{
				this._imgAspectRatioFitter.aspectRatio = (float)texture.width / (float)texture.height;
				this._imgCO.texture = texture;
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.GetComponent<RectTransform>());
			this._txtDescription.ForceMeshUpdate();
			this._txtFullName.ForceMeshUpdate();
			LayoutRebuilder.ForceRebuildLayoutImmediate(base.transform.parent.GetComponent<RectTransform>());
		}

		protected virtual Texture GetTexture(CondOwner co)
		{
			return DataHandler.LoadPNG(co.strPortraitImg + ".png", false, false);
		}

		public void StartRename()
		{
			coRename.ShowInstance(this._txtFullName.text);
		}

		[SerializeField]
		private TMP_Text _txtFullName;

		[SerializeField]
		protected TMP_Text _txtDescription;

		[SerializeField]
		private RawImage _imgCO;

		[SerializeField]
		private AspectRatioFitter _imgAspectRatioFitter;
	}
}
