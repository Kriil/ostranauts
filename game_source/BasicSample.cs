using System;
using SFB;
using UnityEngine;

public class BasicSample : MonoBehaviour
{
	private void OnGUI()
	{
		Vector3 s = new Vector3((float)Screen.width / 800f, (float)Screen.height / 600f, 1f);
		GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, s);
		GUILayout.Space(20f);
		GUILayout.BeginHorizontal(new GUILayoutOption[0]);
		GUILayout.Space(20f);
		GUILayout.BeginVertical(new GUILayoutOption[0]);
		if (GUILayout.Button("Open File", new GUILayoutOption[0]))
		{
			this.WriteResult(StandaloneFileBrowser.OpenFilePanel("Open File", string.Empty, string.Empty, false));
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Open File Async", new GUILayoutOption[0]))
		{
			StandaloneFileBrowser.OpenFilePanelAsync("Open File", string.Empty, string.Empty, false, delegate(string[] paths)
			{
				this.WriteResult(paths);
			});
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Open File Multiple", new GUILayoutOption[0]))
		{
			this.WriteResult(StandaloneFileBrowser.OpenFilePanel("Open File", string.Empty, string.Empty, true));
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Open File Extension", new GUILayoutOption[0]))
		{
			this.WriteResult(StandaloneFileBrowser.OpenFilePanel("Open File", string.Empty, "txt", true));
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Open File Directory", new GUILayoutOption[0]))
		{
			this.WriteResult(StandaloneFileBrowser.OpenFilePanel("Open File", Application.dataPath, string.Empty, true));
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Open File Filter", new GUILayoutOption[0]))
		{
			ExtensionFilter[] extensions = new ExtensionFilter[]
			{
				new ExtensionFilter("Image Files", new string[]
				{
					"png",
					"jpg",
					"jpeg"
				}),
				new ExtensionFilter("Sound Files", new string[]
				{
					"mp3",
					"wav"
				}),
				new ExtensionFilter("All Files", new string[]
				{
					"*"
				})
			};
			this.WriteResult(StandaloneFileBrowser.OpenFilePanel("Open File", string.Empty, extensions, true));
		}
		GUILayout.Space(15f);
		if (GUILayout.Button("Open Folder", new GUILayoutOption[0]))
		{
			string[] paths3 = StandaloneFileBrowser.OpenFolderPanel("Select Folder", string.Empty, true);
			this.WriteResult(paths3);
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Open Folder Async", new GUILayoutOption[0]))
		{
			StandaloneFileBrowser.OpenFolderPanelAsync("Select Folder", string.Empty, true, delegate(string[] paths)
			{
				this.WriteResult(paths);
			});
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Open Folder Directory", new GUILayoutOption[0]))
		{
			string[] paths2 = StandaloneFileBrowser.OpenFolderPanel("Select Folder", Application.dataPath, true);
			this.WriteResult(paths2);
		}
		GUILayout.Space(15f);
		if (GUILayout.Button("Save File", new GUILayoutOption[0]))
		{
			this._path = StandaloneFileBrowser.SaveFilePanel("Save File", string.Empty, string.Empty, string.Empty);
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Save File Async", new GUILayoutOption[0]))
		{
			StandaloneFileBrowser.SaveFilePanelAsync("Save File", string.Empty, string.Empty, string.Empty, delegate(string path)
			{
				this.WriteResult(path);
			});
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Save File Default Name", new GUILayoutOption[0]))
		{
			this._path = StandaloneFileBrowser.SaveFilePanel("Save File", string.Empty, "MySaveFile", string.Empty);
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Save File Default Name Ext", new GUILayoutOption[0]))
		{
			this._path = StandaloneFileBrowser.SaveFilePanel("Save File", string.Empty, "MySaveFile", "dat");
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Save File Directory", new GUILayoutOption[0]))
		{
			this._path = StandaloneFileBrowser.SaveFilePanel("Save File", Application.dataPath, string.Empty, string.Empty);
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Save File Filter", new GUILayoutOption[0]))
		{
			ExtensionFilter[] extensions2 = new ExtensionFilter[]
			{
				new ExtensionFilter("Binary", new string[]
				{
					"bin"
				}),
				new ExtensionFilter("Text", new string[]
				{
					"txt"
				})
			};
			this._path = StandaloneFileBrowser.SaveFilePanel("Save File", string.Empty, "MySaveFile", extensions2);
		}
		GUILayout.EndVertical();
		GUILayout.Space(20f);
		GUILayout.Label(this._path, new GUILayoutOption[0]);
		GUILayout.EndHorizontal();
	}

	public void WriteResult(string[] paths)
	{
		if (paths.Length == 0)
		{
			return;
		}
		this._path = string.Empty;
		foreach (string str in paths)
		{
			this._path = this._path + str + "\n";
		}
	}

	public void WriteResult(string path)
	{
		this._path = path;
	}

	private string _path;
}
