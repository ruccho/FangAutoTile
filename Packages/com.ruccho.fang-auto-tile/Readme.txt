----------------------------------------------------------------
Fang Auto Tile
©2023 ruccho
@ruccho_vector on X
Version 3.0.0
----------------------------------------------------------------

For more information and usage, see the video. (https://youtu.be/vck-rSThycA)
The wiki is also available. (https://github.com/ruccho/FangAutoTile/wiki)(Japanese)

What is Fang Auto Tile? ----------------------------------------

Fang Auto Tile provides easier way to use auto tiles in Unity Tilemap system.

Generally, auto tiles are tiles which change their own appearances depending on whether the neighboring tile is the same tile. Most implementions of auto tiles require so many kinds of sprite assets of tiles, but Fang Auto Tile requires only 5. If you create assets in compliance with the given format, it automatically creates all the patterns.
The material format is a superset of one in WOLF RPG Editor (https://www.silversecond.com/WolfRPGEditor/), which is a popular game development software especially in Japan.

Features -------------------------------------------------------
 - Animation
 - Random
 - Padding to avoid dirty lines between tiles
 - Sprite Packer (packs multiple generated tile sprites into a single texture)
 - Custom map overlapping (ex. normal map, emission map)
 - Slopes