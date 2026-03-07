using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Ostranauts.UI.PDA
{
	public class PDANotes : MonoBehaviour
	{
		private void Awake()
		{
			this._substitutions["="] = "<equals>";
			this._substitutions["#"] = "<hash>";
		}

		public void LoadApp()
		{
			this._input.text = this._customInfo;
		}

		public void SaveApp()
		{
			this._customInfo = this._input.text;
		}

		public void StartTyping()
		{
			CrewSim.StartTyping();
		}

		public void EndTyping()
		{
			CrewSim.EndTyping();
			this.SaveApp();
		}

		public string CreateCustomInfo()
		{
			string text = this._customInfo;
			foreach (KeyValuePair<string, string> keyValuePair in this._substitutions)
			{
				text = text.Replace(keyValuePair.Key, keyValuePair.Value);
			}
			return text;
		}

		public void ResolveCustomInfo(string customInfo)
		{
			this._customInfo = customInfo;
			foreach (KeyValuePair<string, string> keyValuePair in this._substitutions)
			{
				this._customInfo = this._customInfo.Replace(keyValuePair.Value, keyValuePair.Key);
			}
		}

		public void CustomInfoAddition(string customInfo)
		{
			foreach (KeyValuePair<string, string> keyValuePair in this._substitutions)
			{
				customInfo = customInfo.Replace(keyValuePair.Value, keyValuePair.Key);
			}
			if (!this._customInfo.EndsWith("\n\n"))
			{
				this._customInfo += "\n";
			}
			if (!this._customInfo.EndsWith("\n\n"))
			{
				this._customInfo += "\n";
			}
			this._customInfo = this._customInfo + "[" + MathUtils.GetTimeFromS(StarSystem.fEpoch) + "]\n";
			this._customInfo += customInfo;
		}

		[SerializeField]
		private TMP_InputField _input;

		private string _customInfo = string.Empty;

		private Dictionary<string, string> _substitutions = new Dictionary<string, string>();
	}
}
