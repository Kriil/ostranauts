using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DevConsole : MonoBehaviour
{
	private void Start()
	{
		this.input.onSelect.AddListener(delegate(string A_1)
		{
			this.SetTyping();
		});
		this.input.onDeselect.AddListener(delegate(string A_1)
		{
			this.EndTyping();
		});
		this.input.onSubmit.AddListener(delegate(string A_1)
		{
			this.Command(this.input.text);
		});
		DevConsole.output_instance = this.output;
		if (DevConsole.commands.Keys.Count == 0)
		{
			HelloWorld helloWorld = new HelloWorld();
			Teleport teleport = new Teleport();
			StopPlayer stopPlayer = new StopPlayer();
			StartPlayer startPlayer = new StartPlayer();
			GiveItem giveItem = new GiveItem();
			GiveItemDrop giveItemDrop = new GiveItemDrop();
		}
	}

	private void Update()
	{
		if (CrewSim.Typing && CrewSim.bDebugShow)
		{
			GUIActionKeySelector.commandDebug.Execute();
		}
		if (CrewSim.bDebugShow)
		{
			this.statsText.text = "Condition Owners: " + DataHandler.debugCOCount;
		}
	}

	public void SetTyping()
	{
		CrewSim.Typing = true;
	}

	public void EndTyping()
	{
		CrewSim.Typing = false;
	}

	public void Command(string text)
	{
		DevCommand devCommand = null;
		if (DevConsole.commands.TryGetValue(text.Split(new char[]
		{
			' '
		})[0].ToLower(), out devCommand))
		{
			devCommand.Execute(text);
		}
		this.input.text = string.Empty;
		this.input.ActivateInputField();
	}

	public static void Output(string text)
	{
		TextMeshProUGUI textMeshProUGUI = DevConsole.output_instance;
		textMeshProUGUI.text = textMeshProUGUI.text + text + "\n";
	}

	public TextMeshProUGUI output;

	public TMP_InputField input;

	public static Dictionary<string, DevCommand> commands = new Dictionary<string, DevCommand>();

	public static TextMeshProUGUI output_instance;

	public TextMeshProUGUI statsText;
}
