using System;

public class WaypointShip
{
	public WaypointShip(ShipSitu obj, float fT)
	{
		this.objSS = obj;
		this.fTime = fT;
	}

	public void Destroy()
	{
		this.objSS.destroy();
		this.objSS = null;
	}

	public ShipSitu objSS;

	public float fTime;
}
