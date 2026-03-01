using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using LitJson;
using TMPro;
using UnityEngine;

public class ItemViewer : MonoBehaviour
{
	private void Start()
	{
		this.m_aJIDs = new Dictionary<string, JsonItemDef>();
		this.m_aJCOs = new Dictionary<string, JsonCOOverlay>();
		this.dictColors = new Dictionary<string, Color>();
		this.dictJsonColors = new Dictionary<string, JsonColor>();
		this.LoadModJsons<JsonItemDef>(this.m_directoryName + "data/items/", this.m_aJIDs, new string[0]);
		this.LoadModJsons<JsonCOOverlay>(this.m_directoryName + "data/cooverlays/", this.m_aJCOs, new string[0]);
		this.LoadModJsons<JsonColor>(this.m_directoryName + "data/colors/", this.dictJsonColors, new string[0]);
		this.prevWear = this.m_wearAmt;
		base.StartCoroutine(this.CycleThrough(this.m_aJIDs));
	}

	private void Update()
	{
	}

	public void Increment()
	{
		this.m_goUp++;
	}

	private IEnumerator CycleThrough(Dictionary<string, JsonItemDef> jids)
	{
		bool changed = false;
		Dictionary<string, JsonItemDef>.Enumerator enumerator = jids.GetEnumerator();
		enumerator.MoveNext();
		while (this.m_counter < jids.Count - 1)
		{
			if (this.m_goUp > 0 && this.m_counter < jids.Count - 1)
			{
				changed = true;
				this.m_counter++;
				enumerator.MoveNext();
				this.m_goUp--;
			}
			if (changed)
			{
				changed = false;
				KeyValuePair<string, JsonItemDef> keyValuePair = enumerator.Current;
				JsonItemDef value = keyValuePair.Value;
				this.m_MeshRenderer.material = this.SetUpShader(value);
				string text = string.Empty;
				text = text + "Item No.\t\t" + this.m_counter.ToString() + "\n";
				text = text + "Name:\t\t" + value.strName + "\n";
				text = text + "Image Tex:\t\t" + value.strImg + "\n";
				text = text + "Normal Tex:\t" + value.strImgNorm + "\n";
				text = text + "Dmg Tex:\t\t" + value.strImgDamaged + "\n";
				text = text + "Dmg Color:\t\t" + value.strDmgColor + "\n";
				this.m_InfoText.text = text;
			}
			else if (this.prevWear != this.m_wearAmt)
			{
				this.prevWear = this.m_wearAmt;
				this.dirty = true;
			}
			if (this.dirty)
			{
				this.m_MeshRenderer.material.SetFloat("_Wear", this.m_wearAmt);
			}
			yield return null;
		}
		Debug.Log("Done with items!");
		yield return null;
		this.m_counter = 0;
		Dictionary<string, JsonCOOverlay>.Enumerator enumerator2 = this.m_aJCOs.GetEnumerator();
		enumerator2.MoveNext();
		while (this.m_counter < this.m_aJCOs.Count)
		{
			if (this.m_goUp > 0 && this.m_counter < this.m_aJCOs.Count - 1)
			{
				changed = true;
				this.m_counter++;
				enumerator2.MoveNext();
				this.m_goUp--;
			}
			if (changed)
			{
				changed = false;
				KeyValuePair<string, JsonCOOverlay> keyValuePair2 = enumerator2.Current;
				JsonCOOverlay value2 = keyValuePair2.Value;
				if (!this.m_aJIDs.ContainsKey(value2.strCOBase))
				{
					Debug.LogError("Oh no! Couldn't find base CO for: " + value2.strCOBase);
					continue;
				}
				this.m_MeshRenderer.material = this.SetUpShader(this.m_aJIDs[value2.strCOBase]);
				if (value2.strImg != "blank" && value2.strImg != string.Empty && value2.strImg != null)
				{
					this.m_MeshRenderer.material.SetTexture("_MainTex", this.LoadPNG(value2.strImg + ".png", false));
				}
				if (value2.strImgNorm != "blank" && value2.strImgNorm != string.Empty && value2.strImgNorm != null)
				{
					this.m_MeshRenderer.material.SetTexture("_BumpMap", this.LoadPNG(value2.strImg + ".png", false));
				}
				if (value2.strImgDamaged != "blank" && value2.strImgDamaged != string.Empty && value2.strImgDamaged != null)
				{
					this.m_MeshRenderer.material.SetTexture("_DmgTex", this.LoadPNG(value2.strImgDamaged + ".png", false));
					this.m_MeshRenderer.material.SetFloat("_DmgPresent", 1f);
				}
				if (value2.strDmgColor != "blank" && value2.strDmgColor != string.Empty && value2.strDmgColor != null)
				{
					this.m_MeshRenderer.material.SetVector("_WearCol", this.GetColor(value2.strDmgColor));
				}
				else if (value2.strImgDamaged != "blank")
				{
					this.m_MeshRenderer.material.SetVector("_WearColor", new Color(1f, 1f, 1f));
				}
				else if (this.m_aJIDs[value2.strCOBase].strDmgColor != string.Empty && this.m_aJIDs[value2.strCOBase].strDmgColor != null)
				{
					this.m_MeshRenderer.material.SetVector("_WearColor", DataHandler.GetColor(this.m_aJIDs[value2.strCOBase].strDmgColor));
				}
				string text2 = string.Empty;
				text2 = text2 + "Item No.\t\t" + this.m_counter.ToString() + "\n";
				text2 = text2 + "Name:\t\t" + value2.strName + "\n";
				text2 = text2 + "Image Tex:\t\t" + value2.strImg + "\n";
				text2 = text2 + "Normal Tex:\t" + value2.strImgNorm + "\n";
				text2 = text2 + "Dmg Tex:\t\t" + value2.strImgDamaged + "\n";
				text2 = text2 + "Dmg Color:\t\t" + value2.strDmgColor + "\n";
				this.m_InfoText.text = text2;
			}
			else if (this.prevWear != this.m_wearAmt)
			{
				this.prevWear = this.m_wearAmt;
				this.dirty = true;
			}
			if (this.dirty)
			{
				this.m_MeshRenderer.material.SetFloat("_Wear", this.m_wearAmt);
			}
			yield return null;
		}
		Debug.Log("Done with cooverlays!");
		yield return null;
		yield break;
	}

	private Material SetUpShader(JsonItemDef jid)
	{
		Material material = UnityEngine.Object.Instantiate<Material>(Resources.Load<Material>("Materials/GUIQuad 1"));
		material.renderQueue = 3000;
		if (jid.strImg != "blank" && jid.strImg != string.Empty && jid.strImg != null)
		{
			material.SetTexture("_MainTex", this.LoadPNG(jid.strImg + ".png", false));
		}
		if (jid.strImgNorm != "blank" && jid.strImgNorm != string.Empty && jid.strImgNorm != null)
		{
			material.SetTexture("_BumpMap", this.LoadPNG(jid.strImg + ".png", false));
		}
		if (jid.strImgDamaged != "blank" && jid.strImgDamaged != string.Empty && jid.strImgDamaged != null)
		{
			material.SetTexture("_DmgTex", this.LoadPNG(jid.strImgDamaged + ".png", false));
			material.SetFloat("_DmgPresent", 1f);
		}
		if (jid.fDmgComplexity != 0f)
		{
			material.SetFloat("_Complexity", jid.fDmgComplexity);
		}
		else
		{
			material.SetFloat("_Complexity", 5000f);
		}
		if (jid.fDmgIntensity != 0f)
		{
			material.SetFloat("_Intensity", jid.fDmgIntensity);
		}
		if (jid.fDmgCut != -999f)
		{
			material.SetFloat("_Cut", jid.fDmgCut);
		}
		if (jid.fDmgTrim != -999f)
		{
			material.SetFloat("_Trim", jid.fDmgTrim);
		}
		if (!jid.bLerp)
		{
			material.SetFloat("_Lerp", 0f);
		}
		if (!jid.bSinew)
		{
			material.SetFloat("_Sinew", 0f);
		}
		material.SetVector("_PositionOffset", new Vector4(0f, 0f, 0f, 0f));
		material.SetFloat("_Wear", this.m_wearAmt);
		if (jid.strDmgColor != string.Empty && jid.strDmgColor != null)
		{
			material.SetVector("_WearCol", this.GetColor(jid.strDmgColor));
		}
		else
		{
			material.SetVector("_WearCol", this.GetColor("DamageTintDefault"));
		}
		int nCols = jid.nCols;
		int num = jid.aSocketAdds.Length / jid.nCols;
		if (nCols > 1 || num > 1)
		{
			if (nCols > num)
			{
				this.m_objSize.localScale = new Vector3(1f, 1f, (float)num / (float)nCols);
			}
			else
			{
				this.m_objSize.localScale = new Vector3((float)nCols / (float)num, 1f, 1f);
			}
		}
		else
		{
			this.m_objSize.localScale = new Vector3(1f, 1f, 1f);
		}
		if (material.GetTexture("_MainTex") != null)
		{
			material.SetVector("_Aspect", new Vector4((float)nCols, (float)num, (float)material.GetTexture("_MainTex").width, (float)material.GetTexture("_MainTex").height));
		}
		else
		{
			material.SetVector("_Aspect", new Vector4((float)nCols, (float)num, 16f, 16f));
		}
		switch (jid.nDmgMode)
		{
		case 1:
			material.SetFloat("_DmgPassThrough", 1f);
			material.SetFloat("_DmgExtend", 0f);
			break;
		case 2:
			material.SetFloat("_DmgPassThrough", 0f);
			material.SetFloat("_DmgExtend", 1f);
			break;
		case 3:
			material.SetFloat("_DmgPassThrough", 1f);
			material.SetFloat("_DmgExtend", 1f);
			break;
		}
		return material;
	}

	private void LoadModJsons<TJson>(string strFolderPath, Dictionary<string, TJson> dict, string[] aIgnorePatterns)
	{
		if (Directory.Exists(strFolderPath))
		{
			string[] files = Directory.GetFiles(strFolderPath, "*.json", SearchOption.AllDirectories);
			foreach (string text in files)
			{
				bool flag = false;
				if (aIgnorePatterns != null)
				{
					foreach (string value in aIgnorePatterns)
					{
						if (text.IndexOf(value) >= 0)
						{
							flag = true;
							break;
						}
					}
				}
				if (flag)
				{
					Debug.LogWarning("Ignore Pattern match: " + text + "; Skipping...");
				}
				else
				{
					this.JsonToData<TJson>(text, dict);
				}
			}
		}
	}

	private void JsonToData<TJson>(string strFile, Dictionary<string, TJson> dict)
	{
		Debug.Log("Loading " + strFile);
		string text = string.Empty;
		try
		{
			string json = File.ReadAllText(strFile, Encoding.UTF8);
			text += "Converting json into Array...\n";
			TJson[] array = JsonMapper.ToObject<TJson[]>(json);
			foreach (TJson tjson in array)
			{
				text += "Getting key: ";
				Type type = tjson.GetType();
				PropertyInfo property = type.GetProperty("strName");
				object value = property.GetValue(tjson, null);
				string text2 = value.ToString();
				text = text + text2 + "\n";
				if (dict.ContainsKey(text2))
				{
					Debug.Log("Warning: Trying to add " + text2 + " twice.");
					dict[text2] = tjson;
				}
				else
				{
					dict.Add(text2, tjson);
				}
			}
		}
		catch (Exception ex)
		{
			if (text.Length > 1000)
			{
				text = text.Substring(text.Length - 1000);
			}
			Debug.LogError(string.Concat(new string[]
			{
				text,
				"\n",
				ex.Message,
				"\n",
				ex.StackTrace.ToString()
			}));
		}
		if (strFile.IndexOf("osSGv1") >= 0)
		{
			Debug.Log(text);
		}
	}

	private Color GetColor(string strName)
	{
		if (this.dictColors.ContainsKey(strName))
		{
			return this.dictColors[strName];
		}
		if (!this.dictJsonColors.ContainsKey(strName))
		{
			Debug.Log("Color not found: " + strName);
			return Color.magenta;
		}
		this.dictColors[strName] = this.dictJsonColors[strName].GetColor();
		return this.dictColors[strName];
	}

	private Texture2D LoadPNG(string strFileName, bool bNorm)
	{
		if (strFileName == "ItmSink01.png")
		{
		}
		string text = this.m_directoryName + "images/" + strFileName;
		if (File.Exists(text))
		{
			byte[] data = File.ReadAllBytes(text);
			Texture2D texture2D = new Texture2D(2, 2, TextureFormat.ARGB32, false);
			texture2D.filterMode = FilterMode.Point;
			texture2D.wrapMode = TextureWrapMode.Clamp;
			texture2D.name = text;
			texture2D.LoadImage(data);
			if (bNorm)
			{
				texture2D = ShaderSetup.NormalPNGtoDXTnm(texture2D);
			}
			return texture2D;
		}
		Debug.Log("Unable to load PNG: " + strFileName);
		return Resources.Load("Sprites/missing") as Texture2D;
	}

	[SerializeField]
	private string m_directoryName;

	[SerializeField]
	private Transform m_objSize;

	[SerializeField]
	private MeshRenderer m_MeshRenderer;

	[SerializeField]
	private TextMeshProUGUI m_InfoText;

	private int m_counter;

	[SerializeField]
	private int m_goUp;

	[SerializeField]
	[Range(0f, 1f)]
	private float m_wearAmt;

	public Dictionary<string, JsonItemDef> m_aJIDs;

	public Dictionary<string, JsonCOOverlay> m_aJCOs;

	private Dictionary<string, Color> dictColors;

	public Dictionary<string, JsonColor> dictJsonColors;

	private float prevWear;

	private bool dirty;
}
