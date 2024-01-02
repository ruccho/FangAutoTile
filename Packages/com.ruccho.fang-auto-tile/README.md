# Fang Auto Tile

[日本語版](./README.ja.md)

Fang Auto Tile is an easy and fully featured auto-tiling system for Unity.

![2024-01-01 16-44-32_re mkv](https://github.com/ruccho/FangAutoTile/assets/16096562/1433007a-02fb-4d4e-a650-29503fb5e24b)

![output](https://github.com/ruccho/FangAutoTile/assets/16096562/8589af18-871f-4ee0-97bc-6ec1ce3a9466)

## 🏔️ Overview

 - **Rapidity**: Fang Auto Tile needs **only 5 patterns of tiles** defined in the format to generate all 47 adjacent connecting tile patterns.
 - **Performance**: Fang Auto Tile has the powerful tile packer system that packs generated sprites into a single texture. It can help you to reduce rendering passes and improve GPU performance.
 - **Practicality**: Useful features are provided; animation, random, slopes, and additional maps.

## 📜 Table of Contents

- [Fang Auto Tile](#fang-auto-tile)
  - [🏔️ Overview](#️-overview)
  - [📜 Table of Contents](#-table-of-contents)
  - [🔥 Installation](#-installation)
    - [Requirements](#requirements)
      - [via UPM git dependencies](#via-upm-git-dependencies)
      - [via Asset Store](#via-asset-store)
    - [Samples](#samples)
  - [👉 Quick Start](#-quick-start)
    - [1. Create tile sheet](#1-create-tile-sheet)
    - [2. Import it to Unity](#2-import-it-to-unity)
      - [Tile Generation Settings](#tile-generation-settings)
    - [3. Use](#3-use)
  - [⚙️ Tile Settings](#️-tile-settings)
    - [Tile Generation Settings](#tile-generation-settings-1)
  - [🎞️ Animation / Random](#️-animation--random)
    - [Animation Settings](#animation-settings)
  - [💡 Sub-channels](#-sub-channels)
  - [📦 Packer](#-packer)
  - [🤝 Connector Tiles](#-connector-tiles)
  - [⛰️ Slopes (experimental)](#️-slopes-experimental)
  - [📝 Override Tiles](#-override-tiles)
  - [Tile Sheet Format](#tile-sheet-format)


## 🔥 Installation

### Requirements

 - Unity 2021.3 or later

#### via UPM git dependencies

Add git URL from Package Manager: 

```
https://github.com/ruccho/FangAutoTile.git?path=/Packages/com.ruccho.fang-auto-tile
```

#### via Asset Store

Asset page here (there is a time lag in updates): https://assetstore.unity.com/packages/slug/132602

### Samples

Samples are available in the package information page.

## 👉 Quick Start

### 1. Create tile sheet

[Download a template](https://github.com/ruccho/FangAutoTile/assets/16096562/98f244aa-3954-43c2-b55f-bd7edddd712f), or create a image in the size:

 - **Width**: tile size
 - **Height**: tile size * 5

Tile sheets must have 5 tile patterns arranged vertically like this:

![guide0](https://github.com/ruccho/FangAutoTile/assets/16096562/c92e97b9-61ff-46ce-b3a6-7e101d124046)

An example above is colored for convenience and will be displayed like this:

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/ab67473d-5724-485c-a3b6-8d6d92ec65ff)


### 2. Import it to Unity

Import tile sheet and **set copression settings to `None`**. 

Open create menu on the project view and select **Fang > Tile** to create a Fang Auto Tile asset.

Open the inspector of created tile asset. Here is a list of the essential settings:

#### Tile Generation Settings

 - **Main Channel**: Set the texture of the tile sheet you have imported.
 - **One Tile Per Unit**: Enable it if your tilemap is in one tile per one unit sizing. Disabling it to customize sizing.

Then click **`Generate!`** button.

### 3. Use

Place your tile asset on the **Tile Palette** window and use it!

## ⚙️ Tile Settings

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/be60cb25-13d6-4cc8-8fe7-dbf33133b784)

 - **Frame Mode**: described in [🎞️ Animation / Random](#️-animation--random) section below.
 - **Animation**: described in [🎞️ Animation / Random](#️-animation--random) section below.
 - **Collision**
   - `None`: the tile will have no collider.
   - `Sprite`: the tile will have a collider created from the sprite's physics shape.
   - `Grid`: the tile will have a collider that fills its tile rect.
 - **Is Slope**:  described in [⛰️ Slopes (experimental)](#️-slopes-experimental) section below.
 - **Connectable Tiles**: You can specify other Fang Auto Tiles that this tile should consider connected to.

### Tile Generation Settings

These settings is used in generation process and don't applied in realtime. To apply them, click `Generate!` button again.

 - **Enable Padding**: avoids dirty lines appear between tiles. It extrudes the pixels on the outer edge of tiles.
 - **One Tile Per Unit**: automatically sets number of pixels per unit (PPU) to be adjusted to 1 tile per unit. Disabling this will show custom PPU property.
 - **Physics Shape Generation**
   - `Sprite`: generates a physics shape from the content of the sprite.
   - `Fine`: generates a physics shape geometrically simple.
 - **Wrap Mode**: wrap mode of generated tile textures.
 - **Filter Mode**: filer mode of generated tile textures.
 - **Num Slopes**: described in [⛰️ Slopes (experimental)](#️-slopes-experimental) section below.
 - **Main Channel**: tile sheet texture used in generation process.
 - **Sub Channels**: described in [💡 Sub-channels](#-sub-channels) section below.
 - **Packer**: the packer used by this tile. (described in [📦 Packer](#-packer) section below)


## 🎞️ Animation / Random

Fang Auto Tile supports animated tiles and randomly selected tiles.

To use them, make a tile sheet **with frames arranged horizontally**.

![template_frames_large](https://github.com/ruccho/FangAutoTile/assets/16096562/e0ac4af1-1b9c-42fd-ac74-4f041833d1cb) 

After generating with the frames, set **Frame Mode** in Tile Settings to **Animation** or **Random**.

### Animation Settings

With **Animation** frame mode, additional settings are available in Tile Settings.

 - **Animation Min Speed**: the minimum possible speed at which the animation of the tile is played. 
 - **Animation Max Speed**: the maximum possible speed at which the animation of the tile is played.
 - **Animation Start Time**: the starting time of the animation.

## 💡 Sub-channels

You can use multiple channels for your tiles such as nornal maps and emission maps.

Fang Auto Tile generates a texture containing all adjascent combinations of the tile and the texture is used for actual rendering. To use multiple channels with Fang Auto Tiles, all channels have to be baked into each textures.

![guide1](https://github.com/ruccho/FangAutoTile/assets/16096562/3fac3fa4-2741-4e38-be4c-26cc9343b00a)

To work with this, set **Sub Channels** property in Tile Generation Settings.
Sub-channels must be same size as the main channel. 

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/0daae950-4079-462e-8386-023e79d49e91)

Then click **Generate!** again and baked textures will appear as sub assets of the tile asset.

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/a0157fd3-c0e5-4c73-a342-e07a158a1918)

## 📦 Packer

**Packer** is a feature to pack generated textures of multiple tiles into a single texture. It helps you to reduce rendering passes and improve performance.

Open create menu on the project view and select **Fang > Packer** to create a Fang Auto Tile Packer asset.

Then register Fang Auto Tile assets you want to pack and click `Generate!` button.

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/deabf80a-9aba-4223-adf6-c5dc460df8f0)

## 🤝 Connector Tiles

**Connector Tile** is a invisible tile that can be tricked into thinking it is adjacent to surrounding tiles.

For example, connector tiles can be used to **connect multiple divided tilemaps seamlessly**.

![guide2](https://github.com/ruccho/FangAutoTile/assets/16096562/617946e6-8336-4315-8188-02e2e5d3e2cb)

To create connector tiles, open create menu on the project view and select **Fang > Connector Tile**.

Connector tiles are invisible but you can make them visible only on the editor by setting the **Editor Tint** property in the inspector.

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/319d49bd-a769-4119-af8d-fb119a7e5a18)

## ⛰️ Slopes (experimental)

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/bcdf39fb-aaea-44e0-9f6f-2e12a3c7075a)

Fang Auto Tile can handle auto-tiled slopes.

> [!NOTE]
> Slope is a experimental feature and the tile sheet format to define slopes maybe changed in future.

Additional tiles for slopes have to be appended to the tile sheet.

![guide2](https://github.com/ruccho/FangAutoTile/assets/16096562/45773457-ac1b-4b34-bab6-1c23b9913942)

Slopes are appended in order of decreasing size.

First is size=1 section, tiles are arranged as follows:
1. ◢ Floor Up
2. ◣ Floor Down
3. ◤ Ceil Up
4. ◥ Ceil Down

Second is size=2 section. From this section, horizonral (2x1) and vertical (1x2) tiles are required. The slope shape order is the same as for size=1.
For each shapes, tiles are arranged in order of proximity from corner.

> [!NOTE]
> Available sizes of slopes are restricted to the height or the width is 1. (1x1, 2x1, 1x2, 3x1, 1x3, ...)
> It is because supporting arbitary sizes of slopes makes it too complex to resolve shapes automatically.

After preparing all sections, you have to specify number of slope sections included in your tile sheet and `Generate!` again.

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/bcb3038e-d79b-4090-b46e-ba2cd4b0f39c)

To set the tile is a slope, enable **Is Slope** property in Tile Settings.

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/ee54be08-247f-4476-a85f-d3fb663d2272)


## 📝 Override Tiles

With slopes, you may want to specify which tiles are slopes and the others are rectangle. You can use **Override Tiles** feature to create a variant of the original Fang Auto Tile asset that behaves as a slope tile or a rectangle tile.

To create override tiles, open create menu on the project view and select **Fang > Override Tile**.

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/0a6bc136-6d5a-4b0c-9712-6035f42952d8)

 - **Original**: Set original Fang Auto Tile asset.
 - **Is Slope**: Overrides original **Is Slope** property.
 - **Editor Tint**: Color applied to this tile to distinguish which tiles are overridden on the tilemap. This property is only applied in the edit mode.

This is the example how override tiles work (blue tiles are overridden as rectangle tiles):

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/51ab1da9-5724-4c8c-8a61-8e7d9c0b503c)


## Tile Sheet Format

The tile sheet format of Fang Auto Tile is the superset of the format used in **WOLF RPG Editor** by SmokingWOLF, which is a popular game development software especially in Japan. Materials made for WOLF RPG Editor can be used for Fang Auto Tile.

https://silversecond.com/WolfRPGEditor/