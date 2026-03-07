using System;
using System.Collections.Generic;
using Ostranauts.Utils;
using UnityEngine;

namespace Ostranauts.Core.Models
{
	public class SaveInfo
	{
		public SaveInfo(JsonSaveInfo jsonSaveInfo, string pathToSaveFolder)
		{
			this._jsonSaveInfo = jsonSaveInfo;
			if (this._jsonSaveInfo.epochCreationTime == 0L)
			{
				this._jsonSaveInfo.epochCreationTime = TimeUtils.ConvertStringDate(this._jsonSaveInfo.realWorldTime);
			}
			this.Path = pathToSaveFolder + "/";
		}

		public Texture2D Texture { get; set; }

		public Texture2D ScreenShot { get; set; }

		public List<Texture2D> CrewPortraits { get; set; }

		public string PlayerName
		{
			get
			{
				return this._jsonSaveInfo.playerName;
			}
		}

		public string ShipName
		{
			get
			{
				return this._jsonSaveInfo.shipName;
			}
		}

		public string Path { get; private set; }

		public string PathPlayer
		{
			get
			{
				return this.Path + this.PlayerName + ".json";
			}
		}

		public string PathShipsFolder
		{
			get
			{
				return this.Path + "ships/";
			}
		}

		public string Timestamp
		{
			get
			{
				if (this._jsonSaveInfo.epochCreationTime != 0L)
				{
					return TimeUtils.FromUnixTimeMillis(this._jsonSaveInfo.epochCreationTime).ToString();
				}
				return "Creation date missing";
			}
		}

		public string Version
		{
			get
			{
				return this._jsonSaveInfo.version ?? "Early Access Build: Pre-0.6.4.1";
			}
		}

		public string TotalPlayTime
		{
			get
			{
				return MathUtils.GetDurationFromS((double)this._jsonSaveInfo.playTimeElapsed, 4);
			}
		}

		public long EpochTimeStamp
		{
			get
			{
				return this._jsonSaveInfo.epochCreationTime;
			}
		}

		public string GetWorldSeedID()
		{
			if (string.IsNullOrEmpty(this._jsonSaveInfo.seedId))
			{
				this._jsonSaveInfo.seedId = DataHandler.GetNextID();
			}
			return this._jsonSaveInfo.seedId;
		}

		public bool IsAutoSave
		{
			get
			{
				return this._jsonSaveInfo.autoSaveCounter > 0;
			}
		}

		public int AutoSaveCounter
		{
			get
			{
				return (this._jsonSaveInfo.autoSaveCounter >= 0) ? this._jsonSaveInfo.autoSaveCounter : 0;
			}
		}

		public string SaveName
		{
			get
			{
				return this._jsonSaveInfo.strName;
			}
			set
			{
				this._jsonSaveInfo.strName = value;
			}
		}

		public void Destroy()
		{
			UnityEngine.Object.Destroy(this.Texture);
			UnityEngine.Object.Destroy(this.ScreenShot);
			this.Texture = null;
			this.ScreenShot = null;
			if (this.CrewPortraits != null)
			{
				foreach (Texture2D obj in this.CrewPortraits)
				{
					UnityEngine.Object.Destroy(obj);
				}
				this.CrewPortraits.Clear();
				this.CrewPortraits = null;
			}
		}

		public readonly JsonSaveInfo _jsonSaveInfo;
	}
}
