# RoomEffects Mod

Adds bonus effects to rooms on the player's ships.  Unless stated, bonuses require the player to be in the room to receive them. This mod is intended to add additional incentive to create rooms on your ship besides just increasing its value.  Plus it adds a little to the immersion and story telling aspects.

## Room Effects Features

### Engineering Room
- Gives bonus to ship-wide work speeds → cause nothing beats your own workshop for finishing those projects faster.

### Wellness Room
- Bonus to fit/strength training → train harder in your own gym while avoiding the creeps.

### Recreation Room
- Increases positive bonuses for all interactions affecting social stats, while reducing negative bonuses. This includes conversations, playing arcade games, watching TV, looking at posters and switching tracks on a Jukebox → There's no place like home!

### Basic Quarters
- Moderately boosts sleep efficiency and the time it takes to receive the refreshed condition → Nothing like sleeping in your own bed.

### Luxury Quarters
- Significantly boosts sleep efficiency and the time it takes to receive the refreshed condition → Nothing like sleeping in your own bed in a decked out room.

### Bathroom
- Decreases defecate and cleansing time → It's always easier to poop at home and those hotel bathrooms never have my brand of shampoo.

### Passenger Room (Small)
- Increases relax bonus when relaxing in chairs → This ship feels super safe.

### Passenger Room (Medium)
- Moderately increases relax bonus when relaxing in chairs  → look at all those safe looking chairs!

### Towing room 
- Reduces tow brace securing duration -> We added a motorized wench. No more hand cranking! (This will overwrite effect of FFU-BR-Data\Exp_Tow_Braces)

### Galley
- Get hot meal bonus from eating Trenchers → Just like prison ramen. Yummy!
- Eating any food in the galley decreases how fast your hunger ticks up for a short time → Just thinking about eating Trenchers makes you less hungry!
- Drinking water in the galley decreases your thirst rate for a short time → Why does my water taste like Trenchers?

## Configuration

`Ostranauts\BepInEx\config\Room_Effects.cfg` can be modified to change the default bonuses.  Descriptions of the bonus effects are given in the file. 

The majority of the values are given as decimal fractions which represent the percentage bonus. e.g. 0.5 corresponds to a 50% bonus and 2 corresponds to a 200% bonus (or 3 times the base amount).  These can be set to 0 to remove the bonus altogether.  

## Planned Updates

### Bridge Room (Closed)
- Lower intimacy and family drain rate while on your ship - assumes the extra privacy of the closed bridge lets you pop in to the bridge and use the comms panel to talk to your friends and family. This update might add ability of player and/or crew to select chat with friends/family from NAV Console to actually simulate this in-game. Maybe even getting a large debuff if the conversation is interrupted - "William, GET OFF THE CONSOLE! Pirates are closing in!"

### Engineering
- Requirements for engineering room (Canister installed or Weber battery charger installed or ship battery installed or RCS Intake Installed) are too low considering how pwerful the bonus is. Maybe combine some of these or add other requirements. Add a config to turn this off and use vanilla requirements.
- Bonus ignores normal cap for installation progress. Add a config to allow the game to honor the vanilla cap.

### AI Behavior
- Crew prefer to use the appropriate room to get the bonuses. They'll happen upon the bonuses automatically of course but manual interaction is currently required to maximise bonuses. 

### Add New Rooms

#### Ready Room
- Tool battery charge bonus -> "Captain, I've diverted all power to ... batteries?"
- Having a chair installed allows sitting here to reduce fatigue faster -> killing all those meat walls just sucks the life out of you.

#### Medical Bay
- Increases medical bed healing rate - that meat wasn't supposed to move!
