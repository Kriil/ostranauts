using System;
using System.Collections.Generic;
using UnityEngine;

public class BBGList : PropertyAttribute
{
	public List<string> aList = new List<string>
	{
		"TIsShakedownModeActive",
		"TIsShakedownTarget",
		"TIsNotPlayerCrew",
		"TCanBeHired"
	};
}
