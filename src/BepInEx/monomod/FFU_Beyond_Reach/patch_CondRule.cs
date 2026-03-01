using System;
// Extends vanilla CondRule parsing with FFU_BR loader helpers.
// Likely: this allows condrule definitions to participate in the same partial
// overwrite and reference-based data layering used by other patched DTOs.
public class patch_CondRule : CondRule
{
	public string strReference { get; set; }
}
