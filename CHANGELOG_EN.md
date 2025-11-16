# Changelog

English | [中文](CHANGELOG.md)

## v1.8.0-fix1

- Fixed issue where multilingual loading failed due to changes in loading method

## v1.8.0

- Split some logic into different assemblies to facilitate future SDK development
- Adjusted loading logic, added additional Mod loading wrapper to reduce issues caused by Harmony loading order
- Under normal working conditions, it should no longer be necessary to consider whether this mod is loaded before Harmony

## v1.7.12

- Added sound tag `trigger_on_death` for automatically playing sound effects when a character dies
- Fixed issue where health bar height was not refreshed when switching models

## v1.7.11

- Added footstep sound frequency configuration feature, supports configuring walk and run footstep trigger frequency per model in `bundleinfo.json`
- Added `WalkSoundFrequency` field (optional), used to configure footstep trigger frequency per second when walking (if not specified, will automatically use the original character's walk footstep frequency setting)
- Added `RunSoundFrequency` field (optional), used to configure footstep trigger frequency per second when running (if not specified, will automatically use the original character's run footstep frequency setting)

## v1.7.10

- Added target switching feature to animator parameters window, can switch between viewing character's own or pet's animator parameters within the window
- Default view is character's own, can switch using two side-by-side buttons (Character/Pet)
- Automatically clears parameter state when switching targets, recalculating parameter changes from scratch

## v1.7.9

- Fixed issue where ModConfigs directory could not be created in read-only environments (such as macOS), causing functionality to fail
- When the installation directory is read-only, automatically switches to ModConfigs in the parent directory of the game save directory (Windows: `AppData\LocalLow\TeamSoda\Duckov\ModConfigs`, macOS/Linux: corresponding user data directory)

## v1.7.8-fix2

- Improved Harmony Patch application and removal mechanism, added fallback methods to improve fault tolerance, automatically tries fallback method when primary method fails

## v1.7.8

- Added animator parameters window hotkey feature, can configure a hotkey in the settings interface to toggle the animator parameters window
- Default value is no key, users need to actively set it in the settings interface
- Fixed the display determination logic for custom models; models now strictly follow the game's native view range logic for display switching

## v1.7.7

- Added "All AI Characters" target settings feature for uniformly setting default configuration items for all AI characters
- Optimized model lookup logic: prioritize individual settings, if not found then look for `*` settings, finally use original model
- Optimized some collision detection calculation logic when using custom models
- Fixed issue where headshot detection could not be triggered when character equipment was hidden
- Fixed issue where some slots would become invisible when switching models
- Fixed fallback logic for AI character configurations; audio, equipment hiding, and other settings now correctly select configuration based on the actually used model: if using fallback model (`*`), use `*` settings; if using own model configuration, use own settings

## v1.7.6

- Fixed the logic for obtaining the original model of character units; should now correctly match and process the original models for various character types
- Added occlusion display handling for character models; character models should now correctly display outlines when occluded
- Fixed issue where bundleinfo.json parsing errors would cause subsequent logic to be lost
- Now when a bundle's JSON file has format errors, it will log the error and skip that bundle, continuing to process other normal bundles
- Enhanced error handling during model loading to ensure errors in a single bundle do not affect overall functionality
- Fixed issue where error messages (such as loading errors, missing prefabs, etc.) were not correctly displayed in the model list interface
- Error messages are now correctly displayed in model list items, not just highlighted in red

## v1.7.5

- Added model audio toggle feature, can configure whether to use model-provided audio in target settings
- When disabled, all model audio (including key press triggers, AI automatic triggers, and idle audio) for the corresponding character will not play
- Supports separate configuration for characters, pets, and AI characters

## v1.7.4

- Optimized interface structure and logic to ensure proper display across all languages
- Added button to open model folder

## v1.7.3

- Added `Weather` Animator parameter (int type) to get current weather state (Sunny, Cloudy, Rainy, Stormy_I, Stormy_II)
- Added `Time` Animator parameter (float type) to get current 24-hour time
- Added `TimePhase` Animator parameter (int type) to get current time phase (day, dawn, night)
- When `TimeOfDayController.Instance` is unavailable, these three parameters will be set to -1

## v1.7.2

- Added InitialValue property to AnimatorParamInfo for storing parameter initial values
- Fixed initial value of ShootMode parameter (changed from 0 to -1)
- Enhanced animator parameter window display with parameter value change detection and color highlighting
  - Changed parameter values are displayed in yellow
  - Currently changing parameter values are displayed in orange
  - Unchanged parameters are displayed in white
- Adjusted animator parameter window height (increased from 800 to 1000) and font size (increased from 13 to 16)

## v1.7.1

- Modified animator parameter update logic to allow viewing animator parameter updates even when not using custom models (parameters are not applied to the animator)
- Changed CustomAnimatorControl to a persistent instance attached to the same object as ModelHandler, only changing the target Animator, avoiding frequent component creation and destruction
- Fixed issue where animator parameter window would not display when not using custom models

## v1.7.0

- Added a fixed settings button in the top-left corner of the screen that can be clicked to open/close the settings interface
- The settings button automatically appears when in the main menu or inventory interface

## v1.6.8

- Now automatically sets the layer of objects containing Renderer in the model to "Character"

## v1.6.7

- Fixed issue where hand-held equipment related parameters (such as `LeftHandEquip`, `RightHandEquip`, etc.) would not update correctly in certain situations
- Fixed issue where some Animator parameters (such as `GunState`, `WeaponInLocator`) would not display in the parameter window; now all defined parameters will display correctly

## v1.6.6

- Added `GunState` Animator parameter to indicate the current state of the gun (shoot cooling, ready, fire, burst each shot cooling, empty, reloading)

## v1.6.5

- Fixed the working logic of paper box locator and carriable locator

## v1.6.4

- Added `WeaponInLocator` Animator parameter to indicate the current locator type where the weapon is placed (left hand, right hand, or melee weapon)

## v1.6.3

- Added `DeathLootBoxPrefabPath` field to models for specifying death loot box style (optional, defaults to original model)
- Death loot box models can now be customized

## v1.6.2

- Removed idle audio feature restrictions for player characters; players can now enable automatic audio
- Added idle audio feature settings for each target character; each target can now be individually configured for automatic audio and audio intervals
- Refactored the model manager UI interface to improve user comfort, aesthetics, and structure

## v1.6.1

- Added idle audio automatic playback feature, supporting configuration of idle audio playback intervals for non-player characters (AI characters and pets)
- Added idle sound tag support; model sounds can be configured with "idle" tag for idle audio playback

## v1.6.0

- Optimized some logic processing
- Added paper box locator support; custom models can include `PaperBoxLocator` locator point, paper boxes will automatically attach to this locator and follow the model
- Hiding equipment no longer causes paper boxes to be hidden
- Added carriable item locator support; custom models can include `CarriableLocator` locator point, carriable items will automatically attach and save original position information when carried

## v1.5.1-fix2

- Fixed issue where audio settings were not correctly applied when reloading models

## v1.5.1-fix1

- Fixed issue where interface did not refresh when selecting models

## v1.5.1

- Added `AICharacter` target for AI characters that are not players or pets (NPCs are not supported as they are actually part of buildings, not conventional model structures)
- Added one-click reset invalid model settings button to quickly reset characters using incompatible or non-existent models back to default model
- Added audio playback related interfaces, supporting replacement of player quacks and AI automatic quacks

## v1.4.1

- Added extensive Animator parameter support to enhance animation control capabilities
- Added firearm-related parameters: `Loaded` (reload state), `Shoot` (shoot trigger), `ShootMode` (shoot mode), `AmmoRate` (ammo ratio)
- Added aiming-related parameters: `InAds` (aiming state), `AdsValue` (aiming progress), `AimType` (aiming type), `AimDirX/Y/Z` (aiming direction)
- Added action-related parameters: `ActionRunning` (action execution state), `ActionProgress` (action progress), `ActionPriority` (action priority)
- Added state-related parameters: `Hidden` (hidden state), `ThermalOn` (thermal imaging state), `VelocityX/Y/Z` (velocity components)
- Optimized some code logic and performance
- Added Animator parameter window to view supported Animator parameter values in real-time

## v1.2.0

- Refactored configuration system:
  - `UsingModel.json` changed to dictionary format
  - Added `HideEquipmentConfig.json` and removed related configurations from `UIConfig.json`
  - Supports automatic migration of old configurations
- Performance optimization: Implemented incremental update mechanism using runtime hash caching, only reloading when Bundle files or configurations change
- Multi-object support: Each ModelTarget can correspond to multiple game objects, uniformly applying changes to all objects when switching models
- Refresh process optimization: Skip Bundle loading and state checks when there are no changes, avoiding unnecessary repeated loading
- Temporary model restoration: Automatically temporarily switch back to original model when Bundle updates, automatically restore after update completes
- User selection protection: User model changes during refresh process will not be overwritten
- UI improvements: Model information displays Bundle name
- Fixed issue where models were loaded multiple times during refresh

## v1.1.2

- Optimized some logic
- Added `CurrentCharacterType` parameter to determine whether it applies to player character (0) or pet (1)

