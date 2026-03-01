using System;
// FFU_BR extends the base `JsonInteractionOverride` JSON payload with extra modding fields.
// These patched DTOs are consumed during synchronized load, partial overwrite,
// reference-copy creation, and dynamic save/template migration.
public class patch_JsonInteractionOverride : JsonInteractionOverride
{
	public string strReference { get; set; }
}
