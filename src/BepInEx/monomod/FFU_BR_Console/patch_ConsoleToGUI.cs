using System;
using System.IO;
using System.Linq;
using System.Text;
using FFU_Beyond_Reach;
using MonoMod;
using UnityEngine;
// Console UI patch for the FFU_BR command set.
// This expands the in-game console for larger logs, chained commands, and
// persistent command history stored beside the game install.
public class patch_ConsoleToGUI : ConsoleToGUI
{
	[MonoModReplace]
	// Rebuilds the console window with FFU_BR-specific usability upgrades.
	// This adds larger log buffers, semicolon-separated command batching, and
	// persistent input history to support heavier debugging sessions.
	private void DrawConsole(int window)
	{
		bool flag = base.HandleInput();
		if (!flag)
		{
			bool flag2 = !this.configLoaded;
			if (flag2)
			{
				this.configLoaded = true;
				this.mChars = FFU_BR_Defs.MaxLogTextSize;
				this.scrollPos = new Vector2(0f, (float)(this.mChars / 4));
				this.logHistoryPath = Path.Combine(Application.dataPath, "console_history.txt");
				bool flag3 = this.logHistoryPath != null && File.Exists(this.logHistoryPath);
				if (flag3)
				{
					try
					{
						this.prevInputs = File.ReadAllText(this.logHistoryPath, Encoding.UTF8).Split(new char[]
						{
							'\n'
						}).ToList<string>();
					}
					catch (Exception ex)
					{
						Debug.Log("Failed to load console history!\n" + ex.Message + "\n" + ex.StackTrace);
					}
				}
			}
			Rect rect;
			rect..ctor(10f, 20f, (float)(Screen.width / 2 - 40), (float)(this.mChars / 4));
			Rect rect2;
			rect2..ctor(10f, 30f, (float)(Screen.width / 2 - 20), (float)(Screen.height / 2) - (this._textActual + 55f));
			Rect rect3;
			rect3..ctor(10f, rect2.y + rect2.height + 5f, (float)(Screen.width / 2 - 40), this._textActual + 10f);
			this.scrollPos = GUI.BeginScrollView(rect2, this.scrollPos, rect, false, true);
			GUI.TextArea(rect, this.myLog, this.mChars, this.logStyle);
			GUI.EndScrollView();
			GUI.SetNextControlName("command");
			this.myInput = GUI.TextField(rect3, this.myInput, this.txtStyle);
			bool flag4 = Event.current.isKey && GUI.GetNameOfFocusedControl() == "command";
			if (flag4)
			{
				bool flag5 = Event.current.keyCode == 13;
				if (flag5)
				{
					string[] array = this.myInput.Split(new char[]
					{
						';'
					});
					bool flag6 = !string.IsNullOrEmpty(this.myInput) && this.prevInputs.LastOrDefault<string>() != this.myInput;
					if (flag6)
					{
						this.prevInputs.Add(this.myInput);
					}
					bool flag7 = this.prevInputs.Count > this.prevMax;
					if (flag7)
					{
						this.prevInputs.RemoveRange(0, this.prevInputs.Count - this.prevMax);
					}
					bool flag8 = array.Length > 1;
					if (flag8)
					{
						this.myInput = "<color=" + this.multipleColor + "><b>[Command]</b></color>: " + this.myInput;
						this.myLog = this.myLog + "\n" + this.myInput;
					}
					this.myInput = string.Empty;
					for (int i = 0; i < array.Length; i++)
					{
						bool flag9 = !(array[i] == string.Empty);
						if (flag9)
						{
							array[i] = array[i].Trim();
							bool flag10 = ConsoleResolver.ResolveString(ref array[i]);
							if (flag10)
							{
								array[i] = "<color=" + this.commandColor + "><b>[Command]</b></color>: " + array[i];
							}
							else
							{
								array[i] = "<color=" + this.failedColor + "><b>[Command]</b></color>: " + array[i];
							}
							this.myLog = this.myLog + "\n" + array[i];
						}
					}
					this.prevPointer = 0;
				}
				else
				{
					bool flag11 = Event.current.keyCode == 273;
					if (flag11)
					{
						bool flag12 = this.prevInputs.Count > 0;
						if (flag12)
						{
							bool flag13 = this.prevPointer == 0 && this.myInput != string.Empty;
							if (flag13)
							{
								this.prevInputs.Add(this.myInput);
								bool flag14 = this.prevInputs.Count > this.prevMax;
								if (flag14)
								{
									this.prevInputs.RemoveRange(0, this.prevInputs.Count - this.prevMax);
								}
								this.prevPointer++;
							}
							this.prevPointer++;
							bool flag15 = this.prevPointer > this.prevInputs.Count;
							if (flag15)
							{
								this.prevPointer = 1;
							}
							this.myInput = this.prevInputs[this.prevInputs.Count - this.prevPointer];
						}
					}
					else
					{
						bool flag16 = Event.current.keyCode == 274;
						if (flag16)
						{
							bool flag17 = this.prevInputs.Count > 0;
							if (flag17)
							{
								bool flag18 = this.prevPointer == 0 && this.myInput != string.Empty;
								if (flag18)
								{
									this.prevInputs.Add(this.myInput);
									bool flag19 = this.prevInputs.Count > this.prevMax;
									if (flag19)
									{
										this.prevInputs.RemoveRange(0, this.prevInputs.Count - this.prevMax);
									}
									this.prevPointer--;
								}
								this.prevPointer--;
								bool flag20 = this.prevPointer < 1;
								if (flag20)
								{
									this.prevPointer = this.prevInputs.Count;
								}
								this.myInput = this.prevInputs[this.prevInputs.Count - this.prevPointer];
							}
						}
						else
						{
							this.prevPointer = 0;
						}
					}
				}
			}
			bool doFocus = this.doFocus;
			if (doFocus)
			{
				GUI.FocusControl("command");
				this.doFocus = false;
			}
			bool flag21 = GUI.GetNameOfFocusedControl() == "command";
			if (flag21)
			{
				CrewSim.Typing = true;
			}
			else
			{
				CrewSim.Typing = false;
			}
			bool flag22 = Event.current.keyCode == 13 && this.logHistoryPath != null;
			if (flag22)
			{
				try
				{
					File.WriteAllText(this.logHistoryPath, string.Join("\n", this.prevInputs.ToArray()));
				}
				catch (Exception ex2)
				{
					Debug.Log("Failed to save console history!\n" + ex2.Message + "\n" + ex2.StackTrace);
				}
			}
			GUI.DragWindow();
		}
	}
	private string logHistoryPath = null;
	private bool configLoaded = false;
}
