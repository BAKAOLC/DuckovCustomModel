# Duckov Custom Model

English | [中文](README.md)

A custom player model mod for Duckov game.

## Basic Features

- **Custom Model Replacement**: Allows players to use custom models to replace character models in the game
- **Model Selection Interface**: Provides a graphical interface for browsing and selecting available models
- **Model Search**: Supports searching models by name, ID, and other keywords
- **Model Management**: Automatically scans and loads model bundles, supports multiple model bundles simultaneously
- **Incremental Updates**: Uses hash caching mechanism to only update changed model bundles, improving refresh efficiency
- **Multi-Object Support**: Each model target type (ModelTarget) can correspond to multiple game objects, applying changes uniformly to all objects when switching
- **Quick Switch**: Supports quick model switching in-game without restarting

## Configuration Files

Configuration files are located at: `<Game Installation Path>/ModConfigs/DuckovCustomModel`

### UIConfig.json

UI interface related configuration.

```json
{
  "ToggleKey": "Backslash"
}
```

- `ToggleKey`: Key to open/close the model selection interface (default: `Backslash`, i.e., backslash key `\`)
  - Supported key values can refer to Unity KeyCode enum

### HideEquipmentConfig.json

Hide equipment configuration. Uses `ModelTarget` as the key, making it easy to extend with new model target types in the future.

```json
{
  "HideEquipment": {
    "Character": false,
    "Pet": false
  }
}
```

- `HideEquipment`: Dictionary type, where keys are `ModelTarget` enum values (e.g., `"Character"`, `"Pet"`), and values are boolean
  - `Character`: Whether to hide character's original equipment (default: `false`)
    - When set to `true`, the character model's Animator's `HideOriginalEquipment` parameter will be set to `true`
    - Can be toggled in the settings area of the model selection interface
  - `Pet`: Whether to hide pet's original equipment (default: `false`)
    - When set to `true`, the pet model's Animator's `HideOriginalEquipment` parameter will be set to `true`
    - Can be toggled in the settings area of the model selection interface
  - When new `ModelTarget` types are added, the configuration will automatically include that type (default value: `false`)

**Compatibility Note**: If old `HideCharacterEquipment` or `HidePetEquipment` configurations exist in `UIConfig.json`, the system will automatically migrate them to the new `HideEquipmentConfig.json` file.

### UsingModel.json

Current model configuration in use. Uses `ModelTarget` as the key, making it easy to extend with new model target types in the future.

```json
{
  "ModelIDs": {
    "Character": "",
    "Pet": ""
  }
}
```

- `ModelIDs`: Dictionary type, where keys are `ModelTarget` enum values (e.g., `"Character"`, `"Pet"`), and values are model IDs (string, uses original model when empty)
  - `Character`: Currently used character model ID
    - After setting, the game will automatically apply this model to all character objects when loading levels
    - Can be modified through the model selection interface, changes will be automatically saved to this file
  - `Pet`: Currently used pet model ID
    - After setting, the game will automatically apply this model to all pet objects when loading levels
    - Can be modified through the model selection interface, changes will be automatically saved to this file
  - When new `ModelTarget` types are added, the configuration will automatically support that type

**Compatibility Note**: If old `ModelID` or `PetModelID` fields exist in the configuration file, the system will automatically migrate them to the new `ModelIDs` dictionary format. After migration, the configuration file will only contain the `ModelIDs` dictionary.

## Model Selection Interface

The model selection interface provides the following features:

- **Target Type Switching**: Switch between "Character" and "Pet" to manage character models and pet models separately
- **Model Browsing**: Scroll to view all available models (filtered based on the currently selected target type)
- **Model Search**: Quickly search models by name, ID, and other keywords
- **Model Selection**: Click the model button to apply the model to all objects of that target type
- **Model Information**: Each model card displays the model name, ID, author, version, and the Bundle name it belongs to
- **Settings Options**: Toggle "Hide Original Equipment" options at the bottom of the interface
  - Separate options for "Hide Character Equipment" and "Hide Pet Equipment"
  - These options are immediately saved to the configuration file
  - Affect the Animator's `HideOriginalEquipment` parameter value

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
      "PrefabPath": "Assets/Model.prefab",
      "Target": ["Character"]
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
- `Target` (optional): Array of target types the model applies to (default: `["Character"]`)
  - Valid values: `"Character"`, `"Pet"`
  - Can contain multiple values, indicating the model is compatible with both characters and pets
  - The model selection interface will filter and display compatible models based on the currently selected target type
- `SoundInfos` (optional): Array of sound information, supports configuring tags for sounds
  - Each sound can be configured with multiple tags (`normal`, `surprise`, `death`)
  - Defaults to `"normal"` tag when no tags are specified
  - The same sound file can be used for multiple scenarios

## Locator Points

To ensure that equipment (weapons, armor, backpacks, etc.) in the game can be correctly bound to custom models, the model Prefab needs to include corresponding locator point GameObjects.

### Required Locator Points

The model Prefab's child objects need to include Transforms with the following names as locator points:

- `LeftHandLocator`: Left hand locator point for binding left hand equipment
- `RightHandLocator`: Right hand locator point for binding right hand equipment
- `ArmorLocator`: Armor locator point for binding armor equipment
- `HelmetLocator`: Helmet locator point for binding helmet equipment
- `FaceLocator`: Face locator point for binding face equipment
- `BackpackLocator`: Backpack locator point for binding backpack equipment
- `MeleeWeaponLocator`: Melee weapon locator point for binding melee weapon equipment
- `PopTextLocator`: Pop text locator point for displaying pop text

### Function of Locator Points

- The mod automatically searches for these locator points in custom models
- Found locator points will be set as binding points for the game's equipment system
- Equipment will be bound according to the position and rotation of the locator points
- If a locator point does not exist, the corresponding equipment will not display correctly on the custom model

### Notes

- Locator point names must **exactly match** (case-sensitive)
- Locator points can be empty GameObjects, just need to set the correct position and rotation
- It is recommended to set locator point positions based on the original model's equipment positions

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
- `HideOriginalEquipment`: Whether to hide original equipment (controlled by the corresponding `ModelTarget` configuration in `HideEquipmentConfig.json`)
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

- `CurrentCharacterType`: Current character type
  - `0`: Character
  - `1`: Pet
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

## Custom Sounds

The mod supports configuring sounds for custom models, including both player key press triggers and AI automatic triggers.

### Sound Configuration

Sounds can be configured in `ModelInfo` within `bundleinfo.json`:

```json
{
  "ModelID": "unique_model_id",
  "Name": "Model Display Name",
  "SoundInfos": [
    {
      "Path": "sounds/normal1.wav",
      "Tags": ["normal"]
    },
    {
      "Path": "sounds/normal2.wav",
      "Tags": ["normal"]
    },
    {
      "Path": "sounds/surprise.wav",
      "Tags": ["surprise", "normal"]
    },
    {
      "Path": "sounds/death.wav",
      "Tags": ["death"]
    }
  ]
}
```

#### SoundInfo Field Descriptions

- `Path` (required): Sound file path, relative to the model bundle folder
- `Tags` (optional): Array of sound tags, used to specify sound usage scenarios
  - `"normal"`: Normal sound, used for player key press triggers and AI normal state
  - `"surprise"`: Surprise sound, used for AI surprise state
  - `"death"`: Death sound, used for AI death state
  - Can contain multiple tags, indicating the sound can be used in multiple scenarios
  - Defaults to `["normal"]` when no tags are specified


### Sound Trigger Methods

#### Player Key Press Trigger

- When a character model has sounds configured, pressing the `Quack` key in-game will trigger a sound
- Only sounds tagged with `"normal"` will be played
- Randomly selects one sound from all sounds tagged with `"normal"`
- Only player characters respond to key presses, pets do not trigger
- When playing a sound, it also creates an AI sound, allowing other AIs to hear the player's sound

#### AI Automatic Trigger

- AI will automatically trigger sounds with corresponding tags based on game state
- `"normal"`: Triggered during AI normal state
- `"surprise"`: Triggered during AI surprise state
- `"death"`: Triggered during AI death state
- If a sound with the specified tag doesn't exist, it will fall back to sounds tagged with `"normal"`

### Sound File Requirements

- Sound files should be placed inside the model bundle folder
- Supports audio formats used by the game (typically WAV, OGG, etc.)
- Sound file paths are specified in `SoundInfo.Path`, relative to the model bundle folder
- Example: If the model bundle folder is `MyModel/` and the sound file is `MyModel/sounds/voice.wav`, then `Path` should be set to `"sounds/voice.wav"`

### Notes

- If a model has no sounds configured, it will not affect other functionality

