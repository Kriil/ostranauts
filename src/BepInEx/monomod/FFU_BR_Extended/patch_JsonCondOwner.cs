using System;
// FFU_BR_Extended adds extra runtime/data parameters to the `JsonCondOwner` contract.
// These fields back the new wiki-documented parameters such as inventory effects,
// slot ordering, room lookup, and same-ship targeting helpers.
public class patch_JsonCondOwner : JsonCondOwner
{
	public string strInvSlotEffect { get; set; }
}
