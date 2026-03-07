using System;
using Ostranauts.Core.Models;
using UnityEngine;

public class VizorPreset
{
	public VizorPreset(string variable, float max, float min, float opacity, RenderOverlayMode rom, RenderPresets preset, bool fov, bool lights, bool exteriors, bool ceiling, bool tasks, bool ao, bool placeholders, bool power, bool log, bool updates)
	{
		this._variable = variable;
		this._max = max;
		this._min = min;
		this._opacity = opacity;
		this._rom = rom;
		this._preset = preset;
		this._fov = fov;
		this._lights = lights;
		this._exteriors = exteriors;
		this._ceiling = ceiling;
		this._tasks = tasks;
		this._ao = ao;
		this._placeholders = placeholders;
		this._power = power;
		this._log = log;
		this._updates = updates;
	}

	public string ToCustomInfo()
	{
		string text = string.Empty;
		text = text + "variable:" + this._variable + "|";
		text = text + "max:" + this._max.ToString() + "|";
		text = text + "min:" + this._min.ToString() + "|";
		text = text + "opacity:" + this._opacity.ToString() + "|";
		string text2 = text;
		text = string.Concat(new object[]
		{
			text2,
			"rom:",
			(int)this._rom,
			"|"
		});
		text2 = text;
		text = string.Concat(new object[]
		{
			text2,
			"preset:",
			(int)this._preset,
			"|"
		});
		text2 = text;
		text = string.Concat(new object[]
		{
			text2,
			"fov:",
			this._fov,
			"|"
		});
		text2 = text;
		text = string.Concat(new object[]
		{
			text2,
			"lights:",
			this._lights,
			"|"
		});
		text2 = text;
		text = string.Concat(new object[]
		{
			text2,
			"exteriors:",
			this._exteriors,
			"|"
		});
		text2 = text;
		text = string.Concat(new object[]
		{
			text2,
			"ceiling:",
			this._ceiling,
			"|"
		});
		text2 = text;
		text = string.Concat(new object[]
		{
			text2,
			"tasks:",
			this._tasks,
			"|"
		});
		text2 = text;
		text = string.Concat(new object[]
		{
			text2,
			"ao:",
			this._ao,
			"|"
		});
		text2 = text;
		text = string.Concat(new object[]
		{
			text2,
			"placeholders:",
			this._placeholders,
			"|"
		});
		text2 = text;
		text = string.Concat(new object[]
		{
			text2,
			"power:",
			this._power,
			"|"
		});
		text2 = text;
		text = string.Concat(new object[]
		{
			text2,
			"log:",
			this._log,
			"|"
		});
		text2 = text;
		return string.Concat(new object[]
		{
			text2,
			"updates:",
			this._updates,
			"|"
		});
	}

	public void FromCustomInfo(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return;
		}
		string[] array = value.Split(new char[]
		{
			'|'
		});
		foreach (string text in array)
		{
			string[] array3 = text.Split(new char[]
			{
				':'
			});
			if (array3.Length >= 2 && array3[0] != null && array3[0].Length != 0)
			{
				string text2 = array3[0];
				switch (text2)
				{
				case "variable":
					this._variable = array3[1];
					goto IL_318;
				case "max":
					this._max = float.Parse(array3[1]);
					goto IL_318;
				case "min":
					this._min = float.Parse(array3[1]);
					goto IL_318;
				case "opacity":
					this._opacity = float.Parse(array3[1]);
					goto IL_318;
				case "rom":
					this._rom = (RenderOverlayMode)int.Parse(array3[1]);
					goto IL_318;
				case "preset":
					this._preset = (RenderPresets)int.Parse(array3[1]);
					goto IL_318;
				case "fov":
					this._fov = bool.Parse(array3[1]);
					goto IL_318;
				case "lights":
					this._lights = bool.Parse(array3[1]);
					goto IL_318;
				case "exteriors":
					this._exteriors = bool.Parse(array3[1]);
					goto IL_318;
				case "ceiling":
					this._ceiling = bool.Parse(array3[1]);
					goto IL_318;
				case "tasks":
					this._tasks = bool.Parse(array3[1]);
					goto IL_318;
				case "ao":
					this._ao = bool.Parse(array3[1]);
					goto IL_318;
				case "placeholders":
					this._placeholders = bool.Parse(array3[1]);
					goto IL_318;
				case "power":
					this._power = bool.Parse(array3[1]);
					goto IL_318;
				case "log":
					this._log = bool.Parse(array3[1]);
					goto IL_318;
				case "updates":
					this._updates = bool.Parse(array3[1]);
					goto IL_318;
				}
				Debug.LogError("PDA Preset value not recognised, check saving code! Preset value: " + array3[0]);
			}
			IL_318:;
		}
	}

	public string _variable = "_None";

	public float _max = 1f;

	public float _min;

	public float _opacity = 1f;

	public RenderOverlayMode _rom;

	public RenderPresets _preset;

	public bool _fov = true;

	public bool _lights = true;

	public bool _exteriors = true;

	public bool _ceiling = true;

	public bool _tasks = true;

	public bool _ao = true;

	public bool _placeholders = true;

	public bool _power;

	public bool _log;

	public bool _updates;
}
