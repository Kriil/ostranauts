using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

// In-game debug console/logger overlay. This appears to capture Unity logs and
// exceptions, optionally persist them to a file, and expose a simple command UI.
public class ConsoleToGUI : MonoBehaviour
{
	// Unity setup: initializes the singleton instance and keeps it alive between
	// scene loads so logging persists across the session.
	private void Awake()
	{
		this.Init();
		UnityEngine.Object.DontDestroyOnLoad(base.transform.gameObject);
	}

	// Singleton guard: loads console settings once, or destroys duplicate copies.
	public void Init()
	{
		if (ConsoleToGUI.instance == null)
		{
			ConsoleToGUI.instance = this;
			this.LoadJson();
		}
		else if (ConsoleToGUI.instance != this)
		{
			UnityEngine.Object.Destroy(base.transform.gameObject);
		}
	}

	// Hooks Unity log and unhandled exception events while this object is active.
	private void OnEnable()
	{
		Application.logMessageReceived += this.Log;
		AppDomain.CurrentDomain.UnhandledException += this.LogException;
	}

	// Removes the log hooks when disabled.
	private void OnDisable()
	{
		Application.logMessageReceived -= this.Log;
		AppDomain.CurrentDomain.UnhandledException -= this.LogException;
	}

	// Manual reattach helper exposed to console commands or debugging.
	public static void Attach()
	{
		if (ConsoleToGUI.instance == null)
		{
			return;
		}
		Application.logMessageReceived += ConsoleToGUI.instance.Log;
		AppDomain.CurrentDomain.UnhandledException += ConsoleToGUI.instance.LogException;
		Debug.Log("TAMPER: Logging manually reatached by player!");
	}

	// Manual detach helper exposed to console commands or debugging.
	public static void Detach()
	{
		if (ConsoleToGUI.instance == null)
		{
			return;
		}
		Debug.Log("TAMPER: Logging manually detached by player!");
		Application.logMessageReceived -= ConsoleToGUI.instance.Log;
		AppDomain.CurrentDomain.UnhandledException -= ConsoleToGUI.instance.LogException;
	}

	// Routes CLR unhandled exceptions through the same in-game log formatter.
	private void LogException(object sender, UnhandledExceptionEventArgs e)
	{
		this.Log(e.ToString(), sender.ToString(), LogType.Exception);
	}

	// Keyboard toggle for showing or focusing the console command window.
	private void Update()
	{
		if (GUIActionKeySelector.commandToggleConsole.Down && !this.handled)
		{
			this.doShow = !this.doShow;
			if (this.doShow)
			{
				this.doFocus = true;
				GUI.FocusWindow(1);
				GUI.FocusControl("command");
			}
			this.handled = true;
		}
		else
		{
			this.handled = false;
		}
	}

	// Main log sink for Unity messages. Formats severity, trims history, tracks
	// bug level, and optionally appends the message to a persistent log file.
	public void Log(string logString, string stackTrace, LogType type)
	{
		if (stackTrace != null && stackTrace.Length > 0)
		{
			stackTrace = stackTrace.TrimEnd(ConsoleToGUI.aNewLines);
			logString = logString + "\n" + stackTrace;
		}
		if (logString.Length > this.kChars)
		{
			logString = logString.Substring(0, this.kChars);
		}
		switch (type)
		{
		case LogType.Error:
			if (!this.doError)
			{
				return;
			}
			logString = "<color=red><b>[Error]</b></color>: " + logString;
			this.nErrorCount++;
			if (this.bugLevel < 3)
			{
				this.bugLevel = 3;
			}
			break;
		case LogType.Assert:
			if (!this.doAssert)
			{
				return;
			}
			logString = "<color=red><b>[Assert]</b></color>: " + logString;
			this.nErrorCount++;
			if (this.bugLevel < 3)
			{
				this.bugLevel = 3;
			}
			break;
		case LogType.Warning:
			if (!this.doWarning)
			{
				return;
			}
			logString = "<color=yellow><b>[Warning]</b></color>: " + logString;
			if (this.bugLevel < 1)
			{
				this.bugLevel = 1;
			}
			break;
		case LogType.Log:
			if (!this.doLogs)
			{
				return;
			}
			logString = "<b>[Log]</b>: " + logString;
			if (this.bugLevel < 0)
			{
				this.bugLevel = 0;
			}
			break;
		case LogType.Exception:
			if (!this.doException)
			{
				return;
			}
			logString = "<color=orange><b>[Exception]</b></color>: " + logString;
			this.nErrorCount++;
			if (this.bugLevel < 2)
			{
				this.bugLevel = 2;
			}
			break;
		default:
			if (!this.doUnknown)
			{
				return;
			}
			logString = "<b>[Unknown]</b>: " + logString;
			break;
		}
		this.myLog = this.myLog + "\n" + logString;
		while (this.myLog.Length > this.mChars)
		{
			int num = this.myLog.IndexOf("\n", 0, this.kChars + 40);
			this.myLog = this.myLog.Substring(num + 1);
		}
		if (this.saveToFile)
		{
			if (this.filename == string.Empty)
			{
				string text = Path.Combine(Application.persistentDataPath, "Debug Log Files/");
				Directory.CreateDirectory(text);
				this.filename = text + this.logName;
			}
			try
			{
				File.AppendAllText(this.filename, logString + "\n");
			}
			catch
			{
			}
		}
	}

	// Helper for non-error informational messages written directly by game code.
	public void LogInfo(string logString)
	{
		logString = "<color=lime><b>[Info]</b></color>: " + logString;
		if (this.bugLevel < 0)
		{
			this.bugLevel = 0;
		}
		this.myLog = this.myLog + "\n" + logString;
		while (this.myLog.Length > this.mChars)
		{
			int num = this.myLog.IndexOf("\n", 0, this.kChars + 40);
			this.myLog = this.myLog.Substring(num + 1);
		}
		if (this.saveToFile)
		{
			if (this.filename == string.Empty)
			{
				string text = Path.Combine(Application.persistentDataPath, "Debug Log Files/");
				Directory.CreateDirectory(text);
				this.filename = text + this.logName;
			}
			try
			{
				File.AppendAllText(this.filename, logString + "\n");
			}
			catch
			{
			}
		}
	}

	// Immediate-mode UI entrypoint. Draws the icon badge, handles visibility, and
	// opens the main console window when active.
	private void OnGUI()
	{
		if (this.logStyle == null)
		{
			this.InitVariables();
			this.Res();
		}
		if (this.bugLevel > -1 && !this.doDarkness)
		{
			if (!ConsoleToGUI.bFirstWindowAppear && this.bugLevel >= 2)
			{
				this.doShow = true;
				ConsoleToGUI.bFirstWindowAppear = true;
			}
			if (GUI.Button(this.guiButtonRect, this.sprites[this.bugLevel].texture, this.butStyle))
			{
				this.doShow = !this.doShow;
				if (!this.doShow && GUI.GetNameOfFocusedControl() == "command")
				{
					CrewSim.Typing = false;
				}
				this.bugLevel = 0;
			}
			if (this.guiButtonRect.Contains(Event.current.mousePosition) && this.doShow)
			{
				Tooltippable2 component = base.GetComponent<Tooltippable2>();
				if (component != null)
				{
					component.OnPointerExit(null);
				}
			}
		}
		if (this.HandleInput())
		{
			return;
		}
		if (!this.doShow)
		{
			if (this.windowRaycastBlocker.enabled)
			{
				this.windowRaycastBlocker.enabled = false;
				PerformanceMonitor.active = false;
			}
			return;
		}
		this.Res();
		this.windowRect = GUI.Window(1, this.windowRect, new GUI.WindowFunction(this.DrawConsole), this.myTitle);
		if (!this.windowRaycastBlocker.enabled)
		{
			this.windowRaycastBlocker.enabled = true;
			PerformanceMonitor.active = true;
			Tooltippable2 component2 = base.GetComponent<Tooltippable2>();
			if (component2 != null)
			{
				component2.OnPointerExit(null);
			}
		}
		this.windowRaycastBlocker.rectTransform.sizeDelta = this.windowRect.size;
		this.windowRaycastBlocker.transform.localPosition = new Vector3(this.windowRect.position.x - this.windowRect.width / 2f, -this.windowRect.position.y + this.windowRect.height / 2f);
	}

	// Draws the scrollback area and command input field, then runs console
	// commands via ConsoleResolver when the player presses Return.
	private void DrawConsole(int window)
	{
		if (this.HandleInput())
		{
			return;
		}
		Rect rect = new Rect(10f, 20f, (float)(Screen.width / 2 - 40), 4000f);
		Rect position = new Rect(10f, 30f, (float)(Screen.width / 2 - 20), (float)(Screen.height / 2) - (this._textActual + 55f));
		Rect position2 = new Rect(10f, position.y + position.height + 5f, (float)(Screen.width / 2 - 40), this._textActual + 10f);
		this.scrollPos = GUI.BeginScrollView(position, this.scrollPos, rect, false, true);
		GUI.TextArea(rect, this.myLog, this.mChars, this.logStyle);
		GUI.EndScrollView();
		GUI.SetNextControlName("command");
		this.myInput = GUI.TextField(position2, this.myInput, this.txtStyle);
		if (Event.current.isKey && GUI.GetNameOfFocusedControl() == "command")
		{
			if (Event.current.keyCode == KeyCode.Return)
			{
				string[] array = this.myInput.Split(new char[]
				{
					';'
				});
				if (array.Length > 1)
				{
					this.myInput = "<color=" + this.multipleColor + "><b>[Command]</b></color>: " + this.myInput;
					this.myLog = this.myLog + "\n" + this.myInput;
				}
				this.myInput = string.Empty;
				for (int i = 0; i < array.Length; i++)
				{
					if (!(array[i] == string.Empty))
					{
						this.prevInputs.Add(array[i]);
						if (this.prevInputs.Count > this.prevMax)
						{
							this.prevInputs.RemoveRange(0, this.prevInputs.Count - this.prevMax);
						}
						if (ConsoleResolver.ResolveString(ref array[i]))
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
			else if (Event.current.keyCode == KeyCode.UpArrow)
			{
				if (this.prevInputs.Count > 0)
				{
					if (this.prevPointer == 0 && this.myInput != string.Empty)
					{
						this.prevInputs.Add(this.myInput);
						if (this.prevInputs.Count > this.prevMax)
						{
							this.prevInputs.RemoveRange(0, this.prevInputs.Count - this.prevMax);
						}
						this.prevPointer++;
					}
					this.prevPointer++;
					if (this.prevPointer > this.prevInputs.Count)
					{
						this.prevPointer = 1;
					}
					this.myInput = this.prevInputs[this.prevInputs.Count - this.prevPointer];
				}
			}
			else if (Event.current.keyCode == KeyCode.DownArrow)
			{
				if (this.prevInputs.Count > 0)
				{
					if (this.prevPointer == 0 && this.myInput != string.Empty)
					{
						this.prevInputs.Add(this.myInput);
						if (this.prevInputs.Count > this.prevMax)
						{
							this.prevInputs.RemoveRange(0, this.prevInputs.Count - this.prevMax);
						}
						this.prevPointer--;
					}
					this.prevPointer--;
					if (this.prevPointer < 1)
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
		if (this.doFocus)
		{
			GUI.FocusControl("command");
			this.doFocus = false;
		}
		if (GUI.GetNameOfFocusedControl() == "command")
		{
			CrewSim.Typing = true;
		}
		else
		{
			CrewSim.Typing = false;
		}
		GUI.DragWindow();
	}

	// Shared hotkey handler used by both Update and IMGUI paths.
	private bool HandleInput()
	{
		if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F3 && !this.handled)
		{
			this.doShow = !this.doShow;
			if (this.doShow)
			{
				this.doFocus = true;
				GUI.FocusWindow(1);
				GUI.FocusControl("command");
			}
			else if (GUI.GetNameOfFocusedControl() == "command")
			{
				CrewSim.Typing = false;
			}
			this.handled = true;
			return true;
		}
		return false;
	}

	// Loads console appearance and behavior from `console_params.json`.
	private void LoadJson()
	{
		ConsoleData consoleData = ConsoleData.CreateFromJSON("console_params.json");
		if (consoleData == null)
		{
			return;
		}
		this.myTitle = consoleData.consoleTitle;
		this.myLog = consoleData.startTxt;
		this.logName = consoleData.fileName;
		this.doLogs = consoleData.enableLog;
		this.doWarning = consoleData.enableWarning;
		this.doException = consoleData.enableException;
		this.doError = consoleData.enableError;
		this.doAssert = consoleData.enableAssert;
		this.doUnknown = consoleData.enableUnknown;
		this.saveToFile = consoleData.saveToFile;
		this.textSize = consoleData.textSize;
		this.kChars = consoleData.maxMessage;
		this.mChars = consoleData.maxTotal;
		this.iconSize = consoleData.popUpSize;
		this.guiButtonRect = new Rect(58f, 5f, (float)this.iconSize, (float)this.iconSize);
		this.windowRect = new Rect(this.windowRect.x, (float)(this.iconSize + 10), this.windowRect.width, this.windowRect.height);
	}

	// Lazily builds the IMGUI styles used by the log and command box.
	private void InitVariables()
	{
		this.logStyle = GUI.skin.GetStyle("Box");
		this.logStyle.richText = true;
		this.logStyle.alignment = TextAnchor.LowerLeft;
		this.logStyle.fontSize = this.textSize;
		this.logStyle.wordWrap = true;
		this.txtStyle = new GUIStyle(GUI.skin.GetStyle("Box"));
		this.txtStyle.richText = false;
		this.txtStyle.alignment = TextAnchor.MiddleLeft;
		this.txtStyle.fontSize = this.textSize;
		this.txtStyle.wordWrap = true;
	}

	// Exposes the number of logged errors/exceptions for HUD or debug checks.
	public int ErrorCount
	{
		get
		{
			return this.nErrorCount;
		}
	}

	// Static helper so other systems can clear the current console history.
	public static bool StaticClear(int nLines = 0)
	{
		if (ConsoleToGUI.instance != null)
		{
			ConsoleToGUI.instance.Clear(nLines);
			return true;
		}
		return false;
	}

	// Clears all history or trims the last N lines from the log buffer.
	private void Clear(int nLines = 0)
	{
		if (nLines <= 0)
		{
			this.myLog = string.Empty;
		}
		else
		{
			this.myLog = this.myLog.TrimEnd(new char[]
			{
				'\n'
			});
			for (int i = 0; i < nLines; i++)
			{
				int num = this.myLog.LastIndexOf('\n');
				if (num < 0)
				{
					num = 0;
				}
				this.myLog = this.myLog.Substring(0, num);
			}
		}
	}

	// Recalculates the IMGUI layout for the current screen resolution.
	private void Res()
	{
		this._textActual = (float)this.textSize * ((float)Screen.height / 1080f);
		if (this._textActual < (float)this.textSize)
		{
			this._textActual = (float)this.textSize;
		}
		this.logStyle.fontSize = (int)this._textActual;
		this.txtStyle.fontSize = (int)this._textActual;
		this.windowRect.size = new Vector2((float)(Screen.width / 2), (float)(Screen.height / 2));
	}

	// Toggles the "bug darkness" overlay mode and flips console visibility.
	public static void ToggleBugs()
	{
		if (ConsoleToGUI.instance == null)
		{
			return;
		}
		ConsoleToGUI.instance.doDarkness = !ConsoleToGUI.instance.doDarkness;
		ConsoleToGUI.instance.doShow = !ConsoleToGUI.instance.doShow;
	}

	// Runtime state for the overlay widgets, formatting, and saved scrollback.
	public Image windowRaycastBlocker;
	public Sprite[] sprites;
	private int bugLevel = -1;
	private int nErrorCount;
	public static ConsoleToGUI instance = null;
	private static bool bFirstWindowAppear = false;
	private static char[] aNewLines = new char[]
	{
		'\n'
	};
	private string myTitle = "Ostranauts Debug Log";
	private string myLog = "*begin log";
	private string myInput = string.Empty;
	private string logName = "recent-log.txt";
	private string filename = string.Empty;
	private string commandColor = "#c371f0";
	private string failedColor = "#ed2499";
	private string multipleColor = "#4615d6";
	private List<string> prevInputs = new List<string>();
	private bool doShow;
	private bool handled;
	private bool doFocus;
	private bool doDarkness;
	private bool doLogs;
	private bool doWarning = true;
	private bool doException = true;
	private bool doError = true;
	private bool doAssert = true;
	private bool doUnknown = true;
	private bool saveToFile;
	private int textSize = 14;
	private float _textActual = 14f;
	private int kChars = 700;
	private int mChars = 16382;
	private int iconSize = 20;
	private int prevMax = 64;
	private int prevPointer;
	private Rect windowRect = new Rect(10f, 30f, (float)(Screen.width / 2), (float)(Screen.height / 2));
	private Vector2 scrollPos = new Vector2(0f, 4000f);
	private GUIStyle butStyle = GUIStyle.none;
	private GUIStyle labStyle = GUIStyle.none;
	private GUIStyle logStyle;
	private GUIStyle txtStyle;
	private Rect guiButtonRect = new Rect(58f, 5f, 20f, 20f);
}
