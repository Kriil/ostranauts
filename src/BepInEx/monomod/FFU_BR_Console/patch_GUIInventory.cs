using System;
public class patch_GUIInventory : GUIInventory
{
	public extern void orig_Reset(GUIInventoryWindow window = null);
	public void Reset(GUIInventoryWindow window = null)
	{
		patch_ConsoleResolver.bInvokedInventory = false;
		this.orig_Reset(window);
	}
}
