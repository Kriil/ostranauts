## Mod Loading Order Instructions
1\. First in the list should be non-FFU mods (i.e. all mods that don't rely on FFU modding API).  
2\. Right after them goes **Minor Fixes Plus** mod (to ensure that nothing overwrites and disrupts it).  
3\. After it you can add all FFU-API mods (i.e. all mods that rely partially or heavily on FFU modding API).  

## Save Compatibility Patch Instructions
1\. Enable **ModName_Patch** in `loading_order.json` right after mod itself.  
2\. Start the game, load your latest save game you'll be playing later.  
3\. Once loaded, create new save game - it will be already with updated objects.  
4\. Save it as new file. Exit game. Remove **ModName_Patch** entries from load order.  
5\. Start game once again, load newly created save and enjoy.  

## Core Mods
### Minor Fixes Plus
The only crucial and required mod. Using **Fight For Universe - Beyond Reach** DLL without using minor fixes mod 
breaks some important parts of the vanilla game. In loading order it must be **before** all **FFU** mods and 
**after** all **non-FFU** mods.

## Major Mods
### EVA Suits Rework
Massive rework of all EVA suits (including some graphical changes). In addition, now all EVA suits come with 
built-in **Misc Pouch** (for license cards and PDAs). **Fatigue Penalty** is defined by how well-built and precise 
EVA suit is. **Fatigue Recovery** and **Load Threshold** are defined by how well servos and synthetic muscles are
integrated into different EVA suits. The **Closed Cycle** means that EVA suit has full internal environment 
regulation system that allows to consume provisions, remove waste, take care of hygiene and sleep without need to 
remove the EVA suit itself. So far game has implemented only two damage types, **Cut** and **Blunt**, thus EVA 
suits have only them (once more types are implemented, I will add them to EVA suits as well).

<details>
<summary>Modified EVA Suits Details (Click to View)</summary>

|EVA Suit<br>Type|Suit<br>Color|Back.<br>Cap.|Clip<br>Point|Weap.<br>Holst.|Ammo<br>Stor.|Med.<br>Pouch|Food<br>Stor.|Batt.<br>Slots|O2<br>Slots|Filter<br>Slots|Lamp<br>Slot|Closed<br>Cycle|Max<br>Durab.|Cut/Bl.<br>Resists|Fatigue<br>Penalty|Fatigue<br>Recovery|Load<br>Threshold|Warm<br>Speed|Insul.<br>Effect|Suit<br>Mass|
|:--|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
Basic|Orange|-/-|1x2|-/-|-/-|-/-|-/-|1|1|1|No|None|4/12|0.2/0.8|50%|-/-|-/-|2.5°C|75%|65.0|
Civilian|White|4x2|2x2|-/-|-/-|-/-|-/-|2|2|2|Yes|None|6/18|0.4/1.0|47.5%|+50%|+100%|6.5°C|76.5%|75.0|
Security|Black|4x4|2x2|4x1|4x2|-/-|-/-|2|2|2|Yes|None|16/24|1.4/2.0|25%|+150%|+150%|9.5°C|78.5%|85.0|
Medical|Red|4x6|3x2|-/-|-/-|4x3|4x1|3|3|3|No|Full|24/36|1.0/1.6|35%|+150%|+200%|12.0°C|79.5%|90.0|
Engineer|Yellow|4x8|4x2|-/-|-/-|-/-|4x1|4|4|4|Yes|None|32/48|1.0/1.6|40%|+100%|+500%|10.0°C|82%|120.0|
Hazard|Green|4x4|3x2|4x1|4x1|4x2|4x3|3|3|3|Yes|Full|40/40|2.0/3.0|30%|+250%|+350%|15.0°C|83%|100.0|
SpecOps|Blue|4x6|4x2|3x2|4x3|4x1|4x2|4|4|4|Yes|Full|60/60|3.6/4.8|10%|+500%|+750%|25.0°C|85%|135.0|
</details>

<details>
<summary>Modified EVA Helmets Details (Click to View)</summary>

|EVA Helm.<br>Type|Helm.<br>Color|Max<br>Durab.|Cut/Bl.<br>Resists|Int. Gas<br>Volume|Insul.<br>Effect|Helm.<br>Mass|
|:--|:-:|:-:|:-:|:-:|:-:|:-:|
|Basic|Orange|5/15|0.2/0.8|0.125m³|10%|1.0|
|Civilian|White|8/24|0.4/1.0|0.135m³|10.5%|1.5|
|Security|Black|20/30|1.4/2.0|0.155m³|11.5%|2.0|
|Medical|Red|28/42|1.0/1.6|0.175m³|12.5%|2.0|
|Engineer|Yellow|36/54|1.0/1.6|0.145m³|13%|3.5|
|Hazard|Green|50/50|2.0/3.0|0.195m³|14%|2.5|
|SpecOps|Blue|80/80|3.6/4.8|0.225m³|15%|5.0|
</details>

As of right now helmets are pretty much glorified set pieces with minimal differences, if you aren't taking into 
account resistances against cut and blunt attacks. Maybe in a future they will gain more crucial functionality and 
set compatibility features.

### Storage Rebalance
Rebalances all installable (and some portable) storage compartments and by expanding their capacity and enforcing 
specific type-based specialization. In addition, installable storages that aren't installed no longer can be used as
portable inventory (even if you're to slot it into character's carry/drag slot, it's inventory window won't open).
Implements new sprites for dollies.

<details>
<summary>Detailed Storage Changes (Click to View)</summary>

Can Store Everything:  
**Testudo "SuperHandy" Equipment Truck** - 3x3 size, 3x3 footprint, 6x6 capacity.  
**Testudo "HyperHandy" Equipment Truck** - 3x3 size, 3x3 footprint, 6x12 capacity.  
**Storage Bay (Rakow's "Silo" Series)** - 3x2 size, 6x4 footprint, 6x12 capacity.  

Can't Store Oversized Objects:  
**Personal Rack (Rakow's "Replete" Series)** - 1x1 size, 1x1 footprint, 6x1 capacity.   
**Small Rack (Rakow's "Replete" Series)** - 2x1 size, 2x1 footprint, 6x2 capacity.  
**Medium Rack (Rakow's "Replete" Series)** - 3x1 size, 3x1 footprint, 6x3 capacity.  
**Large Rack (Rakow's "Replete" Series)** - 4x1 size, 4x1 footprint, 6x4 capacity.  
**Huge Rack (Rakow's "Replete" Series)** - 2x2 size, 2x2 footprint, 6x6 capacity.  

Can't Store Oversized & Cumbersome Objects:  
**Cargo Crate (Rakow Smart Crate)** - 2x2 size, 2x2 footprint, 4x4 capacity.  
**Cargo Cellar (Rakow's Subfloor Bin)** - 1x1 size, 3x3 footprint, 4x4 capacity.  

Can Store Only Small Objects:  
**Compact Bin (Rakow's "Reserve" Series)** - 1x1 size, 1x1 footprint, 6x2 capacity.  
**Advanced Bin (Rakow's "Reserve" Series)** - 2x1 size, 2x1 footprint, 6x4 capacity.  
**Corner Bin (Rakow's "Vanilla" Series)** - 1x1 size, 1x1 footprint, 6x1 capacity.  
**Small Bin (Rakow's "Vanilla" Series)** - 1x1 size, 1x1 footprint, 6x1 capacity.  
**Medium Bin (Rakow's "Vanilla" Series)** - 2x1 size, 2x1 footprint, 6x2 capacity.  
**Large Bin (Rakow's "Vanilla" Series)** - 3x1 size, 3x1 footprint, 6x3 capacity.  

Can Store Only Specific Categories:  
**Fridge** - 2x2 size, 2x2 footprint, 6x12 capacity. Can only store food, drinks and medicine.  
**Sink** - 2x1 size, 2x1 footprint, 6x18 capacity. Can only store liquid without containers.  
**Toilet** - 2x2 size, 2x2 footprint, 6x12 capacity. Can only store organic waste (i.e. nothing).  
</details>

### Spaceship Engineering
Adds completely new spaceship modules. Some of them have completely new mechanics and some of them just advanced 
versions of existing modules. In addition, will include some changes for existing modules for sake of balance.

<details>
<summary>New Spaceship Modules (Click to View)</summary>

**Halvorson Maintenance Pump** - 5x5 size, 5x5 footprint. Spaceship module that consumes trash, scrap, parts or 
dedicated nanite packs to restores installed ship parts automatically over time. For now it don't have built-in 
priorities, but maybe in future it will: i.e. first will restore hull and only then lamps (& etc). Independent
and doesn't require operator - you only need to turn it on, keep it powered and supply with materials.  
</details>

<details>
<summary>Planned Spaceship Modules (Click to View)</summary>

**Weber Material Refinery** - 8x3 size, 8x3 footprint. Spaceship module that can breakdown everything into basic
elements and then convert them into basic materials (scrap/parts/etc) or gasses (oxygen/nitrogen/etc), if needed. 
Production of fusion materials (Helium-3, Deuterium & Cryonic Helium) also included. Guaranteed to be power hungry, 
so don't even think about running it without powered Fusion Reactor. How much it will be automatic or manual is 
still under consideration. Efficiency will depend on operator's skill (and maybe other parameters).  
**Gott Multiform Fabricator** - 9x4 size, 9x4 footprint. Spaceship module that can produce advanced materials 
(motors, screens, etc) and even other spaceship modules (floors, walls, fabricators). Initially, will also be able
to produce rations (foods and drinks), until proper hydroponics is introduced. Will be extremely power hungry,
so no running it without reactor as well. Operator's skill will affect production time and not efficiency.   
**Sulaiman Hydrogen Scoop** - 5x3 size, 5x3 footprint. Spaceship module that gathers ambient hydrogen and space
dust, whilst (traveling/moving?) in space. If ship's speed will affect gathering efficiency is under consideration.
If presence of atmosphere outside ship will limit gathering efficiency is also under consideration. Module will be
pretty much automatic - operator only will be needed to pump gathered hydrogen into tanks/canisters. Gathered 
hydrogen (that was pumped int tanks/canisters) can be converted into other materials via refinery module.  
**Multi-Battery Charger** - 3x2 size, 3x2 footprint. Can charge up to 12 or 16 batteries of any type.  
**Medium Gas Canisters** - 2x1 size, 2x1 footprint. ~4x capacity of Small Gas Canister.  
**Large Gas Canisters** - 3x1 size, 3x1 footprint. ~10x capacity of Small Gas Canister.  
**Large Ship Battery** - 3x2 size, 3x2 footprint. ~2x capacity of Medium Ship Battery.  
**Huge Ship Battery** - 4x2 size, 4x2 footprint. ~5x capacity of Medium Ship Battery.  
**Massive Ship Battery** - 6x2 size, 6x2 footprint. ~10x capacity of Medium Ship Battery.  
**Fission Ship Battery** - 4x2 size, 6x4 footprint. ~50x capacity of Medium Ship Battery. Requires coolant.  
**Fusion Ship Battery** - 6x2 size, 8x4 footprint. ~200x capacity of Medium Ship Battery. Requires coolant.  
**Advanced Cryonic Tank** - 3x5 size, 7x7 footprint. ~2x capacity of Cryonic Tank.  
**Advanced Deuterium Tank** - 3x5 size, 7x7 footprint. ~2x capacity of Deuterium Tank.  
**Advanced Helium-3 Tank** - 3x5 size, 7x7 footprint. ~4.28x capacity of Helium-3 Tank.  
</details>

<details>
<summary>Existing Modules Changes (Click to View)</summary>

Not yet implemented, work in progress...
</details>

**Under Consideration**: Hydroponics, RTGs, Eco/Performance/Bigger/Smaller Reactors.

## Minor Mods
### Expertise: Transponders
Success chance of removing transponder without damage depends on Electrical Engineering Skills of the character. 
There are 7 different skill levels for grading Electrical Engineering requirements (listed below). Integration
relies on existing condition values (`SkillEngElectronic` and `StatTrainingEngElectronic`) that are used by the 
original game, so there should be no compatibility issues and mod takes effect immediately, even on existing
transponders.

<details>
<summary>Electrical Engineer Proficiency Levels (Click to View)</summary>

**Novice Electrical Engineer** (no skill and training is below 20%) - has only 10% chance to keep transponder intact.  
**Beginner Electrical Engineer** (no skill and training is between 20% ~ 50%) - has 30% chance to keep transponder intact.  
**Skilled Electrical Engineer** (no skill and training is between 50% ~ 70%) - has 50% chance to keep transponder intact.  
**Proficient Electrical Engineer** (no skill and training is between 70% ~ 85%) - has 65% chance to keep transponder intact.  
**Seasoned Electrical Engineer** (no skill and training is between 85% ~ 95%) - has 80% chance to keep transponder intact.  
**Expert Electrical Engineer** (no skill and training is between 95% ~ 100%) - has 90% chance to keep transponder intact.  
**Master Electrical Engineer** (has skill or training above 100%) - has 100% chance to keep transponder intact.  
</details>

### Expertise: Tow Braces
Speed of securing and releasing vessels with tow braces now depends on operating skills of the user. Mod is still 
work in progress, so you can safely use **Fast Tow Braces** mod for now.

### Bigger Chargers Capacity
Increases inventory capacity of Gott, Halvorson and Weber chargers from **1x1** to **2x1**.

### Extended Licensing
Initially added new licenses with longer timespans for prolonged salvage operations. After **0.14.3.13 update** 
only reworks existing OKLG salvage licensing options with alternative timespans and option to sell used ones for 
1/10 of purchase price.

<details>
<summary>Extended Licenses Changes (Click to View)</summary>

**Daily Permit (Yellow)** - lasts 1 day, costs $5,000 and can be sold for $500 once used up.  
**Weekly Permit (Red)** - lasts 7 days, costs $32,000 and can be sold for $3,200 once used up.  
**Monthly Permit (Blue)** - lasts 30 days, costs $120,000 and can be sold for $12,000 once used up.  
**Seasonal Permit (Silver)** - lasts 3 months, costs $300,000 and can be sold for $30,000 once used up.  
**Annual Permit (Black)** - lasts 12 months, costs $1,000,000 and can be sold for $100,000 once used up.  
</details>

### Fully Automatic Air Vents
Makes current Auto Air Vents completely automatic. As long as they powered and turned on, they will automatically 
open, if pressure is stable.

### Glass Only EVA
Replaces EVA helmet texture with glass only semi-transparent texture. Opacity properties are unchanged.

### More Learnable Skills
Makes almost all skills learnable, just like Hacking skill is - and not only acquirable through adventures.

### Lighter Shadows
Reduces or completely removes shadows for some objects, to prevent ship interior illumination obstruction.

### Sharp Laser Torch
Allows to use Weber 'Lance' Laser Torch as Buffing tool as well. Maybe in future will be moved, once more tools are 
added.

### Slower Auto-Doors
Slightly increases time character requires to automatically open the door. To prevent accidental depressurization. 
Kinda helps.

### Slower Thermostats
Slows down thermostats considerably, to prevent them from breaking as often, especially in reactor rooms. Not very
relevant with the release of 'Spaceship Engineering' mod (the 'Maintenance Pump' module), because ship repairs can
be handled on full auto by the module, without wasting work hours of the spaceship crew.