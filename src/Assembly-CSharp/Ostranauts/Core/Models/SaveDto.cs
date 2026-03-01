using System;
using System.Collections.Generic;

namespace Ostranauts.Core.Models
{
	public class SaveDto
	{
		public void AddShip(JsonShip jShip, string path)
		{
			this.dictShips.Add(path, jShip);
		}

		public void AddShipImages(Dictionary<string, byte[]> images, string path)
		{
			if (images == null)
			{
				return;
			}
			this.dictShipImages.Add(path, images);
		}

		public void AddGameSave(JsonGameSave jgame, string path)
		{
			this.jGameSave = new Tuple<string, JsonGameSave>(path, jgame);
		}

		public void AddSaveInfo(JsonSaveInfo jgame, string path)
		{
			this.jSaveInfo = new Tuple<string, JsonSaveInfo>(path, jgame);
		}

		public Dictionary<string, JsonShip> dictShips = new Dictionary<string, JsonShip>();

		public Tuple<string, JsonGameSave> jGameSave = new Tuple<string, JsonGameSave>();

		public Tuple<string, JsonSaveInfo> jSaveInfo = new Tuple<string, JsonSaveInfo>();

		public Dictionary<string, Dictionary<string, byte[]>> dictShipImages = new Dictionary<string, Dictionary<string, byte[]>>();

		public string filepath = string.Empty;

		public string persistenpath = string.Empty;
	}
}
