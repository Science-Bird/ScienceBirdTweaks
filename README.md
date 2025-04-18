# ScienceBird Tweaks

Just some ~~small~~ changes my friends and I wanted that I couldn't find elsewhere (though some have since popped up as separate mods by others, I'm grouping the ones I want here for convenience and simplicity).

I recommend all players have this mod installed (and ideally the same/very similar config), but if you want to try using it client-side, **use the Client-side Mode config option**.

In the list below, I'll mention which tweaks (to my knowledge) should work client-side. My current testing setup isn't great for testing de-synced modpacks, so if any of this is wrong or if anything breaks client-side, please let me know.

---

*The default values of these tweaks are **intended to not interfere with gameplay in an undesired way**.*

*This should minimize upfront annoyance, but I recommend you consider which ones you want to enable.*

## SHIP TWEAKS

### Fixed Ship Objects

<details>
<summary>Details</summary>
<br />

>*Default: ON - Client-side*

Do you relate to being unable to press the teleport button while launching or landing the ship? Does it seem like its hitbox drifts away from where the button looks like it is? Have you ever stood on a piece of ship furniture (like the welcome mat) and jittered all over the place? Well, your struggles end now.

All this tweak does is ensure all ship furniture/unlockables will be parented to the main ship object, and thus all of their colliders will stay in sync with the ship.

- **Only Fix Vanilla Objects**: This is a setting to avoid any unwanted errors with attempting (or failing) to fix modded furniture items.
>*Default: ON*

- **Alternate Fix Logic (EXPERIMENTAL)**: If you end up having any unexpected issues with furniture while using this tweak, you can try this. It's not super thoroughly tested, but it should simplify the code a bit to reduce possible points of failure or incompatibilities with other mods.
>*Default: OFF*

![FixedShipObjects](https://imgur.com/zgVE4My.png)

</details>

### Fixed Suit Rack

<details>
<summary>Details</summary>
<br />

>*Default: ON - Client-side*

Same as Fixed Ship Objects, but for suits: properly parents them so they are selectable easily on takeoff and landing without jitter.

![FixedSuits](https://imgur.com/D0Y5lFF.png)

</details>

### Rotating Floodlight

<details>
<summary>Details</summary>
<br />

>*Default: OFF - All clients*

Ever wanted the ship to be a bit more dynamic? Well, the big spotlights spin now.

![FloodlightSpinDemonstration](https://imgur.com/qYCJYxP.gif)

This is toggled by a button next to the ship's lever, and works especially well with "blackout" effects (see associated section) or generally dark moons, making the ship easier to spot and providing dynamic lighting to the surroundings. The exact rotation speed, brightness, etc. are all configurable.

Other configurable modes:

- **Rotate On Landing**: The floodlight will start rotating automatically once the ship lands.
>*Default: OFF - All clients*

- **Follow Players (EXPERIMENTAL)**: The floodlight will rotate to face the closest player outside the ship (this is a bit weird currently since the two lights point outwards, meaning the player facing them will actually be between the lights).
>*Default: OFF - All clients*

At some point this might be turned into a purchasable upgrade, but for now it's just something you can enable.

*`Credit for this tweak goes to xameryn`*

</details>

### Consistent Catwalk Collision

<details>
<summary>Details</summary>
<br />

>*Default: ON - Client-side*

If you've ever desperately clambered onto the edge of the ship's railing only to slide right off since you didn't catch the right part of the collision, this change is for you!

![RailingCollisionDemonstration](https://imgur.com/d9K9jAR.png)

This just slightly extends the floor collision of the ship catwalk so you can consistently land on it from the other side of the railing, allowing you to then gracefully vault over it at your leisure.

</details>

### Tiny Teleporter Collision

<details>
<summary>Details</summary>
<br />

>*Default: ON - Client-side*

Shrinks the placement hitbox of both teleporters so they can be more easily placed close to walls or in small spaces (and require less hassle with finnicky rotation).

The exact size of the hitbox is also adjustable in one of the config sections.

Note that this also makes the build selection hitbox smaller, so if you're getting annoyed by being unable to move the teleporter easily, try increasing the hitbox size in config.

</details>

### Large Lever Collision

<details>
<summary>Details</summary>
<br />

>*Default: OFF - Client-side*

Enlarges the start lever's hitbox so it can be pulled more easily at a moment's notice.

The exact size of the hitbox is also adjustable in one of the config sections.

</details>

### Begone Bottom Collision

<details>
<summary>Details</summary>
<br />

>*Default: OFF - Client-side*

Removes the colliders from bottom components of the ship (e.g. thrusters and structural supports), making it easier to access if that is desired. This is still moon dependent, and the underside of the ship still cannot be accessed on the Company moon, for example.

</details>

### Ship Item Removals

<details>
<summary>Details</summary>
<br />

>*Default: OFF - Client-side (except maybe interactables like clipboard and sticky note?)*

Under "Ship Tweaks Removals" in config, you will find a list of items you can remove from the ship according to your aesthetic preferences:
- Service manual clipboard
- "SIGURD" sticky note
- Teleporter button cord
- Long tube (the one connected to the generator which is strewn across the floor)
- Generator (next to the door)
- Helmet (on the counter by the monitors and teleport buttons)
- Oxygen tanks (leaning against the wall)
- Boots (by the suit rack)
- Air filter (in the corner by the monitors)
- Batteries (on the counter by the monitors)

</details>

## GENERAL TWEAKS

### Big Screw

<details>
<summary>Details</summary>
<br />

>*Default: ON - Client-side*

Changes the name of the "big bolt" to reflect what it actually is (a big screw).

</details>

### Joining Clients Item Fix

<details>
<summary>Details</summary>
<br />

>*Default: ON - Client-side*

This fixes a vanilla issue where when players join, the fact that all the items are in the ship isn't updated. For example, if a client picks up an item after joining, it would act as if it had been collected in the ship for the first time.

I believe this is also fixed by other mods, but it's included here for better compatibility with some of the scrap keeping tweaks (see associated section).

</details>

### Mine Explosion On Exit Fix

<details>
<summary>Details</summary>
<br />

>*Default: ON - All clients*

Fixes a vanilla issue where after stepping on a deactivated mine then leaving the interior, the mine would explode (other mods might've already gotten to this, but either way it's here for compatibility with the zap gun and hazard reworks).

*(If you're curious why, it's because mines don't actually check that you've stepped off them when they're deactivated, and mines are also set to explode when you're teleported off them. Since the exit doors count as teleports and the mine doesn't know that you've stepped off of it, it explodes)*

</details>

### Falling Rotation Fix

<details>
<summary>Details</summary>
<br />

>*Default: OFF - Client-side*

Normally, if you drop an object from high up, it will rotate much slower than the rate it falls. This means the object will hit the ground while its rotation is still being updated (and the game will still consider it in a "falling" state).

The only immediate consequence of this bug is the visual effect of objects strangely spinning on the ground when you drop them from high up. This change may also end up making them set off mines on contact in more cases (rather than setting them off *only* after their rotation is finished).

In any case, this tweak scales this rotation so it will finish while the object is still in the air and its falling state will end normally.

*The default value of this tweak has been changed to OFF after some reports of issues (the fix is so small that it's really not worth risking). However, its code has remained unchanged for a long time and I've never encountered any issues nor recieved any other reports, making it likely a mod incompatibility. So, it should be safe to enable this in most cases, but if you do end up having issues please send the details to me.*

</details>

### Old Halloween Elevator Music

<details>
<summary>Details</summary>
<br />

>*Default: OFF - Client-side*

Reverts the behaviour of the mineshaft elevator to its behaviour from the 2024 Halloween patch (v65 to v68), playing a random clip of groovy music by [ZedFox](https://zedfox.carrd.co/). The track is synced, so players using the same mod will hear the same elevator music track.

![ElevatorMusicLogo](https://imgur.com/8iIZjuE.png)

ButteryStancakes has a more [extensively customizable version of this feature](https://thunderstore.io/c/lethal-company/p/ButteryStancakes/HalloweenElevator/), and if that mod is detected this tweak automatically disables to let it take priority.

</details>

### Pause Menu Flicker Fix

<details>
<summary>Details</summary>
<br />

>*Default: OFF - Client-side*

I call this a "fix", but it's more like a janky work-around. If you've had an issue with the "Resume" button flickering when you open the pause menu, this will resolve that by making the currently selected option always highlighted. This does look a little strange in-game, but it's better than flickering at least.

The flickering isn't a vanilla issue (at least to a meaningful extent), but I'm uncertain what mod(s) cause it or how widespread it is. Either way, this is a tweak that exists.

</details>

## ZAP GUN & MAP HAZARD OVERHAUL

### Hazard Cooldown Animations

<details>
<summary>Details</summary>
<br />

>*Default: OFF - All clients*

Unlike turrets, mines and spike traps don't have any obvious animations or audio cues for when they are disabled by the terminal. This tweak changes their lights to green, and for mines their flashing speeds up significantly accompanied by a new sound effect.

This will automatically be enabled if using the zap gun rework described in the following section, since this kind of feedback is more important if you're disabling traps right in front of you rather than from the ship.

*(See Zappable Hazards section for visual demonstrations of these animations)*

</details>

### Zap Gun Tutorial Improvements

<details>
<summary>Details</summary>
<br />

I haven't thoroughly tested this or seen it documented elsewhere, but it seems like the zap gun "tutorial arrow" which guides your mouse movement isn't working as intended at all. In my experience, the tutorial tends to always appear for the host, but never other players.

<details>
<summary><i>Expand for wordy explanation</i></summary>

>*From looking at the code, it seems like the tutorial is supposed to only happen the first 2 times you use it. The issue is that the code that increments the number of uses requires the player to have used the gun for a certain amount of time before running, but this timer value seems to usually be set to zero before that check happens, thus the game never increments the tutorial counter. Another issue is that your progress in the tutorial is only saved if the tutorial is still active, meaning that even if you manage to increment the counter and finish the tutorial, the fact that it's finished means it won't save, and you'd have the tutorial again after restarting.*

</details>

<br />

I've tried my best to restore what I assume is the intended behaviour, as well as making it generally configurable so you can adjust to your liking with the following modes:

>*Default: Only First Time - All clients*

- **Only First Time**: The assumed intended behaviour: each player will individually experience the tutorial their first few times (number configurable), then never again after that.

- **Per Session**: Same as above, but instead of only happening once, it resets each session (in-case you need a reminder).

- **Always**: Simply always shows the tutorial for all players.

- **Vanilla**: Leaves it unpatched.

<br />

**Tutorial Animation Change**
>*Default: OFF - Client-side*

Another thing that bothered me about the vanilla implementation is that the arrow graphic itself is a little misleading. The mouse icon continually repeats left or rightwards swipes, but what you should really be doing is shifting then holding the mouse on one side or the other, not swiping.

So, I also threw together a dynamic behaviour for the mouse which will adjust its position relative to how far off target you are. It's still pretty janky looking, but the option is there for those interested.

</details>

### Zappable Hazards

<details>
<summary>Details</summary>
<br />

>*Default: OFF - All clients*

>*(Zap gun changes as a whole are disabled by default, but the individual tweaks have their own default values when the zap gun rework is enabled)*

Turrets, mines, spike traps, and the big airlock/pressure doors in the facility interior all become zappable by the zap gun. This behaves the same way as disabling them with the terminal, but the time a given hazard stays deactivated is dependent on how long you manage to zap it for (individual base cooldowns and scaling factor are configurable).

The big doors are a bit different: zapping a closed big door will "jam" it open for as long as you're zapping it, but close as soon as you let go. It's not too hard to let players (or other things) through this way, but you'll find it's hard to get yourself through...

*(Currently, no modded hazards are supported)*

<details>
<summary>Mine demo</summary>

![MineDemonstration](https://imgur.com/KDG2reP.gif)

</details>

<details>
<summary>Turret demo</summary>

![TurretDemonstration](https://imgur.com/sMy82g5.gif)

</details>

<details>
<summary>Spike trap demo</summary>

![SpikeTrapDemonstration](https://imgur.com/f7UDKDM.gif)

</details>

<details>
<summary>Big door demo</summary>

![BigDoorDemonstration](https://imgur.com/qYaAfBQ.gif)

</details>

<br />

A few properties of the zap gun are also configurable:
    
- Battery life of the zap gun
    
- Priority list for which types of entities should be zapped when there are multiple valid targets

**If you have any mods which re-balance the zap gun, it's recommended you disable those or adjust their config (e.g. [ButteRyBalance](https://thunderstore.io/c/lethal-company/p/ButteryStancakes/ButteRyBalance/)). Not just to avoid errors with conflicts, but because allowing traps to be zapped significantly buffs the zap gun, so additional buffs to their battery life or other things can easily make them a little unbalanced.**

</details>

### Deadly Facility Doors

<details>
<summary>Details</summary>
<br />

>*Default: ON - All clients*

>*(Only activates if zap gun rework is enabled)*

To balance the ability to jam doors open, the facility doors now kill players when closing. It's pretty tough to get through one while being slowed by the zap gun!

Also enabled by default is the ability for these doors to kill enemies. Both of these work when closing them from the terminal, too.

![DoorCrushingDemonstration](https://imgur.com/nJkVKiI.gif)

</details>

## BETTER DUST CLOUDS

### Dust (Space) Clouds

<details>
<summary>Details</summary>
<br />

>*Default: ON - Client-side*

Adds a space to the `DustClouds` weather condition when it's displayed on the main level screen or terminal monitor, making it `Dust Clouds`.

Dust clouds isn't used in vanilla, so you'll only see this if a modded moon or weather mod adds dust clouds into the rotation.

</details>

### New Visuals and Audio

<details>
<summary>Details</summary>
<br />

A few tweaks to make dust clouds into a more interesting and gameplay-relevant weather:

- **Thick Dust Clouds**: Increases the thickness of the dust clouds (exact value configurable).
>*Default: OFF - All clients*

- **Dust Clouds Noise**: Adds windy sound effects to the dust clouds (same as you hear on blizzard moons like Rend and Dine).
>*Default: OFF - All clients*

![DustCloudsComparison](https://imgur.com/xcQ6d4k.png)

(Before and after)

Since dust clouds is an unused weather in vanilla, you'll only see this weather if it's explicitly added on a modded moon, or you use a mod like [Dusted](https://thunderstore.io/c/lethal-company/p/ZetaArcade/Dusted/) to add them into the normal weather rotation (which I recommend if you're interested in these changes).

</details>

## SHOTGUN QUALITY OF LIFE

### Better Tooltips

<details>
<summary>Details</summary>
<br />

Improves the readability of shotgun tooltips:

- **Show Loaded Shells**: The number of currently loaded shells are displayed.

>*Default: OFF - Client-side*

*This is essentially a port of [AmmoIndicator by ironbean](https://thunderstore.io/c/lethal-company/p/ironbean/AmmoIndicator/) or the feature in [AtomicStudio's Better Shotgun Tooltip](https://thunderstore.io/c/lethal-company/p/AtomicStudio/Better_Shotgun_Tooltip/) (and a similar feature is added in many other mods), all credit for the idea/implementation goes to them, it's just bundled here for compatibility.*

- **Custom Safety Tooltip**: The safety tooltip is made more clear, and is fully customizable in config.

>*Default: ON - Client-side*

*The default new tooltip is also from [AtomicStudio's Better Shotgun Tooltip](https://thunderstore.io/c/lethal-company/p/AtomicStudio/Better_Shotgun_Tooltip/), but it doesn't appear to display correctly (in the modpacks I've tested), so it's also implemented here.*

- **Dynamic Tooltip Behaviour**: The reload tooltip will only appear when you are actually able to reload the shotgun (when paired with the unload shells option).

>*Default: ON - Client-side*

![ShotgunNewTooltips](https://imgur.com/n6GiLV3.png)

*(A lot of these behaviours are clustered together and not individually able to be disabled, but if you want to totally reject all these changes, there's a "master disable" option in the config)*

</details>

### Unloading Shells

<details>
<summary>Details</summary>
<br />

>*Default: OFF - All clients*

When you have no shells in your inventory or when the shotgun is full, you can hold down the reload button and it will act as an "eject shells" button, with the tooltip dynamically updating accordingly. This will drop the loaded ammunition onto the ground so you can redistribute or store shotgun shells as you please.

![EjectedShells](https://imgur.com/J44BjUC.png)

</details>

### Pick Up In Orbit

<details>
<summary>Details</summary>
<br />

- **Pick Up Shotgun In Orbit**: Allows you to pick up the shotgun while in the ship orbit phase.
>*Default: OFF - Client-side (?)*

- **Pick Up Shells In Orbit**: Allows you to pick up the shotgun shells while in the ship orbit phase (this is enabled by default to make the use of the unloading feature more smooth).
>*Default: ON - Client-side (?)*

I'm not familiar with the specifics, but I've heard that not being able to pick up certain items in orbit helps reduce weird issues with items or desyncs. This is why these are configurable for only these specific items and only shells are enabled by default. So, use at your own risk I guess.

</details>

### Keep Shotgun Upon Scrap Loss

(see Selective Scrap Keeping)

## BLACKOUT

### True Blackout - MrovWeathers

<details>
<summary>Details</summary>
<br />

>*Default: ON - All clients*

This is a total rewrite of the blackout weather from [MrovWeathers](https://thunderstore.io/c/lethal-company/p/mrov/MrovWeathers/), which shuts off all lights in the map, featuring:

- Darkened emissivity of associated light textures, meaning that lights will not look pure white or with a texture reflecting an "on" state. Instead, they will look dark/dim as if they have been turned off.
- More lights should be caught in the search (especially interior ones).
- Certain lights like those on traps or outdoor apparatuses will be excluded automatically.
- Light switches on maps like Rend, Adamance, and Artifice will have no effect
- Configurable blacklists of what kinds of lights/parent objects should be excluded from the blackout routine.
- Configurable ship floodlight properties during a blackout (brightness, angle, etc.).
- Highly optimized performance (especially compared to the previous experimental version of this tweak) which batches the lights to shut off in cycles rather than all at once.

In-fact, this feature has come such a long way that it's become hard to justify not merging it into Mrov Weathers itself, so that may happen in the future.

![BlackoutComparison](https://imgur.com/v2P98AD.png)

*(Note that despite how far it's come, this feature is still a work in progress and some textures in modded moons/interiors likely haven't been addressed by us yet)*

Technically this has some gameplay impacts by making the map overall a bit darker and thus harder to navigate, and a bit easier by preserving all hazard lights, but this is mainly meant to be an aesthetic change.

The same logic is used in the Apparatus True Blackout tweak in the following section, so if you're interested in this without wanting Mrov Weathers, you can enable that.

</details>

### Vanilla True Blackout Options

<details>
<summary>Details</summary>
<br />

Some optional adjustments to make vanilla power outages more thorough:

- **Apparatus Blackout**: When removing the apparatus, lights both inside and out will be shut off, along with any emissive textures (using the same logic as the above Mrov Weathers True Blackout tweak). This of course does not include the sun.

>*Default: OFF - All clients*

- **Apparatus Hazard Blackout**: Along with doors being opened, traps like turrets, mines, and spike traps will also be shut down with the power outage.

>*Default: OFF - All clients*

- **Breaker Hazard Blackout**: Same as above, but when the breaker is switched off (everything reactivates when the breaker is turned back on).

>*Default: OFF - All clients*

</details>

*`Credit for these tweaks goes to xameryn`*

## SELECTIVE SCRAP KEEPING

### Keep Worthless Scrap

<details>
<summary>Details</summary>
<br />

Are you a hoarder? Do you like keeping silly items in your ship even when their only worth is sentimental? Here's some special configurable behaviours relating to scrap items with zero value (e.g. if you gamble them via [LethalCasino](https://thunderstore.io/c/lethal-company/p/mrgrm7/LethalCasino/)):

- **Keep Worthless Scrap**: Worthless scrap won't be removed from the ship along with the rest of the scrap when all players die.

>*Default: ON - All clients*

- **Worthless Scrap Scan Text**: When a piece of worthless scrap is taken into the ship, it will get some custom flavour text when scanned. By default, the value of the item will simply say "Priceless". Items can be blacklisted from this feature in config.

>*Default: ON - Client-side (?)*

*(Joining Clients Item Fix should be enabled for this to work properly upon loading a save)*

![PricelessScanText](https://imgur.com/K1VjLS1.png)

*You can find this plush and many others in my [dedicated scrap mod](https://thunderstore.io/c/lethal-company/p/ScienceBird/Polished_Plushies_and_Silly_Scrap/) :)*

</details>

### Keep Custom Scrap List

<details>
<summary>Details</summary>
<br />

Some additional configuration allowing you to customize how certain scrap is kept:

- **List of Scrap to Keep**: A configurable list of specific items to keep when all players die and scrap is lost. By default, this list only includes the shotgun.

>*Default: OFF - All clients*

- **Zero Kept Scrap Value**: When an item from this list is kept, its scrap value will be set to zero.

>*Default: ON - All clients*

</details>

These should be compatible with [Zigzag's SelfSortingStorage](https://thunderstore.io/c/lethal-company/p/Zigzag/SelfSortingStorage/) (likely not any other storage mods at the moment).

*`Credit for these tweaks goes to xameryn`*

## GAMEPLAY TWEAKS

### Snare Flea Forgiveness Options

<details>
<summary>Details</summary>
<br />

>*Default: Vanilla - All clients*

Provides some alternatives to the vanilla behaviour of the snare flea/centipede:

- **Second chance:** provides players with a second chance, as the snare flea will leave them alone one time after bringing them to low health (default/vanilla is 15 HP).
    - This is equivalent to the singleplayer mechanic, just implemented in multiplayer (any config changes to this mechanic will also affect the singleplayer mechanic as well).

- **Fixed damage:** The snare flea will do a fixed portion (default 50%) of a player's maximum health before dropping off them (when set at 50%, players will always be killed on their second encounter if their health is above half, and killed on their first encounter if it's below half).
    - To account for Lethal Company's built-in "extra life" when a player reaches critical health (saving them from lethal damage once), a snare flea will always damage a player when they are at critical HP.

These come with config options like the low health threshold for second chance and the health % for fixed damage.

</details>

### Gimme That Mask

<details>
<summary>Details</summary>
<br />

>*Default: OFF - All clients*

When a mask is killed you can now grab that mask right off their face! This is very similar to the mod [TakeThatMaskOff](https://thunderstore.io/c/lethal-company/p/SillySquad/TakeThatMaskOff/), but unlike that mod, this should (hopefully) not cause any de-syncs or other bugs.

This should be compatible with most common masked changing mods like [Mirage](https://thunderstore.io/c/lethal-company/p/qwbarch/Mirage/) and the various mods fixing masked behaviour, but it does rely on there actually being a mask on the enemy. So, if you have any configuration enabled which removes the masks from masked enemies, this tweak won't do anything.

The average scrap value of these recoverable masks is also configurable (default is 25, set fairly low so mask farming doesn't become too powerful).

![GrabbableMaskDemonstration](https://imgur.com/PskfkCv.png)

</details>

## MOD TWEAKS

*These tweaks **do not require the relevant mods** as dependencies, and if they are enabled without those mods, nothing will happen.*

### Various Mod Patches - JLL/LLL/Wesley's Moons

<details>
<summary>Details</summary>
<br />

Some quick patches I put in for issues and inconveniences I've encountered:

- **JLL Noisemaker Fix**: Fixes occasional noisemaking item malfunctions (e.g. Wesley audio logs not playing) by initializing the RNG functions of JLL objects when a moon loads to avoid an occasional bug with null RNG functions.
>*Default: ON - Client-side*

- **LLL Unlock Syncing**: Manually applies the host's unlocked moons to all clients, so any moons the host has, the clients will have too (addressing an issue where unlocks in Wesley's Moons could become desynced).
>*Default: OFF - All clients*

- **Wesley's Moons Tape Insert Fix (EXPERIMENTAL)**: A quick patch which attempts to fix a problem where clients would be unable to interact with the casette tape loader or story log machine. I only did fairly light testing with 2 players, and this involves a lot of messing around with player IDs, so I can't be certain how this will operate in varied multiplayer circumstances. 
>*Default: OFF - All clients*

</details>

### Video Tape Skip - Wesley's Moons

<details>
<summary>Details</summary>
<br />

>*Default: OFF - All clients*

If you like the tapes in [Wesley's Journey™](https://thunderstore.io/c/lethal-company/p/Magic_Wesley/Wesleys_Moons/), but don't want to sit through the video every time, try this new and improved skippable cassette loader!

While playing, the projector will give you a prompt to skip the currently playing tape to the end and unlock the moon immediately.

![VideoTapeSkip](https://imgur.com/qjEYVwL.png)

Note this may have some unintended side-effects when used with tapes with special effects like the cursed tape.

</details>

### Weather Announcement Adjustment - WeatherTweaks

<details>
<summary>Details</summary>
<br />

>*Default: ON - Client-side*

Adjusts the wording of the notifications to be a bit more clear for transitioning weathers in mrov's [WeatherTweaks](https://thunderstore.io/c/lethal-company/p/mrov/WeatherTweaks/), and explicitly state the current and upcoming weather (does not work with uncertain weather).

![WeatherTweaksNewAnnouncments](https://imgur.com/hFYA116.png)

</details>

### Terminal Stock - SelfSortingStorage/TerminalFomatter

<details>
<summary>Details</summary>
<br />

>*Default: ON - All clients*

A little patch for the niche interaction between Zigzag's [SelfSortingStorage](https://thunderstore.io/c/lethal-company/p/Zigzag/SelfSortingStorage/)'s Smart Cupboard and mrov's [TerminalFormatter](https://thunderstore.io/c/lethal-company/p/mrov/TerminalFormatter/): items in the cupboard will now count towards the "owned" column in the terminal's store menu.

![TerminalStockDemo](https://imgur.com/26AId6p.png)

Try scrolling around or re-entering the store menu if you have any issues with the item counts not being immediately updated.

</details>

### Diversity Computer Begone - Diversity

<details>
<summary>Details</summary>
<br />

>*Default: OFF - Client-side*

Removes the floppy reader computer and floppy disk spawns from [Diversity](https://thunderstore.io/c/lethal-company/p/IntegrityChaos/Diversity/) if you don't want those features of the mod (this shouldn't cause any compatibility issues even if you don't have Diversity installed).

This is also compatible (though redundant) with [v0xx's more thorough patch DiversityNoFloppy](https://thunderstore.io/c/lethal-company/p/v0xx/DiversityNoFloppy/).

A major update for Diversity fixing its outstanding issues and possibly adding configuration for the floppy disk aspect is in development, so this tweak may soon be removed!

</details>

---

## Credits

**All work on this mod is done by myself and my friend xameryn.**

Thank you to all the wonderful mod creators mentioned throughout that have inspired me to learn how their mods work so I could add things myself!

Many of these tweaks were originally suggested or improved by various users in my Discord thread, thank you for your contributions as well.

## Contact

Let me know about any suggestions or issues on the [GitHub](https://github.com/Science-Bird/ScienceBirdTweaks) or the [Discord forum thread](https://discord.com/channels/1168655651455639582/1350616165289951272) (I'm "sciencebird" on Discord).

---
I'm ScienceBird, I've made some other Lethal Company mods, I also do Twitter art sometimes, and I'm part of the Minecraft modding team Rasa Novum (along with another contributor to this mod, **xameryn**). Check us out on [CurseForge](https://www.curseforge.com/members/rasanovum/projects) or [Modrinth](https://modrinth.com/user/RasaNovum/mods) if you're interested in highly polished, balanced, yet simple Minecraft mods.