## Overview

This will be a Mobile first RTS mainly inspired by the two games Beyond All Reason and Clash Royal.
All units will automatically move towards enemy units and the enemy base. No Unit micro is required.
Unit producing Buildings continually try to produce units, as long as the production is not stopped.
For the MVP, only a 1v1 gamemode will be implemented, at first against NPCs but with a planned multiplayer mode aswell.
The wincondition is destroying the enemies Nexus/HQ/Guildhouse.

There are two ressources. Something like Metal/Food/Crystals that will be needed to construct buildings and produce units. This is stored globaly.
The other ressource is something like Power/Energy/Mana that will be consumed to speed up building/production times. All buildings provide a small amount of energy themselves when building or producing but can be added to by nearby beacons/Power Poles/Tesla towers. Each unit and building requires a certain amount of both ressources.

As a lot of units can be visible at once and the camera is fixed, the game will be implemented as 2d to save ressources (performance/dev time)

The artstyle is not yet set in stone.
