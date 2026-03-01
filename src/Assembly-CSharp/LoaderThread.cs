using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Ostranauts.Core;
using UnityEngine;

public class LoaderThread
{
	public void Run()
	{
		for (int i = 0; i < this.fileLoaders.Count; i++)
		{
			ModLoader.LoadFile loadDelegate = this.fileLoaders[i].loadDelegate;
			object obj = this.handle;
			lock (obj)
			{
				if (this.terminate)
				{
					if (this.t != null && this.t.IsAlive)
					{
						this.t.Abort();
					}
					break;
				}
			}
			try
			{
				if (loadDelegate != null)
				{
					loadDelegate();
					object outputLock = LoadManager.outputLock;
					lock (outputLock)
					{
						LoadManager.fileNamesLoaded.Add(Path.GetFileNameWithoutExtension(this.fileLoaders[i].fileName));
					}
				}
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
		}
		object obj2 = this.handle;
		lock (obj2)
		{
			this.complete = true;
		}
	}

	public Thread t;

	public List<FileLoader> fileLoaders = new List<FileLoader>();

	public object handle = new object();

	public bool terminate;

	public bool complete;
}
