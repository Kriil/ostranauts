using System;
using System.Collections.Generic;

public class JsonTransit
{
	public string strName { get; set; }

	public string strCustomPrefabPathOptional { get; set; }

	public JsonTransitConnection[] aConnections { get; set; }

	public List<JsonTransitConnection> GetConnectionsForKiosk(CondOwner coKiosk)
	{
		if (this.aConnections == null)
		{
			return null;
		}
		List<JsonTransitConnection> list = new List<JsonTransitConnection>();
		foreach (JsonTransitConnection jsonTransitConnection in this.aConnections)
		{
			if (!jsonTransitConnection.bHide)
			{
				if (string.IsNullOrEmpty(jsonTransitConnection.ctKioskOrigin))
				{
					list.Add(jsonTransitConnection);
				}
				else
				{
					CondTrigger condTrigger = DataHandler.GetCondTrigger(jsonTransitConnection.ctKioskOrigin);
					if (condTrigger.Triggered(coKiosk, null, true))
					{
						if (jsonTransitConnection.TargetsWildCard)
						{
							bool flag = false;
							foreach (KeyValuePair<string, Ship> keyValuePair in CrewSim.system.dictShips)
							{
								if (keyValuePair.Key.Contains(jsonTransitConnection.strTargetRegID))
								{
									flag = true;
									list.Add(new JsonTransitConnection
									{
										strName = jsonTransitConnection.strName,
										strLabelNameOptional = jsonTransitConnection.strLabelNameOptional + " | " + keyValuePair.Key,
										ctUserOptional = jsonTransitConnection.ctUserOptional,
										strTargetRegID = keyValuePair.Key,
										ctKioskOrigin = jsonTransitConnection.ctKioskOrigin,
										ctKioskDestination = jsonTransitConnection.ctKioskDestination
									});
								}
							}
							if (!flag)
							{
								list.Add(new JsonTransitConnection
								{
									strName = jsonTransitConnection.strName,
									strLabelNameOptional = jsonTransitConnection.strLabelNameOptional,
									ctUserOptional = "TIsDead",
									strTargetRegID = jsonTransitConnection.strTargetRegID,
									ctKioskOrigin = jsonTransitConnection.ctKioskOrigin,
									ctKioskDestination = jsonTransitConnection.ctKioskDestination
								});
							}
						}
						else
						{
							list.Add(jsonTransitConnection);
						}
					}
				}
			}
		}
		return list;
	}

	public static bool IsTransitConnected(string strRegIDHere, string strRegIDThere)
	{
		if (strRegIDHere == null || strRegIDThere == null)
		{
			return false;
		}
		foreach (KeyValuePair<string, JsonTransit> keyValuePair in DataHandler.dictTransit)
		{
			if (keyValuePair.Value != null && !(keyValuePair.Value.strName != strRegIDHere) && keyValuePair.Value.aConnections != null)
			{
				foreach (JsonTransitConnection jsonTransitConnection in keyValuePair.Value.aConnections)
				{
					if (jsonTransitConnection.strTargetRegID == strRegIDThere)
					{
						return true;
					}
				}
			}
		}
		return false;
	}
}
