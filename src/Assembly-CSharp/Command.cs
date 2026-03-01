using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class Command
{
	public virtual void Execute()
	{
	}

	public bool Held
	{
		get
		{
			foreach (List<KeyCode> list in this.currentCombos)
			{
				bool flag = false;
				if (list.Count > 0)
				{
					flag = true;
				}
				foreach (KeyCode key in list)
				{
					if (!Input.GetKey(key))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					if (GUIActionKeySelector.verbose)
					{
						Debug.LogWarning(this.commandDisplayLabel + " Held");
					}
					return true;
				}
			}
			if (this.currentCombos.Count == 0 && this.vital)
			{
				bool flag2 = false;
				if (this.defaultCombo.Count > 0)
				{
					flag2 = true;
				}
				foreach (KeyCode key2 in this.defaultCombo)
				{
					if (!Input.GetKey(key2))
					{
						flag2 = false;
						break;
					}
				}
				if (flag2)
				{
					if (GUIActionKeySelector.verbose)
					{
						Debug.LogWarning(this.commandDisplayLabel + " Held");
					}
					return true;
				}
			}
			return false;
		}
	}

	public bool Down
	{
		get
		{
			foreach (List<KeyCode> list in this.currentCombos)
			{
				bool flag = false;
				if (list.Count > 0)
				{
					flag = true;
				}
				foreach (KeyCode key in list)
				{
					if (!Input.GetKey(key))
					{
						flag = false;
						break;
					}
				}
				if (flag && Input.GetKeyDown(list[list.Count - 1]))
				{
					if (GUIActionKeySelector.verbose)
					{
						Debug.LogWarning(this.commandDisplayLabel + " Down");
					}
					return true;
				}
			}
			if (this.currentCombos.Count == 0 && this.vital)
			{
				bool flag2 = false;
				if (this.defaultCombo.Count > 0)
				{
					flag2 = true;
				}
				foreach (KeyCode key2 in this.defaultCombo)
				{
					if (!Input.GetKey(key2))
					{
						flag2 = false;
						break;
					}
				}
				if (flag2 && Input.GetKeyDown(this.defaultCombo[this.defaultCombo.Count - 1]))
				{
					if (GUIActionKeySelector.verbose)
					{
						Debug.LogWarning(this.commandDisplayLabel + " Down");
					}
					return true;
				}
			}
			return false;
		}
	}

	public bool Mash
	{
		get
		{
			foreach (List<KeyCode> list in this.currentCombos)
			{
				bool flag = false;
				bool flag2 = false;
				if (list.Count > 0)
				{
					flag = true;
				}
				foreach (KeyCode key in list)
				{
					if (!Input.GetKey(key))
					{
						flag = false;
						break;
					}
					if (Input.GetKeyDown(key))
					{
						flag2 = true;
					}
				}
				if (flag && flag2)
				{
					if (GUIActionKeySelector.verbose)
					{
						Debug.LogWarning(this.commandDisplayLabel + " Mashed");
					}
					return true;
				}
			}
			if (this.currentCombos.Count == 0 && this.vital)
			{
				bool flag3 = false;
				bool flag4 = false;
				if (this.defaultCombo.Count > 0)
				{
					flag3 = true;
				}
				foreach (KeyCode key2 in this.defaultCombo)
				{
					if (!Input.GetKey(key2))
					{
						flag3 = false;
						break;
					}
					if (Input.GetKeyDown(key2))
					{
						flag4 = true;
					}
				}
				if (flag3 && flag4)
				{
					if (GUIActionKeySelector.verbose)
					{
						Debug.LogWarning(this.commandDisplayLabel + " Mashed");
					}
					return true;
				}
			}
			return false;
		}
	}

	public string KeyName
	{
		get
		{
			string text = string.Empty;
			if (this.currentCombos.Count > 0)
			{
				foreach (List<KeyCode> list in this.currentCombos)
				{
					if (list.Count > 0)
					{
						foreach (KeyCode keyCode in list)
						{
							text += keyCode.ToString();
							text += " + ";
						}
						text = text.Remove(text.Length - 3, 3);
					}
					text += "  <i>or</i>  ";
				}
				text = text.Remove(text.Length - 13, 13);
			}
			else if (this.vital && this.defaultCombo.Count > 0)
			{
				text = "<i><color=#ff5050>None! Cannot be left blank. Will autoassign ";
				foreach (KeyCode keyCode2 in this.defaultCombo)
				{
					text += keyCode2.ToString();
					text += " + ";
				}
				text = text.Remove(text.Length - 3, 3);
				text += " </color></i>";
			}
			else
			{
				text = "<i><color=#ff5050>None! </color></i>";
			}
			return text;
		}
	}

	public string KeyNameShort
	{
		get
		{
			string text = string.Empty;
			if (this.currentCombos.Count > 0)
			{
				List<KeyCode> list = this.currentCombos[this.currentCombos.Count - 1];
				if (list.Count > 0)
				{
					for (int i = 0; i < list.Count; i++)
					{
						if (i > 0)
						{
							text += " + ";
						}
						text += list[i].ToString();
					}
				}
			}
			else if (this.defaultCombo.Count > 0)
			{
				text = string.Empty;
				for (int j = 0; j < this.defaultCombo.Count; j++)
				{
					if (j > 0)
					{
						text += " + ";
					}
					text += this.defaultCombo[j].ToString();
				}
			}
			else
			{
				text = "N/A";
			}
			return text;
		}
	}

	public string commandDisplayLabel;

	public GameAction gameAction;

	public List<KeyCode> defaultCombo = new List<KeyCode>();

	public List<List<KeyCode>> currentCombos = new List<List<KeyCode>>();

	public bool vital;
}
