using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIFeatureVote : GUIData
{
	protected override void Awake()
	{
		base.Awake();
		this.txtDesc = base.transform.Find("txtDesc").GetComponent<TMP_Text>();
		this.txtDesc.text = string.Empty;
		this.cgVoted = base.transform.Find("txtVoted").GetComponent<CanvasGroup>();
		this.cgVoted.alpha = 0f;
		this.SetupBtn(base.transform.Find("pnl01").GetComponent<Button>(), "FEATURE_VOTE_01", "Change items and ship parts in Ostranauts to wear out with normal use.\n\nOver time, you will be required to repair or replace personal items or pieces of ship equipment.");
		this.SetupBtn(base.transform.Find("pnl02").GetComponent<Button>(), "FEATURE_VOTE_02", "Add zero-g environments to Ostranauts.\n\nFloat down corridors, reach places where there is no floor, and risk getting stranded.");
		this.SetupBtn(base.transform.Find("pnl03").GetComponent<Button>(), "FEATURE_VOTE_03", "Improve crew AI and management in Ostranauts.\n\nAdd the ability to order crew to dock for you, start and stop reactors, and other useful actions.");
		this.SetupBtn(base.transform.Find("pnl04").GetComponent<Button>(), "FEATURE_VOTE_04", "Add new ship equipment to Ostranauts.\n\nAdd functional and decorative items for use on ships, like air scrubbers, crash couches, and decor.");
		this.SetupBtn(base.transform.Find("pnl05").GetComponent<Button>(), "FEATURE_VOTE_05", "Add injuries and wounds to Ostranauts, and the means to treat them.\n\nCuts, bruises, burns, medical tools and drugs, and the UI to manage it all.");
		this.SetupBtn(base.transform.Find("pnl06").GetComponent<Button>(), "FEATURE_VOTE_06", "Add another port to Ostranauts.\n\nAdd a long range destination to reach via fusion torch, and the means to accelerate time during the long voyage.");
	}

	private void Update()
	{
		if (this.cgVoted.alpha > 0f)
		{
			this.cgVoted.alpha -= 0.01f;
		}
	}

	private void SetUI()
	{
	}

	private void SetupBtn(Button btn, string strVote, string strDesc)
	{
		btn.onClick.AddListener(delegate()
		{
			this.Vote(strVote);
		});
		btn.GetComponent<GUIEnterExitHandler>().fnOnEnter = delegate()
		{
			this.Rollover(strDesc);
		};
		btn.GetComponent<GUIEnterExitHandler>().fnOnExit = delegate()
		{
			this.txtDesc.text = string.Empty;
		};
	}

	private void Vote(string strVote)
	{
		this.cgVoted.alpha = 1f;
	}

	private void Rollover(string strDesc)
	{
		this.txtDesc.text = strDesc;
	}

	private void Quit()
	{
		CrewSim.LowerUI(false);
	}

	public override void Init(CondOwner coSelf, Dictionary<string, string> mapGPMData, string strGPMKey)
	{
		base.Init(coSelf, mapGPMData, strGPMKey);
		this.SetUI();
	}

	private TMP_Text txtDesc;

	private CanvasGroup cgVoted;

	private const string VOTING_ROUND = "GUIFeatureVote";

	private const string FEATURE01 = "Change items and ship parts in Ostranauts to wear out with normal use.\n\nOver time, you will be required to repair or replace personal items or pieces of ship equipment.";

	private const string FEATURE02 = "Add zero-g environments to Ostranauts.\n\nFloat down corridors, reach places where there is no floor, and risk getting stranded.";

	private const string FEATURE03 = "Improve crew AI and management in Ostranauts.\n\nAdd the ability to order crew to dock for you, start and stop reactors, and other useful actions.";

	private const string FEATURE04 = "Add new ship equipment to Ostranauts.\n\nAdd functional and decorative items for use on ships, like air scrubbers, crash couches, and decor.";

	private const string FEATURE05 = "Add injuries and wounds to Ostranauts, and the means to treat them.\n\nCuts, bruises, burns, medical tools and drugs, and the UI to manage it all.";

	private const string FEATURE06 = "Add another port to Ostranauts.\n\nAdd a long range destination to reach via fusion torch, and the means to accelerate time during the long voyage.";
}
