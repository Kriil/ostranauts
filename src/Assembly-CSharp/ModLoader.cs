using System;
using System.Collections.Generic;

public class ModLoader
{
	public FileLoader AddDelegate(ModLoader.LoadFile loadDataFolder)
	{
		FileLoader fileLoader = new FileLoader();
		FileLoader fileLoader2 = fileLoader;
		fileLoader2.loadDelegate = (ModLoader.LoadFile)Delegate.Combine(fileLoader2.loadDelegate, loadDataFolder);
		this.fileLoaders.Add(fileLoader);
		return fileLoader;
	}

	public FileLoader AddShip(ModLoader.LoadFile loadDataFolder)
	{
		FileLoader fileLoader = new FileLoader();
		FileLoader fileLoader2 = fileLoader;
		fileLoader2.loadDelegate = (ModLoader.LoadFile)Delegate.Combine(fileLoader2.loadDelegate, loadDataFolder);
		this.ships.Add(fileLoader);
		return fileLoader;
	}

	public bool complete;

	public JsonModInfo JsonModInfo;

	public List<FileLoader> fileLoaders = new List<FileLoader>();

	public List<FileLoader> ships = new List<FileLoader>();

	public List<Action> PerModPostLoadAsyncOkay = new List<Action>();

	public delegate void LoadFile();
}
