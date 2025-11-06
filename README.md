# Duckov Custom Model

[English](README_EN.md) | 中文

一个用于 Duckov 游戏的自定义玩家模型模组。

## 基本功能

- **自定义模型替换**：允许玩家使用自定义模型替换游戏中的角色模型
- **模型选择界面**：提供图形化界面，方便玩家浏览和选择可用模型
- **模型搜索**：支持通过模型名称、ID 等关键词搜索模型
- **模型管理**：自动扫描并加载模型包，支持多个模型包同时存在
- **快速切换**：支持在游戏中快速切换模型，无需重启游戏

## 配置文件

配置文件位于：`游戏安装路径/ModConfigs/DuckovCustomModel`

### UIConfig.json

UI 界面相关配置。

```json
{
  "ToggleKey": "Backslash",
  "HideOriginalEquipment": false
}
```

- `ToggleKey`：打开/关闭模型选择界面的按键（默认：`Backslash`，即反斜杠键 `\`）
  - 支持的按键值可参考 Unity KeyCode 枚举
- `HideOriginalEquipment`：是否隐藏原有装备（默认：`false`）
  - 设置为 `true` 时，Animator 的 `HideOriginalEquipment` 参数会被设置为 `true`
  - 可在模型选择界面的设置区域中切换此选项

### UsingModel.json

当前使用的模型配置。

```json
{
  "ModelID": ""
}
```

- `ModelID`：当前使用的模型 ID（字符串，为空时使用原始模型）
  - 设置后，游戏会在关卡加载时自动应用该模型
  - 可通过模型选择界面修改，修改后会自动保存到此文件

## 模型选择界面

模型选择界面提供了以下功能：

- **模型浏览**：滚动查看所有可用的模型
- **模型搜索**：通过模型名称、ID 等关键词快速搜索模型
- **模型选择**：点击模型按钮即可应用该模型
- **设置选项**：在界面底部可以切换"隐藏原有装备"选项
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
└── thumbnail.png            # 缩略图文件（可选，也可放在 AssetBundle 内）
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
      "PrefabPath": "Assets/Model.prefab"
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
- `ThumbnailPath`（可选）：缩略图路径
  - 可以是 AssetBundle 内的资源路径（如 `"Assets/Thumbnail.png"`）
  - 也可以是模型包文件夹下的外部文件路径（如 `"thumbnail.png"`）
- `PrefabPath`（必需）：模型 Prefab 在 AssetBundle 内的资源路径（如 `"Assets/Model.prefab"`）

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
- `Reloading`：是否正在装弹
- `RightHandOut`：右手是否伸出
- `HideOriginalEquipment`：是否隐藏原有装备（由配置项 `HideOriginalEquipment` 控制）
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
- `HealthRate`：生命值比率（0.0 - 1.0，当前生命值 / 最大生命值）
- `WaterRate`：水分比率（0.0 - 1.0，当前水分 / 最大水分）
- `WeightRate`：重量比率（当前总重量 / 最大负重，可能大于 1.0）

#### Int 类型参数

- `HandState`：手部状态
  - `0`：默认状态
  - `1`：正常（normal）
  - `2`：枪械（gun）
  - `3`：近战武器（meleeWeapon）
  - `4`：弓（bow）
  - `-1`：搬运状态
- `WeightState`：重量状态（仅在Raid地图中生效）
  - `0`：轻量（WeightRate ≤ 0.25）
  - `1`：正常（0.25 < WeightRate ≤ 0.75）
  - `2`：超重（0.75 < WeightRate ≤ 1.0）
  - `3`：过载（WeightRate > 1.0）
- `LeftHandTypeID`：左手装备的 TypeID（无装备时为 `0`）
- `RightHandTypeID`：右手装备的 TypeID（无装备时为 `0`）
- `ArmorTypeID`：护甲装备的 TypeID（无装备时为 `0`）
- `HelmetTypeID`：头盔装备的 TypeID（无装备时为 `0`）
- `HeadsetTypeID`：耳机装备的 TypeID（无装备时为 `0`）
- `FaceTypeID`：面部装备的 TypeID（无装备时为 `0`）
- `BackpackTypeID`：背包装备的 TypeID（无装备时为 `0`）
- `MeleeWeaponTypeID`：近战武器装备的 TypeID（无装备时为 `0`）

#### Trigger 类型参数

- `Attack`：攻击触发（用于触发近战攻击动画）

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



