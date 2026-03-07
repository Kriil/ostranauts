using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class JsonActionKey
{
	public string strName { get; set; }

	public int nEnum { get; set; }

	public List<List<KeyCode>> lKeyCodes { get; set; }
}
