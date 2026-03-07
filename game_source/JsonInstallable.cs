using System;

// Install/build recipe definition.
// Likely loaded from `data/installables` and used to generate the install/construct
// interaction, job, required inputs, and target condition rules for placed items.
public class JsonInstallable
{
	// `strName` is the internal installable id.
	public string strName { get; set; }

	public string strActionCO { get; set; }

	public string strActionGroup { get; set; }

	public string strInteractionTemplate { get; set; }

	// Build and job types likely map to job board categories or install UI grouping.
	public string strStartInstall { get; set; }

	public string strBuildType { get; set; }

	public string strJobType { get; set; }

	public string strInteractionName { get; set; }

	public string CTUs { get; set; }

	// Condition trigger fields likely reference `data/condtrigs` ids for actor/target gating.
	public string CTAllowUs { get; set; }

	public string CTThem { get; set; }

	public string strCTThemMultCondUs { get; set; }

	public string strCTThemMultCondTools { get; set; }

	public string[] aInputs { get; set; }

	public string[] aToolCTsUse { get; set; }

	// Flags and payload arrays define produced CondOwners, inverse variants, and tool requirements.
	public string[] aLootCOs { get; set; }

	public string[] aInverse { get; set; }

	public bool bHeadless { get; set; }

	public bool bNoJobMenu { get; set; }

	public bool bNoDestructable { get; set; }

	public float fDuration { get; set; }

	public float fTargetPointRange { get; set; }

	// Creates a detached copy used when generating per-install runtime variants.
	public JsonInstallable Clone()
	{
		return new JsonInstallable
		{
			strName = this.strName,
			strActionCO = this.strActionCO,
			strActionGroup = this.strActionGroup,
			strInteractionTemplate = this.strInteractionTemplate,
			strStartInstall = this.strStartInstall,
			strBuildType = this.strBuildType,
			strJobType = this.strJobType,
			strInteractionName = this.strInteractionName,
			CTUs = this.CTUs,
			CTAllowUs = this.CTAllowUs,
			CTThem = this.CTThem,
			strAllowLootCTsThem = this.strAllowLootCTsThem,
			strAllowLootCTsUs = this.strAllowLootCTsUs,
			strCTThemMultCondUs = this.strCTThemMultCondUs,
			strCTThemMultCondTools = this.strCTThemMultCondTools,
			strProgressStat = this.strProgressStat,
			strFinishInteraction = this.strFinishInteraction,
			strLootOut = this.strLootOut,
			aInputs = this.aInputs,
			aToolCTsUse = this.aToolCTsUse,
			aLootCOs = this.aLootCOs,
			aInverse = this.aInverse,
			bHeadless = this.bHeadless,
			bNoJobMenu = this.bNoJobMenu,
			bNoDestructable = this.bNoDestructable,
			fDuration = this.fDuration,
			fTargetPointRange = this.fTargetPointRange,
			strPersistentCO = this.strPersistentCO
		};
	}

	// Keeps logs/debug strings readable by returning the internal id.
	public override string ToString()
	{
		return this.strName;
	}

	public string strAllowLootCTsThem;

	public string strAllowLootCTsUs;

	public string strProgressStat;

	public string strFinishInteraction;

	public string strLootOut;

	public string strPersistentCO;
}
