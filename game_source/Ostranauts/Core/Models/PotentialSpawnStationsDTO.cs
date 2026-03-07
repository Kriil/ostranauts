using System;
using System.Collections.Generic;

namespace Ostranauts.Core.Models
{
	public class PotentialSpawnStationsDTO
	{
		public List<Tuple<Ship, int>> DerelictSpawnerStations = new List<Tuple<Ship, int>>();

		public List<Tuple<Ship, int>> DerelictCollectorStations = new List<Tuple<Ship, int>>();
	}
}
