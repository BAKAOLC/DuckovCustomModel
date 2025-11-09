# Duckov Custom Model

[English](README_EN.md) | 中文

[更新日志](CHANGELOG.md)

一个用于 Duckov 游戏的自定义玩家模型模组。

## 基本功能

- **自定义模型替换**：允许玩家使用自定义模型替换游戏中的角色模型
- **模型选择界面**：提供图形化界面，方便玩家浏览和选择可用模型
- **模型搜索**：支持通过模型名称、ID 等关键词搜索模型
- **模型管理**：自动扫描并加载模型包，支持多个模型包同时存在
- **增量更新**：使用哈希缓存机制，只更新变更的模型包，提高刷新效率
- **多对象支持**：每个模型目标类型（ModelTarget）可对应多个游戏对象，切换时统一应用到所有对象
- **快速切换**：支持在游戏中快速切换模型，无需重启游戏

## 配置文件

配置文件位于：`游戏安装路径/ModConfigs/DuckovCustomModel`

### UIConfig.json

UI 界面相关配置。

```json
{
  "ToggleKey": "Backslash"
}
```

- `ToggleKey`：打开/关闭模型选择界面的按键（默认：`Backslash`，即反斜杠键 `\`）
  - 支持的按键值可参考 Unity KeyCode 枚举

### HideEquipmentConfig.json

隐藏装备配置。使用 `ModelTarget` 作为键，便于后续扩展新的模型目标类型。

```json
{
  "HideEquipment": {
    "Character": false,
    "Pet": false
  }
}
```

- `HideEquipment`：字典类型，键为 `ModelTarget` 枚举值（如 `"Character"`、`"Pet"`），值为布尔类型
  - `Character`：是否隐藏角色原有装备（默认：`false`）
    - 设置为 `true` 时，角色模型的 Animator 的 `HideOriginalEquipment` 参数会被设置为 `true`
    - 可在模型选择界面的设置区域中切换此选项
  - `Pet`：是否隐藏宠物原有装备（默认：`false`）
    - 设置为 `true` 时，宠物模型的 Animator 的 `HideOriginalEquipment` 参数会被设置为 `true`
    - 可在模型选择界面的设置区域中切换此选项
  - 当添加新的 `ModelTarget` 类型时，配置会自动包含该类型（默认值为 `false`）

**兼容性说明**：如果 `UIConfig.json` 中存在旧的 `HideCharacterEquipment` 或 `HidePetEquipment` 配置，系统会自动迁移到新的 `HideEquipmentConfig.json` 文件中。

### UsingModel.json

当前使用的模型配置。使用 `ModelTarget` 作为键，便于后续扩展新的模型目标类型。

```json
{
  "ModelIDs": {
    "Character": "",
    "Pet": ""
  },
  "AICharacterModelIDs": {
    "Cname_Wolf": "",
    "Cname_Scav": "",
    "*": ""
  }
}
```

- `ModelIDs`：字典类型，键为 `ModelTarget` 枚举值（如 `"Character"`、`"Pet"`），值为模型 ID（字符串，为空时使用原始模型）
  - `Character`：当前使用的角色模型 ID
    - 设置后，游戏会在关卡加载时自动应用该模型到所有角色对象
    - 可通过模型选择界面修改，修改后会自动保存到此文件
  - `Pet`：当前使用的宠物模型 ID
    - 设置后，游戏会在关卡加载时自动应用该模型到所有宠物对象
    - 可通过模型选择界面修改，修改后会自动保存到此文件
  - 当添加新的 `ModelTarget` 类型时，配置会自动支持该类型

- `AICharacterModelIDs`：字典类型，键为 AI 角色名称键（如 `"Cname_Wolf"`、`"Cname_Scav"`），值为模型 ID（字符串，为空时使用原始模型）
  - 可以为每个 AI 角色单独配置模型
  - 特殊键 `"*"`：为所有 AI 角色设置默认模型
    - 当某个 AI 角色没有单独配置模型时，会使用 `"*"` 对应的模型
    - 如果 `"*"` 也没有配置，则使用原始模型
  - 可通过模型选择界面修改，修改后会自动保存到此文件

**兼容性说明**：如果配置文件中存在旧的 `ModelID` 或 `PetModelID` 字段，系统会自动迁移到新的 `ModelIDs` 字典格式。迁移完成后，配置文件将只包含 `ModelIDs` 字典。

### IdleAudioConfig.json

待机音频自动播放间隔配置。用于配置不同角色的自动播放间隔（最小值和最大值）。

```json
{
  "IdleAudioIntervals": {
    "Pet": {
      "Min": 30.0,
      "Max": 45.0
    }
  },
  "AICharacterIdleAudioIntervals": {
    "Cname_Wolf": {
      "Min": 20.0,
      "Max": 30.0
    },
    "*": {
      "Min": 30.0,
      "Max": 45.0
    }
  },
  "EnableIdleAudio": {
    "Character": false,
    "Pet": true
  },
  "AICharacterEnableIdleAudio": {
    "*": true
  }
}
```

- `IdleAudioIntervals`：字典类型，键为 `ModelTarget` 枚举值（如 `"Character"`、`"Pet"`），值为包含 `Min` 和 `Max` 的对象
  - `Pet`：宠物角色的待机音频播放间隔（秒）
    - `Min`：最小间隔时间（默认：`30.0`）
    - `Max`：最大间隔时间（默认：`45.0`）
    - 系统会在最小值和最大值之间随机选择间隔时间
  - 当添加新的 `ModelTarget` 类型时，配置会自动包含该类型（默认值：`Min: 30.0, Max: 45.0`）

- `AICharacterIdleAudioIntervals`：字典类型，键为 AI 角色名称键（如 `"Cname_Wolf"`、`"Cname_Scav"`），值为包含 `Min` 和 `Max` 的对象
  - 可以为每个 AI 角色单独配置待机音频播放间隔
  - 特殊键 `"*"`：为所有 AI 角色设置默认间隔
    - 当某个 AI 角色没有单独配置间隔时，会使用 `"*"` 对应的间隔
    - 如果 `"*"` 也没有配置，则使用默认值（`Min: 30.0, Max: 45.0`）
  - `Min`：最小间隔时间（默认：`30.0`）
  - `Max`：最大间隔时间（默认：`45.0`）
  - 系统会在最小值和最大值之间随机选择间隔时间

- `EnableIdleAudio`：字典类型，键为 `ModelTarget` 枚举值（如 `"Character"`、`"Pet"`），值为布尔值，控制该角色类型是否允许自动播放待机音频
  - `Character`：玩家角色是否允许自动播放待机音频（默认：`false`）
  - `Pet`：宠物角色是否允许自动播放待机音频（默认：`true`）
  - 当添加新的 `ModelTarget` 类型时，配置会自动包含该类型（默认值：`Character` 为 `false`，其他为 `true`）

- `AICharacterEnableIdleAudio`：字典类型，键为 AI 角色名称键（如 `"Cname_Wolf"`、`"Cname_Scav"`），值为布尔值，控制该 AI 角色是否允许自动播放待机音频
  - 可以为每个 AI 角色单独配置是否允许自动播放待机音频
  - 特殊键 `"*"`：为所有 AI 角色设置默认值
    - 当某个 AI 角色没有单独配置时，会使用 `"*"` 对应的值
    - 如果 `"*"` 也没有配置，则使用默认值（`true`）
  - 默认值：`true`（允许自动播放）

**注意事项**：
- 最小间隔时间不能小于 0.1 秒
- 最大间隔时间不能小于最小间隔时间
- 只有配置了 `"idle"` 标签音效的模型才会自动播放待机音效
- 只有启用了自动播放的角色类型才会自动播放待机音效（通过 `EnableIdleAudio` 和 `AICharacterEnableIdleAudio` 配置控制）
- 玩家角色默认不允许自动播放待机音效，可以通过配置启用

## 模型选择界面

模型选择界面提供了以下功能：

- **目标类型切换**：可以在"角色"、"宠物"和"AI角色"之间切换，分别管理角色模型、宠物模型和 AI 角色模型
- **模型浏览**：滚动查看所有可用的模型（会根据当前选择的目标类型过滤显示）
- **模型搜索**：通过模型名称、ID 等关键词快速搜索模型
- **模型选择**：点击模型按钮即可应用该模型到所有属于该目标类型的对象
- **模型信息**：每个模型卡片显示模型名称、ID、作者、版本以及所属的 Bundle 名称
- **AI 角色选择**：当选择"AI角色"目标类型时，需要先选择具体的 AI 角色，然后为该角色选择模型
- **设置选项**：在界面底部可以切换"隐藏原有装备"选项
  - 分别有"隐藏角色装备"和"隐藏宠物装备"选项
  - 此选项会立即保存到配置文件
  - 影响 Animator 的 `HideOriginalEquipment` 参数值
- **AI 角色装备设置**：当选择"AI角色"目标类型并选择具体 AI 角色后，在模型列表页面会显示针对该 AI 角色的"隐藏装备"选项
  - 每个 AI 角色都有独立的隐藏装备设置
  - 此选项会立即保存到配置文件
  - 影响 Animator 的 `HideOriginalEquipment` 参数值

### 打开模型选择界面

- 默认按键：`\`（反斜杠键）
- 可通过修改 `UIConfig.json` 中的 `ToggleKey` 来更改按键
- 按 `ESC` 键可关闭界面

## 模型安装

将模型包放置在：`游戏安装路径/ModConfigs/DuckovCustomModel/Models`

每个模型包应放在独立的文件夹中，包含模型资源文件和配置信息。

### 模型包结构

每个模型包文件夹应包含以下文件：

```
模型包文件夹/
├── bundleinfo.json          # 模型包配置文件（必需）
├── modelbundle.assetbundle  # Unity AssetBundle 文件（必需）
└── thumbnail.png            # 缩略图文件（可选）
```

### bundleinfo.json 格式

```json
{
  "BundleName": "模型包名称",
  "BundlePath": "modelbundle.assetbundle",
  "Models": [
    {
      "ModelID": "unique_model_id",
      "Name": "模型显示名称",
      "Author": "作者名称",
      "Description": "模型描述",
      "Version": "1.0.0",
      "ThumbnailPath": "thumbnail.png",
      "PrefabPath": "Assets/Model.prefab",
      "DeathLootBoxPrefabPath": "Assets/DeathLootBox.prefab",
      "Target": ["Character", "AICharacter"],
      "SupportedAICharacters": ["Cname_Wolf", "Cname_Scav", "*"],
      "CustomSounds": [
        {
          "Path": "sounds/normal1.wav",
          "Tags": ["normal"]
        },
        {
          "Path": "sounds/surprise.wav",
          "Tags": ["surprise", "normal"]
        },
        {
          "Path": "sounds/death.wav",
          "Tags": ["death"]
        },
        {
          "Path": "sounds/idle1.wav",
          "Tags": ["idle"]
        }
      ]
    }
  ]
}
```

#### 字段说明

**BundleName**（必需）：模型包名称，用于标识和显示

**BundlePath**（必需）：AssetBundle 文件路径，相对于模型包文件夹的路径

**Models**（必需）：模型信息数组，可包含多个模型

**ModelInfo 字段**：

- `ModelID`（必需）：模型的唯一标识符，用于在配置文件中引用模型
- `Name`（可选）：模型在界面中显示的名称
- `Author`（可选）：模型作者
- `Description`（可选）：模型描述信息
- `Version`（可选）：模型版本号
- `ThumbnailPath`（可选）：缩略图路径，相对于模型包文件夹的外部文件路径（如 `"thumbnail.png"`）
- `PrefabPath`（必需）：模型 Prefab 在 AssetBundle 内的资源路径（如 `"Assets/Model.prefab"`）
- `DeathLootBoxPrefabPath`（可选）：死亡战利品箱 Prefab 在 AssetBundle 内的资源路径（如 `"Assets/DeathLootBox.prefab"`）
  - 当角色使用该模型并死亡时，如果配置了此字段，死亡战利品箱会使用自定义的 Prefab 替换默认模型
  - 如果未配置此字段，死亡战利品箱将使用默认模型
- `Target`（可选）：模型适用的目标类型数组（默认：`["Character"]`）
  - 可选值：`"Character"`（角色）、`"Pet"`（宠物）、`"AICharacter"`（AI 角色）
  - 可以同时包含多个值，表示该模型同时适用于多个目标类型
  - 模型选择界面会根据当前选择的目标类型过滤显示兼容的模型
- `SupportedAICharacters`（可选）：支持的 AI 角色名称键数组（仅在 `Target` 包含 `"AICharacter"` 时有效）
  - 可以指定该模型适用于哪些 AI 角色
  - 特殊值 `"*"`：表示该模型适用于所有 AI 角色
  - 如果为空数组且 `Target` 包含 `"AICharacter"`，则该模型不会应用于任何 AI 角色
- `CustomSounds`（可选）：自定义音效信息数组，支持为音效配置标签
  - 每个音效可以配置多个标签（`normal`、`surprise`、`death`）
  - 未指定标签时，默认为 `["normal"]`
  - 同一音效文件可以同时用于多个场景
  - 音效文件路径在 `Path` 中指定，相对于模型包文件夹

## 定位锚点

为了确保游戏中的装备（武器、护甲、背包等）能够正确绑定到自定义模型上，模型 Prefab 需要包含相应的定位锚点（Locator）GameObject。

### 必需的定位锚点

模型 Prefab 的子对象中需要包含以下名称的 Transform 作为定位锚点：

- `LeftHandLocator`：左手定位锚点，用于绑定左手装备
- `RightHandLocator`：右手定位锚点，用于绑定右手装备
- `ArmorLocator`：护甲定位锚点，用于绑定护甲装备
- `HelmetLocator`：头盔定位锚点，用于绑定头盔装备
- `FaceLocator`：面部定位锚点，用于绑定面部装备
- `BackpackLocator`：背包定位锚点，用于绑定背包装备
- `MeleeWeaponLocator`：近战武器定位锚点，用于绑定近战武器装备
- `PopTextLocator`：弹出文本定位锚点，用于显示弹出文本

### 可选的定位锚点

除了必需的定位锚点外，模型还可以包含以下可选的定位锚点：

- `PaperBoxLocator`：纸箱定位锚点，用于绑定纸箱
  - 当自定义模型包含此定位锚点时，游戏中生成的纸箱会自动附加到此定位点
  - 纸箱会跟随自定义模型的位置和旋转
  - 如果模型不包含此定位锚点，纸箱将使用原始模型的定位点
- `CarriableLocator`：可搬运物品定位锚点，用于绑定可搬运物品
  - 当自定义模型包含此定位锚点时，角色搬运物品时会自动附加到此定位点
  - 可搬运物品会跟随自定义模型的位置和旋转
  - 搬运物品时会保存原始的位置、旋转和缩放信息，放下物品时会恢复
  - 如果模型不包含此定位锚点，可搬运物品将使用原始模型的定位点

### 定位锚点的作用

- 模组会自动在自定义模型中搜索这些定位锚点
- 找到的定位锚点会被设置为游戏装备系统的绑定点
- 装备会按照定位锚点的位置和旋转进行绑定
- 如果某个定位锚点不存在，对应的装备将无法正确显示在自定义模型上

### 注意事项

- 定位锚点的名称必须**完全匹配**（区分大小写）
- 定位锚点可以是空的 GameObject，只需要设置正确的位置和旋转
- 建议根据原始模型的装备位置来设置定位锚点的位置

## 动画器配置

自定义模型 Prefab 需要包含 `Animator` 组件，并配置相应的 Animator Controller。

### Animator Controller 参数

Animator Controller 可以使用以下参数：

#### Bool 类型参数

- `Grounded`：角色是否在地面上
- `Die`：角色是否死亡
- `Moving`：角色是否正在移动
- `Running`：角色是否正在奔跑
- `Dashing`：角色是否正在冲刺
- `GunReady`：枪械是否准备就绪
- `Loaded`：枪械是否已装弹（当持有 `ItemAgent_Gun` 时，由 `OnLoadedEvent` 事件更新）
- `Reloading`：是否正在装弹
- `RightHandOut`：右手是否伸出
- `ActionRunning`：是否正在执行动作（由 `CharacterMainControl.CurrentAction` 决定）
- `Hidden`：角色是否处于隐藏状态
- `ThermalOn`：热成像是否开启
- `InAds`：是否正在瞄准（ADS - Aim Down Sights）
- `HideOriginalEquipment`：是否隐藏原有装备（由 `HideEquipmentConfig.json` 中对应 `ModelTarget` 的配置控制）
- `LeftHandEquip`：左手槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `RightHandEquip`：右手槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `ArmorEquip`：护甲槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `HelmetEquip`：头盔槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `HeadsetEquip`：耳机槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `FaceEquip`：面部槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `BackpackEquip`：背包槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `MeleeWeaponEquip`：近战武器槽位是否有装备（基于装备的 TypeID 判断，TypeID > 0 时为 `true`）
- `HavePopText`：是否有弹出文本（检测弹出文本槽位是否有子对象）

#### Float 类型参数

- `MoveSpeed`：移动速度比例（正常走 0~1，跑步可达 2）
- `MoveDirX`：移动方向 X 分量（-1.0 ~ 1.0，角色本地坐标系）
- `MoveDirY`：移动方向 Y 分量（-1.0 ~ 1.0，角色本地坐标系）
- `VelocityX`：速度 X 分量
- `VelocityY`：速度 Y 分量
- `VelocityZ`：速度 Z 分量
- `VelocityMagnitude`：速度大小（速度向量的长度）
- `AimDirX`：瞄准方向 X 分量
- `AimDirY`：瞄准方向 Y 分量
- `AimDirZ`：瞄准方向 Z 分量
- `AdsValue`：瞄准值（0.0 - 1.0，瞄准进度）
- `AmmoRate`：弹药比率（0.0 - 1.0，当前弹药数 / 最大弹药容量）
- `HealthRate`：生命值比率（0.0 - 1.0，当前生命值 / 最大生命值）
- `WaterRate`：水分比率（0.0 - 1.0，当前水分 / 最大水分）
- `WeightRate`：重量比率（当前总重量 / 最大负重，可能大于 1.0）
- `ActionProgress`：动作进度百分比（0.0 - 1.0，当前动作的进度，由 `IProgress.GetProgress().progress` 获取）

#### Int 类型参数

- `CurrentCharacterType`：当前角色类型
  - `0`：角色（Character）
  - `1`：宠物（Pet）
- `HandState`：手部状态
  - `0`：默认状态
  - `1`：正常（normal）
  - `2`：枪械（gun）
  - `3`：近战武器（meleeWeapon）
  - `4`：弓（bow）
  - `-1`：搬运状态
- `ShootMode`：射击模式（当持有 `ItemAgent_Gun` 时，由枪械的 `triggerMode` 决定）
  - `0`：自动（auto）
  - `1`：半自动（semi）
  - `2`：栓动（bolt）
- `AimType`：瞄准类型（由 `CharacterMainControl.AimType` 决定）
  - `0`：正常瞄准（normalAim）
  - `1`：角色技能（characterSkill）
  - `2`：手持技能（handheldSkill）
- `WeightState`：重量状态（仅在Raid地图中生效）
  - `0`：轻量（WeightRate ≤ 0.25）
  - `1`：正常（0.25 < WeightRate ≤ 0.75）
  - `2`：超重（0.75 < WeightRate ≤ 1.0）
  - `3`：过载（WeightRate > 1.0）
- `WeaponInLocator`：武器当前所在的定位点类型（无武器时为 `0`）
  - `0`：无武器
  - `1`：右手定位点（`normalHandheld`）
  - `2`：近战武器定位点（`meleeWeapon`）
  - `3`：左手定位点（`leftHandSocket`）
  - 当武器类型为左手但模型没有左手定位点时，会自动使用右手定位点（值为 `1`）
- `LeftHandTypeID`：左手装备的 TypeID（无装备时为 `0`）
- `RightHandTypeID`：右手装备的 TypeID（无装备时为 `0`）
- `ArmorTypeID`：护甲装备的 TypeID（无装备时为 `0`）
- `HelmetTypeID`：头盔装备的 TypeID（无装备时为 `0`）
- `HeadsetTypeID`：耳机装备的 TypeID（无装备时为 `0`）
- `FaceTypeID`：面部装备的 TypeID（无装备时为 `0`）
- `BackpackTypeID`：背包装备的 TypeID（无装备时为 `0`）
- `MeleeWeaponTypeID`：近战武器装备的 TypeID（无装备时为 `0`）
- `ActionPriority`：动作优先级（由 `CharacterMainControl.CurrentAction.ActionPriority()` 决定）
  - `0`：Whatever（任意）
  - `1`：Reload（装弹）
  - `2`：Attack（攻击）
  - `3`：usingItem（使用物品）
  - `4`：Dash（冲刺）
  - `5`：Skills（技能）
  - `6`：Fishing（钓鱼）
  - `7`：Interact（交互）
  - 当 `ActionRunning` 为 `true` 时，动作优先级可以近似用于判断角色正在执行什么动作

#### Trigger 类型参数

- `Attack`：攻击触发（用于触发近战攻击动画）
- `Shoot`：射击触发（当持有 `ItemAgent_Gun` 时，由 `OnShootEvent` 事件触发）

### 可选动画层

如果模型包含近战攻击动画，可以添加名为 `"MeleeAttack"` 的动画层：

- 层名称必须为 `"MeleeAttack"`
- 该层用于播放近战攻击动画
- 层的权重会根据攻击状态自动调整

### 动画器工作流程

1. 模组会自动读取游戏状态并更新 Animator 参数
2. 移动、跳跃、冲刺等状态会实时同步到自定义模型的 Animator
3. 攻击、装弹等动作会触发相应的动画参数
4. 如果存在 `MeleeAttack` 层，攻击时会自动调整该层的权重以播放攻击动画
5. 当角色持有 `ItemAgent_Gun` 时，会自动订阅枪械的 `OnShootEvent` 和 `OnLoadedEvent` 事件
   - `OnShootEvent`：触发时设置 `Shoot` 触发器
   - `OnLoadedEvent`：触发时更新 `Loaded` 布尔值
6. 当角色切换持有物品时（`OnHoldAgentChanged` 事件），会自动更新相关订阅

## 自定义音效

模组支持为自定义模型配置音效，包括玩家按键触发和 AI 自动触发两种方式。

### 音效配置

在 `bundleinfo.json` 的 `ModelInfo` 中可以配置音效：

```json
{
  "ModelID": "unique_model_id",
  "Name": "模型显示名称",
  "CustomSounds": [
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

#### SoundInfo 字段说明

- `Path`（必需）：音效文件路径，相对于模型包文件夹
- `Tags`（可选）：音效标签数组，用于指定音效的使用场景
  - `"normal"`：普通音效，用于玩家按键触发和 AI 普通状态
  - `"surprise"`：惊讶音效，用于 AI 惊讶状态
  - `"death"`：死亡音效，用于 AI 死亡状态
  - `"idle"`：待机音效，用于角色自动播放（可通过配置控制哪些角色类型允许自动播放）
  - 可以同时包含多个标签，表示该音效可用于多个场景
  - 未指定标签时，默认为 `["normal"]`


### 音效触发方式

#### 玩家按键触发

- 当角色模型配置了音效时，玩家按下游戏中的 `Quack` 键会触发音效
- 只会播放标签为 `"normal"` 的音效
- 从所有 `"normal"` 标签的音效中随机选择一个播放
- 只有玩家角色会响应按键，宠物不会触发
- 播放音效时会同时创建 AI 声音，使其他 AI 能够听到玩家发出的声音

#### AI 自动触发

- AI 会根据游戏状态自动触发相应标签的音效
- `"normal"`：AI 普通状态时触发
- `"surprise"`：AI 惊讶状态时触发
- `"death"`：AI 死亡状态时触发
- `"idle"`：启用了自动播放的角色会在随机间隔时间自动播放待机音效
  - 播放间隔可在 `IdleAudioConfig.json` 中配置
  - 默认间隔为 30-45 秒（随机）
  - 角色死亡时不会播放
  - 哪些角色类型允许自动播放可通过 `EnableIdleAudio` 和 `AICharacterEnableIdleAudio` 配置控制
  - 默认情况下，AI 角色和宠物允许自动播放，玩家角色不允许（可通过配置启用）
- 如果指定标签的音效不存在，将使用原版事件（不会回退到其他标签）

### 音效文件要求

- 音效文件应放置在模型包文件夹内
- 支持游戏使用的音频格式（通常为 WAV、OGG 等）
- 音效文件路径在 `Path` 中指定，相对于模型包文件夹
- 例如：如果模型包文件夹为 `MyModel/`，音效文件为 `MyModel/sounds/voice.wav`，则 `Path` 应设置为 `"sounds/voice.wav"`

### 注意事项

- 如果模型没有配置音效，不会影响其他功能

## AI 角色适配

模组支持为游戏中的 AI 角色配置自定义模型，可以为不同的 AI 角色配置不同的模型。

### 配置 AI 角色模型

#### 在 bundleinfo.json 中配置

在模型的 `bundleinfo.json` 中，需要：

1. 在 `Target` 数组中包含 `"AICharacter"`，表示该模型适用于 AI 角色
2. 在 `SupportedAICharacters` 数组中指定支持的 AI 角色名称键

示例：

```json
{
  "ModelID": "ai_model_id",
  "Name": "AI 模型",
  "Target": ["AICharacter"],
  "SupportedAICharacters": ["Cname_Wolf", "Cname_Scav", "*"]
}
```

- 如果 `SupportedAICharacters` 包含 `"*"`，表示该模型适用于所有 AI 角色
- 如果 `SupportedAICharacters` 包含具体的 AI 角色名称键，表示该模型仅适用于这些 AI 角色
- 如果 `SupportedAICharacters` 为空数组，则该模型不会应用于任何 AI 角色

#### 在 UsingModel.json 中配置

在 `UsingModel.json` 中，可以为每个 AI 角色单独配置模型：

```json
{
  "AICharacterModelIDs": {
    "Cname_Wolf": "wolf_model_id",
    "Cname_Scav": "scav_model_id",
    "*": "default_ai_model_id"
  }
}
```

配置优先级：

1. 首先检查该 AI 角色是否有单独配置的模型
2. 如果没有，则检查 `"*"` 对应的默认模型
3. 如果都没有，则使用原始模型

#### 查找 AI 角色名称键

AI 单位目标的 key（如 `"Cname_Wolf"`、`"Cname_Scav"`）可以从游戏本地化文件中查找：

- 文件位置：`游戏安装目录/Duckov_Data/StreamingAssets/Localization` 目录下的 CSV 文件
- 查找方法：打开任意语言的 CSV 文件（如 `ChineseSimplified.csv`），找到 `Characters` 这个 sheet（工作表），其中的 key 列就是 AI 角色名称键
- 这些 key 可以用于 `SupportedAICharacters` 数组和 `AICharacterModelIDs` 字典中

### 使用模型选择界面

1. 打开模型选择界面（默认按键：`\`）
2. 在目标类型下拉菜单中选择"AI角色"
3. 选择要配置的 AI 角色（或选择"所有 AI 角色"来设置默认模型）
4. 浏览并选择要应用的模型
5. 配置会自动保存到 `UsingModel.json`

### 隐藏 AI 角色装备

可以为每个 AI 角色单独配置是否隐藏原有装备：

- 在模型选择界面中，选择"AI角色"目标类型
- 选择具体的 AI 角色
- 在模型列表页面顶部会显示针对该 AI 角色的"隐藏装备"切换选项
- 每个 AI 角色都有独立的隐藏装备设置
- 配置会自动保存到 `HideEquipmentConfig.json`

### 注意事项

- AI 角色模型需要满足与角色模型相同的要求（定位锚点、Animator 配置等）
- 模型必须在其 `bundleinfo.json` 中明确声明支持 AI 角色（`Target` 包含 `"AICharacter"`）
- 模型必须在其 `SupportedAICharacters` 中声明支持该 AI 角色，或包含 `"*"` 表示支持所有 AI 角色
- 如果模型没有正确配置，AI 角色将使用原始模型



