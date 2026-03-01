using System;
// FFU_BR extends the base `JsonMusic` JSON payload with extra modding fields.
// These patched DTOs are consumed during synchronized load, partial overwrite,
// reference-copy creation, and dynamic save/template migration.
public class patch_JsonMusic : JsonMusic
{
	public string strReference { get; set; }
}
