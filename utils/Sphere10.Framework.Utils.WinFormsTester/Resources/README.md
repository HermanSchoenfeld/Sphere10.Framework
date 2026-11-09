# Resources

Images and sounds embedded through `Properties/Resources.resx` for the tester's block buttons, menus, toolbars and sound demonstrations.

Code reads these files through the generated `Resources` class, for example `Resources.Generic16x16` or `Resources.LeftClickDown`. The plugins in the project folder pass a `Clone()` of each image to the menu and block builders, because a native definition disposes the images it owns. `DraggableControlsTestScreen` shows `LargeSearchIcon`, and `TestSoundsScreen` plays the click sounds. Both screens are in [Screens](../Screens/README.md).

## Contents

This folder declares no types. It holds these resource files.

| File | Resource name | Purpose |
|---|---|---|
| [`Database_16x16.png`](Database_16x16.png) | `Database16x16` | Menu image for the database, Visual Grid and SourceGrid items. |
| [`Email_16x16.png`](Email_16x16.png) | `Email16x16` | Menu image for the Email item. |
| [`Generic_16x16.png`](Generic_16x16.png) | `Generic16x16` | Default menu and toolbar image for most items. |
| [`LargeSearchIcon.png`](LargeSearchIcon.png) | `LargeSearchIcon` | Picture shown in the draggable picture boxes. |
| [`LeftClickDown.wav`](LeftClickDown.wav) | `LeftClickDown` | Left mouse button press sound. |
| [`LeftClickUp.wav`](LeftClickUp.wav) | `LeftClickUp` | Left mouse button release sound. |
| [`Menu_32x32.png`](Menu_32x32.png) | `Menu32x32` | Block and menu heading image. |
| [`Network_16x16.png`](Network_16x16.png) | `Network16x16` | Menu image for the WebSockets item. |
| [`ObjectSpace_16x16.png`](ObjectSpace_16x16.png) | `ObjectSpace16x16` | Menu image for the ObjectSpace item. |
| [`RightClickDown.wav`](RightClickDown.wav) | `RightClickDown` | Right mouse button press sound. |
| [`RightClickUp.wav`](RightClickUp.wav) | `RightClickUp` | Right mouse button release sound. |
| [`Settings_16x16.png`](Settings_16x16.png) | `Settings16x16` | Menu image for the settings and application services items. |
| [`Test_16x16.png`](Test_16x16.png) | `Test16x16` | Menu image for the WAMS-8 Signatures item. |
| [`TestBlock_32x32.png`](TestBlock_32x32.png) | `TestBlock32x32` | Large image of the Workspace and Integration blocks. |
| [`TestBlock_8x8.png`](TestBlock_8x8.png) | `TestBlock8x8` | Small image of the Workspace block. |
| [`Tests_32x32.png`](Tests_32x32.png) | `Tests32x32` | Block and menu heading image. |
| [`Tests2_32x32.png`](Tests2_32x32.png) | `Tests232x32` | Block and menu heading image. |
| [`Tree_16x16.png`](Tree_16x16.png) | `Tree16x16` | Menu image for the Merkle Tree item. |
| [`Wizard_16x16.png`](Wizard_16x16.png) | `Wizard16x16` | Menu image for the Demo Wizard item. |
| [`Wizard_32x32.png`](Wizard_32x32.png) | `Wizard32x32` | Heading image of the Wizard menu. |
