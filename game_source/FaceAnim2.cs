using System;
using System.Collections.Generic;
using Anima2D;
using UnityEngine;

public class FaceAnim2 : MonoBehaviour
{
	private static string[] FacePartNames
	{
		get
		{
			if (FaceAnim2._facePartNames == null)
			{
				FaceAnim2._facePartNames = DataHandler.GetLoot("TXTFacePartNames").GetLootNames(null, false, null).ToArray();
			}
			return FaceAnim2._facePartNames;
		}
	}

	private static string[] FacePartOrder
	{
		get
		{
			if (FaceAnim2._facePartOrder == null)
			{
				FaceAnim2._facePartOrder = DataHandler.GetLoot("TXTFacePartOrder").GetLootNames(null, false, null).ToArray();
			}
			return FaceAnim2._facePartOrder;
		}
	}

	private void Start()
	{
		this.fFaceOffsetX = base.transform.position.x;
		this.SetFace(this.coLast, false);
	}

	private void Update()
	{
		this.GetEmoteState();
		this.nBlinkState = 0;
		this.fMinPupilDuration -= Time.deltaTime;
		bool flag = false;
		bool flag2 = false;
		if (this.coLast != null)
		{
			if (this.coLast.HasCond("Unconscious"))
			{
				flag = true;
			}
			if (!this.coLast.bAlive)
			{
				flag2 = true;
			}
		}
		if (flag)
		{
			float value = 0f;
			float value2 = 1f;
			this.anim.SetFloat("fPupilX", value);
			this.anim.SetFloat("fPupilY", value2);
		}
		else if (!flag2 && UnityEngine.Random.Range(0f, 1f) < 0.0025f)
		{
			this.nBlinkState = 1;
		}
		if (!flag2 && !flag && this.fMinPupilDuration <= 0f && UnityEngine.Random.Range(0f, 1f) < 0.025f)
		{
			float value3 = UnityEngine.Random.Range(0f, 2f) - 1f;
			float num = UnityEngine.Random.Range(0f, 2f) - 1f;
			if (num > 0.5f)
			{
				num -= 0.5f;
			}
			this.anim.SetFloat("fPupilX", value3);
			this.anim.SetFloat("fPupilY", num);
			this.fMinPupilDuration = 0.5f;
		}
		if ((double)Time.realtimeSinceStartup - this.fEpochOverride <= 2.0)
		{
			this.anim.SetInteger("nState", this.nStateOverride);
		}
		else
		{
			this.anim.SetInteger("nState", this.nState);
		}
		this.anim.SetInteger("nBlinkState", this.nBlinkState);
	}

	private void Init()
	{
		this.aEmoteCounts = new int[6];
		FaceAnim2.dictEmotes = new Dictionary<int, string[]>();
		FaceAnim2.dictEmotes[5] = DataHandler.GetLoot("CONDFacePain").GetLootNames(null, false, null).ToArray();
		FaceAnim2.dictEmotes[4] = DataHandler.GetLoot("CONDFaceSad").GetLootNames(null, false, null).ToArray();
		FaceAnim2.dictEmotes[3] = DataHandler.GetLoot("CONDFaceFear").GetLootNames(null, false, null).ToArray();
		FaceAnim2.dictEmotes[2] = DataHandler.GetLoot("CONDFaceAngry").GetLootNames(null, false, null).ToArray();
		FaceAnim2.dictEmotes[1] = DataHandler.GetLoot("CONDFaceHappy").GetLootNames(null, false, null).ToArray();
		this.anim = base.gameObject.GetComponentInChildren<Animator>();
	}

	private void GetEmoteState()
	{
		if (this.aEmoteCounts[5] > 0)
		{
			this.nState = 5;
		}
		else if (this.aEmoteCounts[3] > 0)
		{
			this.nState = 3;
		}
		else if (this.aEmoteCounts[2] > 0)
		{
			this.nState = 2;
		}
		else if (this.aEmoteCounts[1] > 0)
		{
			this.nState = 1;
		}
		else if (this.aEmoteCounts[4] > 0)
		{
			this.nState = 4;
		}
		else
		{
			this.nState = 0;
		}
	}

	public void SetEmoteStateOverride(int nStateNew)
	{
		this.nStateOverride = nStateNew;
		this.fEpochOverride = (double)Time.realtimeSinceStartup;
	}

	public void RecordCond(string strCond, bool bRemove)
	{
		bool flag = false;
		for (int i = 1; i <= 5; i++)
		{
			if (Array.IndexOf<string>(FaceAnim2.dictEmotes[i], strCond) >= 0)
			{
				if (bRemove)
				{
					this.aEmoteCounts[i]--;
				}
				else
				{
					this.aEmoteCounts[i]++;
				}
				if (this.aEmoteCounts[i] < 0)
				{
					this.aEmoteCounts[i] = 0;
				}
				flag = true;
				break;
			}
		}
		if (flag)
		{
			this.GetEmoteState();
		}
	}

	public void SetFace(CondOwner co, bool bForce = false)
	{
		if (this.aEmoteCounts == null)
		{
			this.Init();
		}
		if (!bForce && co == this.coLast)
		{
			return;
		}
		Crew crew = null;
		if (co != null)
		{
			crew = co.Crew;
		}
		if (crew == null)
		{
			this.aFaceParts = FaceAnim2.GetRandomFace(true, true, null);
		}
		else if (crew.FaceParts == null || crew.FaceParts.Length < 9)
		{
			bool flag = co.HasCond("IsNB");
			bool flag2 = co.HasCond("IsMale");
			bool flag3 = co.HasCond("IsFemale");
			string strSkin = null;
			if (co.pspec != null)
			{
				strSkin = co.pspec.strSkin;
			}
			this.aFaceParts = FaceAnim2.GetRandomFace(flag || flag2, flag || flag3, strSkin);
		}
		else
		{
			this.aFaceParts = crew.FaceParts;
			if (this.coLast != null)
			{
				this.coLast.faceRef = null;
			}
			co.faceRef = this;
			this.coLast = co;
			for (int i = 0; i < this.aEmoteCounts.Length; i++)
			{
				this.aEmoteCounts[i] = 0;
			}
			foreach (string strCond in co.mapConds.Keys)
			{
				this.RecordCond(strCond, false);
			}
			this.GetEmoteState();
		}
		if (co.pspec != null)
		{
			co.pspec.strSkin = FaceAnim2.GetFaceGroups(this.aFaceParts)[0];
		}
		this.LoadFace();
	}

	public static string[] GetRandomFace(bool bMale, bool bFemale, string strSkin = null)
	{
		string[] array = new string[FaceAnim2.FacePartNames.Length];
		string str = "A";
		if (string.IsNullOrEmpty(strSkin))
		{
			List<string> lootNames = DataHandler.GetLoot("TXTPortraitType").GetLootNames("TXTPortraitType", false, null);
			if (lootNames.Count >= 1)
			{
				str = lootNames[0];
			}
		}
		else
		{
			List<string> allLootNames = DataHandler.GetLoot("TXTPortraitType").GetAllLootNames();
			if (allLootNames.IndexOf(strSkin) >= 0)
			{
				str = strSkin;
			}
		}
		string str2 = "Nonbinary";
		if (bMale && !bFemale)
		{
			str2 = "Male";
		}
		if (bFemale && !bMale)
		{
			str2 = "Female";
		}
		List<string> lootNames2 = DataHandler.GetLoot("TXTPortrait" + str + str2).GetLootNames("TXTPortrait", false, null);
		if (array.Length == lootNames2.Count)
		{
			lootNames2.CopyTo(array);
		}
		else
		{
			for (int i = 0; i < lootNames2.Count; i++)
			{
				Debug.Log(string.Concat(new object[]
				{
					"aPartNames[",
					i,
					"] = ",
					FaceAnim2.FacePartNames[i],
					"; aParts[",
					i,
					"] = ",
					lootNames2[i]
				}));
			}
		}
		return array;
	}

	public static string[] GetFaceGroups(string[] aFaceParts)
	{
		return new string[]
		{
			aFaceParts[2].Substring(FaceAnim2.strPrefix.Length + FaceAnim2.FacePartNames[2].Length, 1),
			DataHandler.GetCrewSkin(aFaceParts[9])
		};
	}

	private void LoadFace()
	{
		Texture2D texture2D = null;
		Texture2D texture2D2 = null;
		Texture2D texture2D3 = null;
		Texture2D texture2D4 = null;
		Texture2D texture2D5 = null;
		string text = string.Empty;
		for (int i = 0; i < FaceAnim2.FacePartNames.Length; i++)
		{
			if (i == 0)
			{
				texture2D = DataHandler.LoadPNG("portraits/" + this.aFaceParts[i] + ".png", false, false);
				text += this.aFaceParts[i];
			}
			else if (i == 2)
			{
				texture2D2 = DataHandler.LoadPNG("portraits/" + this.aFaceParts[i] + ".png", false, false);
				text += this.aFaceParts[i];
			}
			else if (i == 3)
			{
				texture2D3 = DataHandler.LoadPNG("portraits/" + this.aFaceParts[i] + ".png", false, false);
				text += this.aFaceParts[i];
			}
			else if (i == 5)
			{
				texture2D4 = DataHandler.LoadPNG("portraits/" + this.aFaceParts[i] + ".png", false, false);
				text += this.aFaceParts[i];
			}
			else if (i == 7)
			{
				texture2D5 = DataHandler.LoadPNG("portraits/" + this.aFaceParts[i] + ".png", false, false);
				text += this.aFaceParts[i];
			}
			else
			{
				Transform transform = base.transform.Find("root2/" + FaceAnim2.strPrefix + FaceAnim2.FacePartNames[i]);
				SpriteMeshInstance component = transform.GetComponent<SpriteMeshInstance>();
				component.m_SpriteTexOverride = DataHandler.LoadPNG("portraits/" + this.aFaceParts[i] + ".png", false, false);
			}
		}
		Texture2D texture2D6 = DataHandler.LoadPNG(text, false, false);
		if (texture2D6.height != texture2D2.height)
		{
			int width = texture2D2.width;
			int height = texture2D2.height;
			texture2D6 = new Texture2D(width, height);
			texture2D6.filterMode = FilterMode.Point;
			for (int j = 0; j < width; j++)
			{
				for (int k = 0; k < height; k++)
				{
					Color pixel = texture2D.GetPixel(j, k);
					Color pixel2 = texture2D2.GetPixel(j, k);
					Color pixel3 = texture2D3.GetPixel(j, k);
					Color pixel4 = texture2D4.GetPixel(j, k);
					Color pixel5 = texture2D5.GetPixel(j, k);
					Color color = Color.Lerp(pixel2, pixel, pixel.a / 1f);
					color = Color.Lerp(color, pixel4, pixel4.a / 1f);
					color = Color.Lerp(color, pixel3, pixel3.a / 1f);
					color = Color.Lerp(color, pixel5, pixel5.a / 1f);
					texture2D6.SetPixel(j, k, color);
				}
			}
			texture2D6.Apply();
			texture2D6.name = text;
			DataHandler.AddPNG(text, texture2D6);
		}
		Transform transform2 = base.transform.Find("root2/pbaseFaceMerged01");
		SpriteMeshInstance component2 = transform2.GetComponent<SpriteMeshInstance>();
		component2.m_SpriteTexOverride = texture2D6;
	}

	public static Texture2D GetPNG(CondOwner co)
	{
		if (co == null)
		{
			return DataHandler.LoadPNG("blank.png", false, false);
		}
		Crew crew = co.Crew;
		if (crew == null)
		{
			return DataHandler.LoadPNG(co.strPortraitImg, false, false);
		}
		string[] faceParts = crew.FaceParts;
		string text = string.Empty;
		Texture2D[] array = new Texture2D[FaceAnim2.FacePartNames.Length];
		int[] array2 = new int[FaceAnim2.FacePartOrder.Length];
		for (int i = 0; i < FaceAnim2.FacePartOrder.Length; i++)
		{
			int num = 0;
			if (int.TryParse(FaceAnim2.FacePartOrder[i], out num))
			{
				array2[i] = num;
			}
		}
		int num2 = 0;
		foreach (int num3 in array2)
		{
			text += faceParts[num3];
			array[num2] = DataHandler.LoadPNG("portraits/" + faceParts[num3] + ".png", false, false);
			num2++;
		}
		Texture2D texture2D = DataHandler.LoadPNG(text, false, false);
		if (texture2D == null || texture2D.height != array[2].height)
		{
			int width = array[2].width;
			int height = array[2].height;
			texture2D = new Texture2D(width, height);
			texture2D.filterMode = FilterMode.Point;
			for (int k = 0; k < width; k++)
			{
				for (int l = 0; l < height; l++)
				{
					Color color = Color.magenta;
					bool flag = true;
					foreach (Texture2D texture2D2 in array)
					{
						if (flag)
						{
							Color pixel = texture2D2.GetPixel(k, l);
							color = Color.Lerp(Color.black, pixel, pixel.a / 1f);
							flag = false;
						}
						else
						{
							Color pixel2 = texture2D2.GetPixel(k, l);
							color = Color.Lerp(color, pixel2, pixel2.a / 1f);
						}
					}
					texture2D.SetPixel(k, l, color);
				}
			}
			texture2D.Apply();
			texture2D.name = text;
			DataHandler.AddPNG(text, texture2D);
		}
		return texture2D;
	}

	public static string PartNameDefault(int nIndex)
	{
		if (FaceAnim2.aPartNameDefaults == null)
		{
			FaceAnim2.aPartNameDefaults = DataHandler.GetLoot("TXTPortraitDefault").GetAllLootNames();
		}
		if (nIndex < 0 || nIndex >= FaceAnim2.aPartNameDefaults.Count)
		{
			return "missing";
		}
		return FaceAnim2.aPartNameDefaults[nIndex];
	}

	private Animator anim;

	public const int STATE_IDLE = 0;

	public const int STATE_HAPPY = 1;

	public const int STATE_ANGRY = 2;

	public const int STATE_FEAR = 3;

	public const int STATE_SAD = 4;

	public const int STATE_PAIN = 5;

	private static string[] _facePartNames;

	private static string[] _facePartOrder;

	private static string strPrefix = "pbase";

	private static Dictionary<int, string[]> dictEmotes;

	private int[] aEmoteCounts;

	public int nState;

	private int nStateOverride;

	private double fEpochOverride;

	private const float fOverrideDur = 2f;

	private int nBlinkState;

	private float fMinPupilDuration = 0.5f;

	public float fFaceOffsetX = -1f;

	private string[] aFaceParts;

	private CondOwner coLast;

	private static List<string> aPartNameDefaults;
}
