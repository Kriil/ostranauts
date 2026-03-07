using System;
using System.Collections.Generic;
using UnityEngine;

// Per-employee company policy block.
// This stores a worker's 24-hour shift map plus duty priorities and permission
// flags used by the company/crew scheduling systems.
public class JsonCompanyRules
{
	// Initializes a full-day schedule and the default duty priorities.
	public JsonCompanyRules()
	{
		this.aHours = new int[24];
		this.InitDefaultDuties();
	}

	// `aHours` maps each hour of the day to a shift id in `mapShifts`.
	public int[] aHours { get; set; }

	public int[] aDutyLvls { get; set; }

	public bool bShoreLeave { get; set; }

	public bool bAirlockPermission { get; set; }

	public bool bRestorePermission { get; set; }

	// Seeds a standard workday pattern starting at the requested hour.
	public void StartWorkdayAt(int nHour)
	{
		if (nHour < 0)
		{
			return;
		}
		if (nHour > 23)
		{
			return;
		}
		int[] array = new int[]
		{
			2,
			2,
			2,
			2,
			2,
			2,
			2,
			2,
			0,
			0,
			0,
			0,
			1,
			1,
			1,
			1,
			1,
			1,
			1,
			1,
			0,
			0,
			0,
			0
		};
		for (int i = 0; i < this.aHours.Length; i++)
		{
			if (nHour > 23)
			{
				nHour = 0;
			}
			this.aHours[nHour] = array[i];
			nHour++;
		}
	}

	// Sets the same shift across the entire day.
	public void SetAllHours(int nShift)
	{
		if (nShift < 0 || nShift > 2)
		{
			return;
		}
		for (int i = 0; i < this.aHours.Length; i++)
		{
			this.aHours[i] = nShift;
		}
	}

	// Builds the default duty-level array, with Priority treated specially.
	private void InitDefaultDuties()
	{
		this.aDutyLvls = new int[JsonCompanyRules.aDutiesNew.Length];
		this.aDutyLvls[0] = JsonCompanyRules.nPriorityMin;
		for (int i = 1; i < JsonCompanyRules.aDutiesNew.Length; i++)
		{
			this.aDutyLvls[i] = JsonCompanyRules.nDutyDefault;
		}
	}

	// Copies schedule, permissions, and duty levels for a new roster entry.
	public JsonCompanyRules Clone()
	{
		JsonCompanyRules jsonCompanyRules = new JsonCompanyRules();
		jsonCompanyRules.bAirlockPermission = this.bAirlockPermission;
		jsonCompanyRules.bRestorePermission = this.bRestorePermission;
		jsonCompanyRules.bShoreLeave = this.bShoreLeave;
		if (this.aDutyLvls != null)
		{
			if (this.aDutyLvls.Length == JsonCompanyRules.aDutiesOld.Length)
			{
				this.InitDefaultDuties();
			}
			else
			{
				jsonCompanyRules.aDutyLvls = (this.aDutyLvls.Clone() as int[]);
			}
		}
		if (this.aHours != null)
		{
			jsonCompanyRules.aHours = (this.aHours.Clone() as int[]);
		}
		return jsonCompanyRules;
	}

	// Shared shift lookup table used by JsonCompany.GetShift.
	public static Dictionary<int, JsonShift> Shifts()
	{
		return JsonCompanyRules.mapShifts;
	}

	public static string[] aDutiesOld = new string[]
	{
		"Priority",
		"Firefight",
		"Patient",
		"Doctor",
		"Operations",
		"Flick",
		"Cook",
		"Construct",
		"Repair",
		"Haul",
		"Clean"
	};

	public static string[] aDutiesNew = new string[]
	{
		"Priority",
		"Operate",
		"Patch",
		"Repair",
		"Construct",
		"Restore",
		"Demolish",
		"Haul"
	};

	public static Color clrDuty = new Color(0.1640625f, 0.4609375f, 0.08984375f);

	public static int nDutyDefault = 3;

	public static int nPriorityMax = 4;

	public static int nPriorityMin = 1;

	private static Dictionary<int, JsonShift> mapShifts = new Dictionary<int, JsonShift>
	{
		{
			0,
			new JsonShift(0, "Free", "CONDShiftFree", Color.black)
		},
		{
			1,
			new JsonShift(1, "Sleep", "CONDShiftSleep", new Color(0f, 0.625f, 1f))
		},
		{
			2,
			new JsonShift(2, "Work", "CONDShiftWork", new Color(1f, 0.3515625f, 0f))
		}
	};
}
