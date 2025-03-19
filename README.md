# ScienceBird Tweaks

Just some small changes my friends and I wanted that I couldn't find elsewhere (though some have since popped up as separate mods by others, I'm grouping the ones I want here for convenience and simplicity).

I reccommend all players have this mod installed (and ideally the same/very similar config), but if you want to try using it client-side, **use the Client-side Mode config option**.

In the list below, I'll mention which tweaks (to my knowledge) should work client-side. My current testing setup isn't great for testing de-synced modpacks, so if any of this is wrong or if anything breaks client-side, please let me know.

---

*The default values of these tweaks are **intended to not interfere with gameplay in an undesired way**.*

*This should minimize upfront annoyance, but I recommend you consider which ones you want to enable.*

## SHIP TWEAKS
---

## Fixed Ship Objects
>*Default: ON - Client-side*

Do you relate to being unable to press the teleport button while launching or landing the ship? Does it seem like its hitbox drifts away from where the button looks like it is? Have you ever stood on a piece of ship furniture (like the welcome mat) and jittered all over the place? Well, your struggles end now.

All this tweak does is ensure all ship furniture/unlockables will be parented to the main ship object, and thus all of their colliders will stay in sync with the ship.

- **Only Fix Vanilla Objects**: This is a setting to avoid any unwanted errors with attempting (or failing) to fix modded furniture items.
>*Default: ON*

- **Alternate Fix Logic (EXPERIMENTAL)**: If you end up having any unexpected issues with furniture while using this tweak, you can try this. It's not super thoroughly tested, but it should simplify the code a bit to reduce possible points of failure or incompatibilities with other mods.
>*Default: OFF*

![FixedShipObjects](https://imgur.com/zgVE4My.png)

## Fixed Suit Rack
>*Default: ON - Client-side*

Same as Fixed Ship Objects, but for suits: properly parents them so they are selectable easily on takeoff and landing without jitter.

![FixedSuits](https://imgur.com/D0Y5lFF.png)

## Consistent Catwalk Collision
>*Default: ON - Client-side*

If you've ever desperately clambered onto the edge of the ship's railing only to slide right off since you didn't catch the right part of the collision, this change is for you!

![RailingCollisionDemonstration](https://imgur.com/d9K9jAR.png)

This just slightly extends the floor collision of the ship catwalk so you can consistently land on it from the other side of the railing, allowing you to then gracefully vault over it at your leisure.

## Tiny Teleporter Collision
>*Default: ON - Client-side*

Shrinks the placement hitbox of both teleporters so they can be more easily placed close to walls or in small spaces (and require less hassle with finnicky rotation).

The exact size of the hitbox is also adjustable in one of the config sections.

Note that this also makes the build selection hitbox smaller, so if you're getting annoyed by being unable to move the teleporter easily, try increasing the hitbox size in config.

## Large Lever Collision
>*Default: OFF - Client-side*

Enlarges the start lever's hitbox so it can be pulled more easily at a moment's notice.

The exact size of the hitbox is also adjustable in one of the config sections.

## Begone Bottom Collision
>*Default: OFF - Client-side*

Removes the colliders from bottom components of the ship (e.g. thrusters and structural supports), making it easier to access if that is desired. This is still moon dependent, and the underside of the ship still cannot be accessed on the Company moon, for example.

## Ship Item Removals
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

## GENERAL TWEAKS
---

## Big Screw
>*Default: ON - Client-side*

Changes the name of the "big bolt" to reflect what it actually is (a big screw).

## Falling Rotation Fix
>*Default: OFF - Client-side*

Normally, if you drop an object from high up, it will rotate much slower than the rate it falls. This means the object will hit the ground while its rotation is still being updated (and the game will still consider it in a "falling" state).

The only immediate consequence of this bug is the visual effect of objects strangely spinning on the ground when you drop them from high up. This change may also end up making them set off mines on contact in more cases (rather than setting them off *only* after their rotation is finished).

In any case, this tweak scales this rotation so it will finish while the object is still in the air and its falling state will end normally.

*The default value of this tweak has been changed to OFF after some reports of issues (the fix is so small that it's really not worth risking). However, its code has remained unchanged for a long time and I've never encountered any issues nor recieved any other reports, making it likely a mod incompatibility. So, it should be safe to enable this in most cases, but if you do end up having issues please send the details to me.*

## Old Halloween Elevator Music
>*Default: OFF - Client-side*

![ElevatorMusicLogo](https://imgur.com/8iIZjuE.png)

Reverts the behaviour of the mineshaft elevator to its behaviour from the 2024 Halloween patch (v65 to v68), playing a random clip of groovy music by [ZedFox](https://zedfox.carrd.co/). The track is synced, so players using the same mod will hear the same elevator music track.

ButteryStancakes has a more [extensively customizeable version of this feature](https://thunderstore.io/c/lethal-company/p/ButteryStancakes/HalloweenElevator/), and if that mod is detected this tweak automatically disables to let it take priority.

## BETTER DUST CLOUDS
---

A few tweaks to make the normally unused dust clouds into a more interesting and gameplay-relevant weather. These consist of:

- **Dust (Space) Clouds**: Adds a space to `DustClouds` weather condition when it's displayed on the main level screen or terminal monitor, making it `Dust Clouds`.
>*Default: ON - Client-side*

- **Thick Dust Clouds**: Increases the thickness of the dust clouds (exact value configurable).
>*Default: OFF - All clients*

- **Dust Clouds Noise**: Adds windy sound effects to the dust clouds (same as you hear on blizzard moons like Rend and Dine).
>*Default: OFF - All clients*

![DustCloudsComparison](https://imgur.com/xcQ6d4k.png)

(Before and after)

Since Dust Clouds is an unused weather in vanilla, you'll only see this weather if it's explicitly added on a modded moon, or you use a mod like [Dusted](https://thunderstore.io/c/lethal-company/p/ZetaArcade/Dusted/) to add them into the normal weather rotation (which I recommend if you're interested in these changes).

## MOD TWEAKS
---

*These tweaks **do not require the relevant mods** as dependencies, and if they are enabled without those mods, nothing will happen.*

## Various Mod Patches - JLL/LLL/Wesley's Moons

Some quick patches I put in for issues and inconveniences I've encountered:

- **JLL Noisemaker Fix**: Fixes occasional noisemaking item malfunctions (e.g. Wesley audio logs not playing) by initializing the RNG functions of JLL objects when a moon loads to avoid an occasional bug with null RNG functions.
>*Default: ON - Client-side*

- **LLL Unlock Syncing**: Manually applies the host's unlocked moons to all clients, so any moons the host has, the clients will have too (addressing an issue where unlocks in Wesley's Moons could become desynced).
>*Default: OFF - All clients*

- **Wesley's Moons Tape Insert Fix (EXPERIMENTAL)**: A quick patch which attempts to fix a problem where clients would be unable to interact with the casette tape loader or story log machine. I only did fairly light testing with 2 players, and this involves a lot of messing around with player IDs, so I can't be certain how this will operate in varied multiplayer circumstances. 
>*Default: OFF - All clients*

## Video Tape Skip - Wesley's Moons
>*Default: OFF - All clients*

If you like the tapes in Wesley's Journey™, but don't want to sit through the video every time, try this new and improved skippable cassette loader!

While playing, the projector will give you a prompt to skip the currently playing tape to the end and unlock the moon immediately.

![VideoTapeSkip](https://imgur.com/qjEYVwL.png)

Note this may have some unintended side-effects when used with tapes with special effects like the cursed tape.

## True Blackout - Mrov Weathers (EXPERIMENTAL)
>*Default: OFF - Client-side (?)*

Along with disabling all the lights like the Blackout weather condition usually does, this will also darken the emissivity of associated light textures. What this means in practice is lights will not look pure white or with a texture reflecting an "on" state. Instead, they will look dark/dim as if they have been turned off.

![BlackoutComparison](https://imgur.com/v2P98AD.png)

Technically this makes the Blackout condition slightly harder to navigate, but this really only exists to make moons more visually appealing when this weather is active.

This tweak should be stable overall, but there will be a significant lag spike when the blackout first loads. Performance will be unaffected afterwards.

## Terminal Stock - SelfSortingStorage/TerminalFomatter
>*Default: ON - All clients*

A little patch for the niche interaction between SelfSortingStorage's Smart Cupboard and mrov's TerminalFormatter: items in the cupboard will now count towards the "owned" column in the terminal's store menu.

![TerminalStockDemo](https://imgur.com/26AId6p.png)

Try scrolling around or re-entering the store menu if you have any issues with the item counts not being immediately updated.

## Diversity Computer Begone - Diversity
>*Default: OFF - Client-side*

Removes the floppy reader computer and floppy disk spawns from Diversity if you don't want those features of the mod (this shouldn't cause any compatibility issues even if you don't have Diversity installed).

This is also compatible (though redundant) with [v0xx's more thorough patch DiversityNoFloppy](https://thunderstore.io/c/lethal-company/p/v0xx/DiversityNoFloppy/).

## GAMEPLAY TWEAKS
---

## Snare Flea Forgiveness Options
>*Default: Vanilla - All clients*

Provides some alternatives to the vanilla behaviour of the snare flea/centipede:

- **Second chance:** provides players with a second chance, as the snare flea will leave them alone one time after bringing them to low health (default/vanilla is 15 HP).
    - This is equivalent to the singleplayer mechanic, just implemented in multiplayer (any config changes to this mechanic will also affect the singleplayer mechanic as well).

- **Fixed damage:** The snare flea will do a fixed portion (default 50%) of a player's maximum health before dropping off them (when set at 50%, players will always be killed on their second encounter if their health is above half, and killed on their first encounter if it's below half).
    - To account for Lethal Company's built-in "extra life" when a player reaches critical health (saving them from lethal damage once), a snare flea will always damage a player when they are at critical HP.

These come with config options like the low health threshold for second chance and the health % for fixed damage.

---

## Contact

Let me know about any suggestions or issues on the [GitHub](https://github.com/Science-Bird/ScienceBirdTweaks), or if you'd prefer you can mention it in my [Discord thread](https://discord.com/channels/1168655651455639582/1350616165289951272) (I'm "sciencebird" on Discord).
