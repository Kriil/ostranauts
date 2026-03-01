using System;
// FFU_BR extends the base `JsonTip` JSON payload with extra modding fields.
// These patched DTOs are consumed during synchronized load, partial overwrite,
// reference-copy creation, and dynamic save/template migration.
public class patch_JsonTip : JsonTip
{
	public string strReference { get; set; }
}
