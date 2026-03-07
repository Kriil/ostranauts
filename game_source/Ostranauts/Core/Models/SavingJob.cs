using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Ostranauts.Tools;
using Ostranauts.Utils;

namespace Ostranauts.Core.Models
{
	public class SavingJob
	{
		public bool IsDone
		{
			get
			{
				object handle = this._handle;
				bool isDone;
				lock (handle)
				{
					isDone = this._isDone;
				}
				return isDone;
			}
			private set
			{
				object handle = this._handle;
				lock (handle)
				{
					this._isDone = value;
				}
			}
		}

		private void ThreadFunction()
		{
			this.Exception = null;
			foreach (KeyValuePair<string, JsonShip> keyValuePair in this.SaveDto.dictShips)
			{
				this.Exception = DataHandler.DataToJsonStreaming<JsonShip>(new Dictionary<string, JsonShip>
				{
					{
						keyValuePair.Key,
						keyValuePair.Value
					}
				}, keyValuePair.Key, true, this.SaveDto.persistenpath);
				if (this.Exception != null)
				{
					return;
				}
				if (this._stop)
				{
					break;
				}
			}
			foreach (KeyValuePair<string, Dictionary<string, byte[]>> keyValuePair2 in this.SaveDto.dictShipImages)
			{
				MonoSingleton<ScreenshotUtil>.Instance.SaveByteArrayToDisk(keyValuePair2.Key, keyValuePair2.Value, true);
				if (this._stop)
				{
					break;
				}
			}
			if (!this._stop)
			{
				this.Exception = DataHandler.DataToJsonStreaming<JsonGameSave>(new Dictionary<string, JsonGameSave>
				{
					{
						this.SaveDto.filepath,
						this.SaveDto.jGameSave.Item2
					}
				}, this.SaveDto.jGameSave.Item1, true, this.SaveDto.persistenpath);
				if (this.Exception != null)
				{
					return;
				}
			}
			if (!this._stop)
			{
				this.Exception = DataHandler.DataToJsonStreaming<JsonSaveInfo>(new Dictionary<string, JsonSaveInfo>
				{
					{
						this.SaveDto.filepath,
						this.SaveDto.jSaveInfo.Item2
					}
				}, this.SaveDto.jSaveInfo.Item1, true, this.SaveDto.persistenpath);
				if (this.Exception != null)
				{
					return;
				}
			}
			if (!this._stop)
			{
				this.Exception = LoadManager.CompressSaveFolder(this.SaveDto.filepath, new DotNetZipCompressor());
			}
			if (this._stop)
			{
				if (!Directory.Exists(this.SaveDto.filepath))
				{
					return;
				}
				try
				{
					Directory.Delete(this.SaveDto.filepath, true);
				}
				catch (Exception exception)
				{
					this.Exception = exception;
				}
			}
		}

		private bool Update()
		{
			return this.IsDone;
		}

		private void Run()
		{
			this.ThreadFunction();
			this.IsDone = true;
		}

		public void Start()
		{
			this._thread = new Thread(new ThreadStart(this.Run));
			this._thread.Start();
		}

		public void StartWithoutThreading()
		{
			this.Run();
		}

		public IEnumerator WaitFor()
		{
			while (!this.Update())
			{
				yield return null;
			}
			yield break;
		}

		public void Abort()
		{
			if (this._thread != null && this._thread.IsAlive)
			{
				this._thread.Abort();
			}
		}

		public void AbortSafe()
		{
			if (this._thread != null)
			{
				this._stop = true;
			}
		}

		private bool _isDone;

		private object _handle = new object();

		public Thread _thread;

		public SaveDto SaveDto;

		public Exception Exception;

		private bool _stop;
	}
}
