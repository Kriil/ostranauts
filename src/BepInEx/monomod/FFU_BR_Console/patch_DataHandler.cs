using System;
using MonoMod;
[MonoModIgnore]
// Lightweight bridge into the FFU_BR loader API from the console module.
// This stub exposes core loader helpers without replacing DataHandler again,
// so console commands can query patched CO templates safely.
public class patch_DataHandler
{
	public static bool TryGetCOValue(string strName, out JsonCondOwner refCO)
	{
		refCO = null;
		return false;
	}
}
