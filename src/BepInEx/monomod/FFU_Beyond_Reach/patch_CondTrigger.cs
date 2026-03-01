using System;
// Extends condition-trigger evaluation for FFU_BR-specific JSON parameters.
// Likely: this is where extra trigger tests like math ops, depth limits, or
// same-ship lookups are interpreted at runtime.
public class patch_CondTrigger : CondTrigger
{
	public string strReference { get; set; }
}
