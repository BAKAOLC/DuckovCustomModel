# Duckov Custom Model

English | [中文](README.md)

A custom player model mod for Duckov game.

## Basic Features

- **Custom Model Replacement**: Allows players to use custom models to replace character models in the game
- **Model Selection Interface**: Provides a graphical interface for browsing and selecting available models
- **Model Search**: Supports searching models by name, ID, and other keywords
- **Model Management**: Automatically scans and loads model bundles, supports multiple model bundles simultaneously
- **Quick Switch**: Supports quick model switching in-game without restarting

## Configuration Files

Configuration files are located at: `<Game Installation Path>/ModConfigs/DuckovCustomModel`

### UIConfig.json

UI interface related configuration.

```json
{
  "ToggleKey": "Backslash",
  "HideOriginalEquipment": false
}
```

- `ToggleKey`: Key to open/close the model selection interface (default: `Backslash`, i.e., backslash key `\`)
  - Supported key values can refer to Unity KeyCode enum
- `HideOriginalEquipment`: Whether to hide original equipment (default: `false`)
  - When set to `true`, the Animator's `HideOriginalEquipment` parameter will be set to `true`
  - Can be toggled in the settings area of the model selection interface

### UsingModel.json

Current model configuration in use.

```json
{
  "ModelID": ""
}
```

- `ModelID`: Currently used model ID (string, uses original model when empty)
  - After setting, the game will automatically apply this model when loading levels
  - Can be modified through the model selection interface, changes will be automatically saved to this file

## Model Selection Interface

The model selection interface provides the following features:

- **Model Browsing**: Scroll to view all available models
- **Model Search**: Quickly search models by name, ID, and other keywords
- **Model Selection**: Click the model button to apply the model
- **Settings Options**: Toggle "Hide Original Equipment" option at the bottom of the interface
  - This option is immediately saved to the configuration file
  - Affects the Animator's `HideOriginalEquipment` parameter value

### Opening the Model Selection Interface

- Default key: `\` (backslash key)
- Can be changed by modifying `ToggleKey` in `UIConfig.json`
- Press `ESC` key to close the interface

## Model Installation

Place model bundles at: `<Game Installation Path>/ModConfigs/DuckovCustomModel/Models`

Each model bundle should be placed in a separate folder, containing model resource files and configuration information.

### Model Bundle Structure

Each model bundle folder should contain the following files:

```
Model Bundle Folder/
├── bundleinfo.json          # Model bundle configuration file (required)
├── modelbundle.assetbundle  # Unity AssetBundle file (required)
└── thumbnail.png            # Thumbnail file (optional, can also be placed inside AssetBundle)
```

### bundleinfo.json Format

```json
{
  "BundleName": "Model Bundle Name",
  "BundlePath": "modelbundle.assetbundle",
  "Models": [
    {
      "ModelID": "unique_model_id",
      "Name": "Model Display Name",
      "Author": "Author Name",
      "Description": "Model Description",
      "Version": "1.0.0",
      "ThumbnailPath": "thumbnail.png",
      "PrefabPath": "Assets/Model.prefab"
    }
  ]
}
```

#### Field Descriptions

**BundleName** (required): Model bundle name, used for identification and display

**BundlePath** (required): AssetBundle file path, relative to the model bundle folder

**Models** (required): Model information array, can contain multiple models

**ModelInfo Fields**:

- `ModelID` (required): Unique identifier for the model, used to reference the model in configuration files
- `Name` (optional): Name displayed in the interface
- `Author` (optional): Model author
- `Description` (optional): Model description information
- `Version` (optional): Model version number
- `ThumbnailPath` (optional): Thumbnail path
  - Can be a resource path inside the AssetBundle (e.g., `"Assets/Thumbnail.png"`)
  - Can also be an external file path relative to the model bundle folder (e.g., `"thumbnail.png"`)
- `PrefabPath` (required): Model Prefab resource path inside the AssetBundle (e.g., `"Assets/Model.prefab"`)

## Animator Configuration

Custom model Prefabs need to include an `Animator` component and configure the corresponding Animator Controller.

### Animator Controller Parameters

The Animator Controller can use the following parameters:

#### Bool Type Parameters

- `Grounded`: Whether the character is on the ground
- `Die`: Whether the character is dead
- `Moving`: Whether the character is moving
- `Running`: Whether the character is running
- `Dashing`: Whether the character is dashing
- `GunReady`: Whether the gun is ready
- `Reloading`: Whether reloading
- `RightHandOut`: Whether the right hand is extended
- `HideOriginalEquipment`: Whether to hide original equipment (controlled by the `HideOriginalEquipment` config option)
- `LeftHandEquip`: Whether there is equipment in the left hand slot (determined by equipment TypeID, `true` when TypeID > 0)
- `RightHandEquip`: Whether there is equipment in the right hand slot (determined by equipment TypeID, `true` when TypeID > 0)
- `ArmorEquip`: Whether there is equipment in the armor slot (determined by equipment TypeID, `true` when TypeID > 0)
- `HelmetEquip`: Whether there is equipment in the helmet slot (determined by equipment TypeID, `true` when TypeID > 0)
- `HeadsetEquip`: Whether there is equipment in the headset slot (determined by equipment TypeID, `true` when TypeID > 0)
- `FaceEquip`: Whether there is equipment in the face slot (determined by equipment TypeID, `true` when TypeID > 0)
- `BackpackEquip`: Whether there is equipment in the backpack slot (determined by equipment TypeID, `true` when TypeID > 0)
- `MeleeWeaponEquip`: Whether there is equipment in the melee weapon slot (determined by equipment TypeID, `true` when TypeID > 0)
- `HavePopText`: Whether there is pop text (checks if the pop text slot has child objects)

#### Float Type Parameters

- `MoveSpeed`: Movement speed ratio (normal walk 0~1, running can reach 2)
- `MoveDirX`: Movement direction X component (-1.0 ~ 1.0, character local coordinate system)
- `MoveDirY`: Movement direction Y component (-1.0 ~ 1.0, character local coordinate system)
- `HealthRate`: Health ratio (0.0 - 1.0, current health / max health)
- `WaterRate`: Water ratio (0.0 - 1.0, current water / max water)
- `WeightRate`: Weight ratio (current total weight / max carrying capacity, may exceed 1.0)

#### Int Type Parameters

- `HandState`: Hand state
  - `0`: Default state
  - `1`: Normal
  - `2`: Gun
  - `3`: Melee weapon
  - `4`: Bow
  - `-1`: Carrying state
- `WeightState`: Weight state (only effective in Raid maps)
  - `0`: Light (WeightRate ≤ 0.25)
  - `1`: Normal (0.25 < WeightRate ≤ 0.75)
  - `2`: Heavy (0.75 < WeightRate ≤ 1.0)
  - `3`: Overloaded (WeightRate > 1.0)
- `LeftHandTypeID`: TypeID of equipment in left hand (0 when no equipment)
- `RightHandTypeID`: TypeID of equipment in right hand (0 when no equipment)
- `ArmorTypeID`: TypeID of armor equipment (0 when no equipment)
- `HelmetTypeID`: TypeID of helmet equipment (0 when no equipment)
- `HeadsetTypeID`: TypeID of headset equipment (0 when no equipment)
- `FaceTypeID`: TypeID of face equipment (0 when no equipment)
- `BackpackTypeID`: TypeID of backpack equipment (0 when no equipment)
- `MeleeWeaponTypeID`: TypeID of melee weapon equipment (0 when no equipment)

#### Trigger Type Parameters

- `Attack`: Attack trigger (used to trigger melee attack animations)

### Optional Animation Layer

If the model contains melee attack animations, you can add an animation layer named `"MeleeAttack"`:

- Layer name must be `"MeleeAttack"`
- This layer is used to play melee attack animations
- Layer weight will be automatically adjusted based on attack state

### Animator Workflow

1. The mod automatically reads game state and updates Animator parameters
2. Movement, jumping, dashing, and other states are synchronized in real-time to the custom model's Animator
3. Actions like attacking and reloading trigger corresponding animation parameters
4. If the `MeleeAttack` layer exists, its weight will be automatically adjusted during attacks to play attack animations

