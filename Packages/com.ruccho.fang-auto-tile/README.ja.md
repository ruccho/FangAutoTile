# Fang Auto Tile

[English version](./README.md)

Fang Auto Tile は Unity で使用できる簡単で高機能なオートタイルシステムです。

![2024-01-01 16-44-32_re mkv](https://github.com/ruccho/FangAutoTile/assets/16096562/1433007a-02fb-4d4e-a650-29503fb5e24b)

![output](https://github.com/ruccho/FangAutoTile/assets/16096562/8589af18-871f-4ee0-97bc-6ec1ce3a9466)

## 🏔️ 概観

 - **効率性**: Fang Auto Tile はたった5パターンのタイル素材からすべての隣接状況に対応した47のタイルパターンを自動的に生成します。
 - **パフォーマンス**: Fang Auto Tile には強力な Packer システムがあり、複数のタイルから生成されたスプライトを一つのテクスチャにパッキングすることができます。これは描画パスを削減し、GPUパフォーマンスを向上するのに役立ちます。
 - **高機能**: アニメーション、ランダム、スロープ、サブチャンネルなどの便利な機能があります。

## 📜 目次

- [Fang Auto Tile](#fang-auto-tile)
  - [🏔️ 概観](#️-概観)
  - [📜 目次](#-目次)
  - [🔥 インストール](#-インストール)
    - [要件](#要件)
      - [UPM git dependencies 経由でのインストール](#upm-git-dependencies-経由でのインストール)
      - [Asset Store 経由でのインストール](#asset-store-経由でのインストール)
    - [サンプル](#サンプル)
  - [👉 Quick Start](#-quick-start)
    - [1. タイルシートを作成する](#1-タイルシートを作成する)
    - [2. Unity にインポートする](#2-unity-にインポートする)
      - [Tile Generation Settings](#tile-generation-settings)
    - [3. 使用する](#3-使用する)
  - [⚙️ タイル設定](#️-タイル設定)
    - [Tile Generation Settings](#tile-generation-settings-1)
  - [🎞️ アニメーション / ランダム](#️-アニメーション--ランダム)
    - [Animation Settings](#animation-settings)
  - [💡 サブチャンネル](#-サブチャンネル)
  - [📦 Packer](#-packer)
  - [🤝 Connector Tiles](#-connector-tiles)
  - [⛰️ スロープ (実験的)](#️-スロープ-実験的)
  - [📝 Override Tiles](#-override-tiles)
  - [タイルシートのフォーマット](#タイルシートのフォーマット)


## 🔥 インストール

### 要件

 - Unity 2021.3 以降

#### UPM git dependencies 経由でのインストール

Package Manager から次の git URL を追加してください:

```
https://github.com/ruccho/FangAutoTile.git?path=/Packages/com.ruccho.fang-auto-tile
```

#### Asset Store 経由でのインストール

アセットページはこちら (アップデートには時間差があります): https://assetstore.unity.com/packages/tools/sprite-management/fang-auto-tile-132602

### サンプル

Package Manager からサンプルをインポートできます。

## 👉 Quick Start

### 1. タイルシートを作成する

[テンプレートをダウンロードするか](https://github.com/ruccho/FangAutoTile/assets/16096562/98f244aa-3954-43c2-b55f-bd7edddd712f), 次のサイズの画像を作成します:

 - **幅**: タイルサイズ
 - **高さ**: タイルサイズ * 5

タイルシートは次の5つのタイルを縦に並べます：

![guide0](https://github.com/ruccho/FangAutoTile/assets/16096562/c92e97b9-61ff-46ce-b3a6-7e101d124046)

上の例では簡単のために色を付けており、実際には以下のように表示されます：

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/ab67473d-5724-485c-a3b6-8d6d92ec65ff)


### 2. Unity にインポートする

タイルシートをインポートし、**圧縮を`None`に設定します**。

Project ビューの Create メニューから **Fang > Tile**を選択して Fang Auto Tile のアセットを作成します。

作成したタイルアセットの Inspector を開き、次の必須の設定を行います：

#### Tile Generation Settings

 - **Main Channel**: インポートしたタイルシートをセットします。
 - **One Tile Per Unit**: 使用するタイルマップのサイズが1タイルにつき1 Unit の場合は有効にします。そのほかのサイズの場合はオフにするとカスタムのPPUを設定できます。

設定が完了したら **`Generate!`** ボタンをクリックします。

### 3. 使用する

タイルアセットを **Tile Palette** に配置して使用します。

## ⚙️ タイル設定

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/be60cb25-13d6-4cc8-8fe7-dbf33133b784)

 - **Frame Mode**: [🎞️ アニメーション / ランダム](#️-アニメーション--ランダム) の項で説明します。
 - **Animation**: [🎞️ アニメーション / ランダム](#️-アニメーション--ランダム) の項で説明します。
 - **Collision**
   - `None`: タイルは当たり判定を持ちません。
   - `Sprite`: タイルはスプライトから自動的に生成された当たり判定を持ちます。
   - `Grid`: タイルはグリッドいっぱいの当たり判定を持ちます。
 - **Is Slope**: [⛰️ スロープ (実験的)](#️-スロープ-実験的) の項で説明します。
 - **Connectable Tiles**: このタイルが接続しているとみなす、他の Fang Auto Tile を指定することができます。

### Tile Generation Settings

これらの設定は生成のプロセスで使用され、リアルタイムに反映されません。変更を反映するためには再度 `Generate!` ボタンをクリックしてください。

 - **Enable Padding**: タイルの間に表示される不正線を防止します。生成されるテクスチャ上でタイルの外縁のピクセルを外側に拡大することで行われます。
 - **One Tile Per Unit**: タイルの大きさを 1 Unit に設定します。チェックを外すと手動で Pixels Per Unit を調整できるプロパティが現れます。
 - **Physics Shape Generation**
   - `Sprite`: スプライトの内容に応じて自動的に Physics Shape を生成します。
   - `Fine`: 幾何学的に単純な Physics Shape を生成します。
 - **Wrap Mode**: 生成されるテクスチャの Wrap Mode です。
 - **Filter Mode**: 生成されるテクスチャの Filter Mode です。
 - **Num Slopes**: [⛰️ スロープ (実験的)](#️-スロープ-実験的) の項で説明します。
 - **Main Channel**: タイルシートをセットします。
 - **Sub Channels**: [💡 サブチャンネル](#-サブチャンネル) の項で説明します。
 - **Packer**: このタイルで使用されている Packer です。 ([📦 Packer](#-packer) の項で説明します。)


## 🎞️ アニメーション / ランダム

Fang Auto Tile はアニメーションタイルとランダムに見た目を変えるタイルをサポートしています。

これを行うためには、タイルシート上で**フレームを横に並べます**。

![template_frames_large](https://github.com/ruccho/FangAutoTile/assets/16096562/e0ac4af1-1b9c-42fd-ac74-4f041833d1cb) 

タイルシートから生成を行ったあと、Tile Settings の **Frame Mode** を **Animation** または **Random** に設定します。

### Animation Settings

Frame Mode が **Animation** のとき、追加の設定が Tile Settings で可能です。

 - **Animation Min Speed**: ランダムなアニメーション速度の下限です。
 - **Animation Max Speed**: ランダムなアニメーション速度の上限です。
 - **Animation Start Time**: アニメーションの開始時間です。

## 💡 サブチャンネル

ノーマルマップやエミッションマップなどの複数チャンネルを使用することができます。

Fang Auto Tile はすべての隣接パターンに対応するタイルのスプライトを生成しテクスチャ上に焼きこみます。このテクスチャが実際のレンダリング時に使用されます。Fang Auto Tile で複数チャンネルを使用するためには、すべてのチャンネルが別々のテクスチャに焼きこまれる必要があります。

![guide1](https://github.com/ruccho/FangAutoTile/assets/16096562/3fac3fa4-2741-4e38-be4c-26cc9343b00a)

これを行うためには、Tile Generation Settings の **Sub Channels** プロパティをセットします。サブチャンネルにセットするテクスチャのサイズはメインチャンネルと同じにする必要があります。

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/0daae950-4079-462e-8386-023e79d49e91)

再度 **Generate!** ボタンをクリックすると、生成された各チャンネルのテクスチャがタイルアセットのサブアセットとして現れます。

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/a0157fd3-c0e5-4c73-a342-e07a158a1918)

## 📦 Packer

**Packer** は複数タイルが生成したテクスチャを一つのテクスチャにパックできる機能です。これは描画パスを削減しパフォーマンスの向上に役立ちます。

Project ビューの Create メニューから **Fang > Packer** を選択し、Fang Auto Tile Packer アセットを作成します。

パッキングしたいタイルアセットを登録し `Generate!` ボタンをクリックします。

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/deabf80a-9aba-4223-adf6-c5dc460df8f0)

## 🤝 Connector Tiles

**Connector Tile** は、隣接する Fang Auto Tile に、同じタイルが接続していると誤認させることができる透明なタイルです。

例えば、Connector Tile は複数の分割されたタイルマップ上の Fang Auto Tile どうしをシームレスにつなげることができます。

![guide2](https://github.com/ruccho/FangAutoTile/assets/16096562/617946e6-8336-4315-8188-02e2e5d3e2cb)

Connector Tile を作成するには、Project ビューの Create メニューで **Fang > Connector Tile** を選択します。

Connector Tile は透明ですが、エディタ上でのみ色を付けて表示することができます。これは Inspector の **Editor Tint** プロパティで設定できます。

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/319d49bd-a769-4119-af8d-fb119a7e5a18)

## ⛰️ スロープ (実験的)

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/bcdf39fb-aaea-44e0-9f6f-2e12a3c7075a)

Fang Auto Tile ではスロープを自動的に扱うことができます。

> [!NOTE]
> スロープは実験的な機能であり、必要なタイルシートのフォーマット等は今後変更される可能性があります。 

スロープのための追加のタイルをタイルシートに追加する必要があります。

![guide2](https://github.com/ruccho/FangAutoTile/assets/16096562/45773457-ac1b-4b34-bab6-1c23b9913942)

スロープはサイズの小さい順に追加します。

最初はサイズが1のセクションで、タイルは以下の順に並べられます。

1. ◢ 上る床
2. ◣ 下る床
3. ◤ 上る天井
4. ◥ 下る天井

次はサイズが2のセクションですが、これ以降のセクションでは横方向（2x1）と縦方向（1x2）の両方のタイルが必要になります。スロープ形状の並び順はサイズが1のときと同様です。
それぞれのスロープ形状に対して、タイルは角から近い順に並べます。

> [!NOTE]
> 使用可能なスロープサイズは幅か高さが1のものに限定されます。
> これは任意サイズのスロープ形状を自動的に解決することが非常に複雑になってしまうためです。

すべてのセクションを準備できたら、タイルシートに含まれるスロープセクションの数を指定した後、再び `Generate!` を行います。

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/bcb3038e-d79b-4090-b46e-ba2cd4b0f39c)

また、**Is Slope** プロパティを有効にしてタイルがスロープであることを指定する必要があります。

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/ee54be08-247f-4476-a85f-d3fb663d2272)


## 📝 Override Tiles

スロープでは、どのタイルがスロープなのか・あるいはスロープではないのかを指定したいかと思います。**Override Tile** 使うことで、オリジナルの Fang Auto Tile アセットのバリアントを作成し、**Is Slope** プロパティをオーバーライドすることができます。

Override Tile を作成するには、Project ビューの Create メニューから **Fang > Override Tile** を選択します。

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/0a6bc136-6d5a-4b0c-9712-6035f42952d8)

 - **Original**: オリジナルの Fang Auto Tile アセットをセットします。
 - **Is Slope**: オリジナルの **Is Slope** プロパティです。
 - **Editor Tint**: タイルマップ上でどのタイルがオーバーライドされているかを判別するために適用される乗算色。Edit Modeでのみ適用されます。

こちらはオーバーライドタイルの使用例です。（青いタイルが非スロープタイルとしてオーバーライドされています）

![image](https://github.com/ruccho/FangAutoTile/assets/16096562/51ab1da9-5724-4c8c-8a61-8e7d9c0b503c)


## タイルシートのフォーマット

タイルシートのフォーマットは **WOLF RPGエディター**で使われているフォーマットのスーパーセットになっており、WOLF RPGエディターのために作成されたオートタイル素材をFang Auto Tileで使用することができます。

https://silversecond.com/WolfRPGEditor/