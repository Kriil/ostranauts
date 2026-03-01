This is an uncompiled BepInEx plugin source scaffold for Ostranauts.

What it does:

* Adds a Harmony transpiler for Interaction.CalcRate().  
* Targets only the clamp applied to fCTThemModifierUs.  
* Replaces the original Mathf.Clamp(value, min, max) call with a hook that can inspect and modify the clamp inputs before calling the real Mathf.Clamp.

Why this approach:

* It avoids replacing the whole CalcRate()method.  
* It is more compatible with mods like FFU\_BR, because it only patches the specific clamp block instead of copying the entire method body.

Current example behavior:

* If the interaction is in the "Work" action group, the hook raises the clamp max to at least 25f.

Notes:

* These are source files only. They will not load in-game until you compile them into a DLL.  
* After compiling, place the compiled DLL in the real game's BepInEx/plugins folder.  
* The plugin expects references to BepInEx, 0Harmony, the game's Assembly-CSharp, and UnityEngine assemblies when you build it.

The multiplier is not hardcoded per action type. Install, uninstall, repair, and the game’s “restore/undamage” style jobs all use the same Work interaction pipeline, and the actual speed comes from data plus one shared calculation.

Files that affect it

The main code path is:

* [Interaction.cs (line 3725\)]() computes the work-rate multiplier in CalcRate().  
* [Interaction.cs (line 2056\)]() applies it in ApplyEffects().  
* [Installables.cs (line 110\)]() copies installable data into the generated runtime interaction (fDuration, strCTThemMultCondUs, strCTThemMultCondTools).  
* [CondTrigger.cs (line 858\)]() is where the scaled amount is finally added to a condition.  
* [CondOwner.cs (line 8906\)]() provides default progress caps (StatInstallProgressMax, StatUninstallProgressMax, StatRepairProgressMax) if an item does not define its own.

The data files that drive the result are:

* data/installables/\*.json (or mod equivalents): define which stat is used for actor speed and tool speed, the base job duration, and which progress stat is advanced. Example: [ins\_set\_fac\_maint\_pump.json (line 25\)](), [ins\_rep\_fac\_maint\_pump.json (line 31\)](), [ins\_fix\_fac\_maint\_pump.json (line 21\)]().  
* data/loot/\*.json: define the base progress per completed work tick. Example: [lot\_generic\_skill\_uninstall.json (line 3\)]() uses TUpUninstallProgress=1x5, so the base increment is 5\.  
* data/condowners/\*.json: define the total required progress (Stat\*ProgressMax) for the target object. Example: [cos\_fac\_maint\_pump.json (line 256\)]().

If FFU Beyond Reach Super is installed, it overrides the same calculation here:

* [patch\_Interaction.cs (line 10\)]()  
* It can multiply “super characters” again and raise the normal 10x cap via [FFU\_BR\_Defs.cs (line 93\)]().

How it is calculated

In vanilla, for any Work interaction:

* usMult \= clamp(objUs.GetCondAmount(strCTThemMultCondUs), 1, 10\) if that field exists, otherwise 1. See [Interaction.cs (line 3731\)](), [Interaction.cs (line 3734\)](), [Interaction.cs (line 3736\)]().  
* toolMult \= sum(tool.GetCondAmount(strCTThemMultCondTools)) if that field exists; otherwise 1. If a tool multiplier field is specified, it starts at 0 and sums all used tools. See [Interaction.cs (line 3737\)](), [Interaction.cs (line 3747\)]().  
* penalty \= min(objUs.GetCondAmount("StatWorkSpeedPenalty"), 0.99). See [Interaction.cs (line 3752\)]().  
* Final coefficient:  
  rate \= usMult \* toolMult \* (1 \- penalty)

That coefficient does not directly shorten fDuration. Instead, it scales the amount of progress applied when one work cycle completes.

So the practical speed is:

effective progress per cycle \= baseProgressFromLoot \* rate

and total work time is approximately:

total time \= fDuration \* (requiredProgress / effective progress per cycle)

For example, if the loot adds 5 uninstall progress per cycle and rate \= 3, one completed cycle adds 15 uninstall progress.

Where and when it is applied

It is applied when the work interaction finishes a cycle, not when the job is first created:

* Installables.Create() bakes the installable’s settings into the generated ...Allow interaction and also wires the target object to complete when its progress stat reaches Stat...ProgressMax. See [Installables.cs (line 189\)](), [Installables.cs (line 192\)]().  
* Then Interaction.ApplyEffects() calls CalcRate() and passes the computed coefficient into ApplyLootCT() / ApplyLootConds() for objThem. See [Interaction.cs (line 2129\)](), [Interaction.cs (line 2143\)]().  
* CondTrigger.ApplyChanceID() and AddCondAmount() multiply the base condition delta by that coefficient. See [CondTrigger.cs (line 870\)]().

So the short version is: the multiplier is defined by installable data, computed in Interaction.CalcRate(), and applied at each completed work tick by scaling the progress/damage-change loot applied to the target.

If you want, I can trace one specific vanilla job definition end-to-end and reduce it to a single exact formula for that item.

