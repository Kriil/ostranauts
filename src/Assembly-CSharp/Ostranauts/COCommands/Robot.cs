using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ostranauts.COCommands
{
	public class Robot : Crew
	{
		protected override void Awake()
		{
			this.tf = base.transform;
			this.dictSpritesClothes = new Dictionary<string, Dictionary<int, List<JsonStringPair>>>();
			this.dictSpritesClothesMRs = new Dictionary<string, Renderer>();
			this.dictSpritesClothes["Base"] = new Dictionary<int, List<JsonStringPair>>();
			JsonStringPair jsonStringPair = new JsonStringPair();
			jsonStringPair.strName = "Crew2BaseA01";
			jsonStringPair.strValue = "blank";
			this.dictSpritesClothes["Base"][0] = new List<JsonStringPair>
			{
				jsonStringPair
			};
			this.dictSpritesClothes["EVA"] = new Dictionary<int, List<JsonStringPair>>();
			this.dictSpritesClothes["EVAHelmet"] = new Dictionary<int, List<JsonStringPair>>();
			this.dictSpritesClothes["EVABackpack"] = new Dictionary<int, List<JsonStringPair>>();
			this.dictSpritesClothes["Outfit01"] = new Dictionary<int, List<JsonStringPair>>();
			Transform transform = this.tf.Find("Base");
			MeshRenderer[] componentsInChildren = transform.GetComponentsInChildren<MeshRenderer>(true);
			int num = 0;
			foreach (MeshRenderer meshRenderer in componentsInChildren)
			{
				this.dictSpritesClothesMRs.Add(num + "_" + meshRenderer.name, meshRenderer);
				num++;
			}
			base.FaceParts = FaceAnim2.GetRandomFace(true, true, null);
		}
	}
}
