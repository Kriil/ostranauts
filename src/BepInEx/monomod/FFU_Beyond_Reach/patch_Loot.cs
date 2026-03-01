using System;
// FFU_BR hook into loot resolution and loot-table parsing.
// This patch is the likely home for dynamic random-range handling and other
// data-layer tweaks that make oversized weighted tables usable.
public class patch_Loot : Loot
{
	public string strReference { get; set; }
}
