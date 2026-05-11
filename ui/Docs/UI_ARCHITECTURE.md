# Beavering Away UI Architecture

这版 UI 的原则：Godot 场景里摆节点，C# 脚本负责绑定、更新数据和少量运行时列表。

当前布局规划已经按这几个决定调整：

- 左上角任务追踪器默认收纳，只显示当前追踪任务；点击按钮展开任务列表。
- 每个任务行有一个 `Track` 按钮。
- `Inventory/InventoryWindow.cs` 继续负责 Tab 打开的合成区；背包显示和拖动改由新 HUD `InventoryBar` 承担。
- `WorldStatusPanel` 只保留两个进度条：土地恢复度、动物朋友回归度。
- 背包栏上方加 `HealthBar`，做成类似 Minecraft 的心形生命条。
- `InteractionPrompt` 设计为跟随玩家头顶，不再固定放在屏幕底部。

## 主入口

- `ui/Core/UIRoot.cs`
  - 挂在主场景里的 `CanvasLayer`，建议节点名叫 `GameUI`。
  - 默认不会自动生成 HUD。
  - `AutoFindSceneNodes` 开着时，它会按节点名找下面这些节点：
	- `ObjectiveTracker`
	- `WorldStatusPanel`
	- `HealthBar`
	- `InventoryBar`
	- `InteractionPrompt`
	- `ToastFeed`
	- `DialogueBox`
	- `PauseDim`
	- `PauseMenu`

- `ui/Core/UIEventBus.cs`
  - 玩法代码向 UI 发消息的入口。

- `ui/Core/LegacyInventoryAdapter.cs`
  - 临时桥接现在的旧单格 `InventoryData`。如果不想让旧背包数据显示到新 HUD，可以在场景里禁用或移除这个节点。

## 推荐节点结构

```text
GameUI CanvasLayer                         script: ui/Core/UIRoot.cs
  HudLayer Control
	ObjectiveTracker PanelContainer        script: ui/Widgets/ObjectiveTracker.cs
	  HeaderRow HBoxContainer
		HeaderLabel Label
		ToggleButton Button
	  CurrentTitleLabel Label
	  CurrentDetailLabel Label
	  CurrentProgressBar ProgressBar
	  ExpandedPanel VBoxContainer
		Rows VBoxContainer

	WorldStatusPanel PanelContainer        script: ui/Widgets/WorldStatusPanel.cs
	  HeaderLabel Label                    text: World
	  RestoredLabel Label                  text: Land
	  RestoredBar ProgressBar
	  AnimalsLabel Label                   text: Friends
	  AnimalBar ProgressBar

	ToastFeed VBoxContainer                script: ui/Widgets/ToastFeed.cs

	InteractionPrompt PanelContainer       script: ui/Widgets/InteractionPrompt.cs
	  PromptContent HBoxContainer
		KeyLabel Label                     text: E
		PromptLabel Label

	HealthBar Control                      script: ui/Widgets/HealthBar.cs
	  Hearts HBoxContainer
		Heart1 Label or TextureRect
		Heart2 Label or TextureRect
		Heart3 Label or TextureRect

	InventoryBar PanelContainer            script: ui/Widgets/InventoryBar.cs
	  Slots HBoxContainer or GridContainer
		Slot1 PanelContainer               script: ui/Widgets/InventorySlotView.cs
		  IconRect TextureRect
		  CountLabel Label
		  NameLabel Label
		  EmptySwatch ColorRect
		Slot2 PanelContainer               script: ui/Widgets/InventorySlotView.cs
		Slot3 PanelContainer               script: ui/Widgets/InventorySlotView.cs

  ModalLayer Control
	PauseDim ColorRect

	DialogueBox PanelContainer             script: ui/Widgets/DialogueBox.cs
	  PortraitRect TextureRect
	  SpeakerLabel Label
	  BodyLabel Label
	  ContinueIndicator Label

	PauseMenu PanelContainer               script: ui/Screens/PauseMenu.cs
	  TitleLabel Label
	  ResumeButton Button
	  SettingsButton Button
	  QuitButton Button

  LegacyInventoryAdapter Node              script: ui/Core/LegacyInventoryAdapter.cs
```

## 摆放建议

- `ObjectiveTracker`
  - 左上角。
  - 收纳高度约 90-120 px，展开后可以到 260-330 px。

- `WorldStatusPanel`
  - 右上角。
  - 宽度 160-220 px 就够，两条进度条，不放大段文字。

- `ToastFeed`
  - 顶部中间。

- `InteractionPrompt`
  - 可以放在场景任意位置作为模板；运行时会跟随玩家头顶。
  - `Interaction/interacting_component.gd` 已经改成调用 `UIEventBus.ShowInteractionPromptForNode(...)`。
  - `KeyLabel` 和 `PromptLabel` 要放进 `PromptContent HBoxContainer`，不然两个 Label 会重叠。
  - 默认 `ApplyDefaultStyle` 是关闭的，你可以直接在 Godot 里改样式；只有勾上它时脚本才会套默认样式。

- `HealthBar`
  - 底部居中，`InventoryBar` 上方。
  - 可以先用 Label 心形，也可以用 `ui/Resources/Assets/Raw/Symbol/Hearts.png` 切图后换成 TextureRect。

- `InventoryBar`
  - 底部居中。
  - 可以直接绑定 `InventoryDataNew`，HUD 槽位和合成台都使用 `ItemData` 资源。
  - 如果你想换 Inventory 底图，保持 `ApplyDefaultStyle` 关闭，然后在 `InventoryBar` 的 `Theme Overrides > Styles > Panel` 里放自己的 `StyleBoxTexture`。

- `InventoryWindow`
  - 已存在，按 Tab 打开合成台。
  - 旧背包面板可以删掉；脚本现在会安全跳过不存在的 `Inventory/SlotGroup`。

## Widget 绑定规则

每个 Widget 有两种绑定方式：

1. 节点名匹配：按上面的名字建子节点，脚本会自动找。
2. Inspector 手动拖引用：比如把 `RestoredBar` 拖到 `WorldStatusPanel` 脚本的 `RestoredBar` 字段。

每个 Widget 还有两个常用开关：

- `GenerateChildren`
  - 临时预览用。打开后脚本会自动生成子节点。
  - 正式 UI 建议关掉。

- `ApplyDefaultStyle`
  - 打开后脚本会套我写的默认像素风样式。
  - 如果你想完全用 Godot theme/图片资源控制外观，可以关掉。

## 玩法代码以后怎么推 UI

```csharp
UIEventBus.SetWorldStatus(new WorldStatusViewData
{
	RestoredTiles = 16,
	TotalRecoverableTiles = 80,
	AnimalFriends = 1,
	TotalAnimalFriends = 8
});
```

```csharp
UIEventBus.SetObjectives(new []
{
	new ObjectiveViewData("restore_grass", "Restore dry grass", "Bring water back to the forest floor", 16, 80)
	{
		IsTracked = true
	},
	new ObjectiveViewData("invite_rabbit", "Bring back Rabbit", "Grow enough good grass", 0, 1)
});
```

```csharp
UIEventBus.SetPlayerHealth(16, 20);
```

```csharp
UIEventBus.ShowInteractionPromptForNode("Chop tree", playerNode);
UIEventBus.HideInteractionPrompt();
```

```csharp
UIEventBus.ShowDialogue(new DialogueViewData("Rabbit", "This grass is starting to look snackable."));
```

```csharp
UIEventBus.ShowToast("Fox stole a log", ToastKind.Warning);
```

## InventoryBar 说明

`InventoryBar` 现在是 HUD 快捷栏显示层。推荐把它的 `Inventory Data` 指到一个 `InventoryDataNew` 资源，比如 `res://Inventory/TestInventory.tres`，这样 HUD 槽位里的物品可以直接拖进 `InventoryWindow` 的 Crafting 格子。

如果 `Inventory Data` 已经绑定，`InventoryBar` 默认不会接受 `UIEventBus.SetInventory(...)` 的外部覆盖，避免旧单格背包桥接把新 UI 的 5 个槽冲乱。只有你明确需要旧数据推过来时，才打开 `Accept Event Bus Inventory`。

- `Slots`
  - 指向你的槽位容器，推荐用 `HBoxContainer` 或 `GridContainer`。
  - 里面每个槽位用 `InventorySlotView`。

推荐结构：

```text
InventoryBar PanelContainer                script: ui/Widgets/InventoryBar.cs
  MarginContainer
	Slots HBoxContainer or GridContainer
	  Slot1 PanelContainer                 script: ui/Widgets/InventorySlotView.cs
		SlotContent Control
		  IconRect TextureRect
		  CountLabel Label
		  NameLabel Label
	  Slot2 PanelContainer                 script: ui/Widgets/InventorySlotView.cs
		SlotContent Control
		  IconRect TextureRect
		  CountLabel Label
	  Slot3 PanelContainer                 script: ui/Widgets/InventorySlotView.cs
	  Slot4 PanelContainer                 script: ui/Widgets/InventorySlotView.cs
	  Slot5 PanelContainer                 script: ui/Widgets/InventorySlotView.cs
```

如果要 5 个槽，推荐先做好 `Slot1`，把脚本、底图、`IconRect`、`CountLabel` 调好，然后复制 4 份。每个 `Slot` 都必须挂 `InventorySlotView.cs`，因为 `InventoryBar` 只会识别这种槽位。

`InventoryBar` 的拖动逻辑按场景里实际存在的 `InventorySlotView` 数量工作；你放 5 个 Slot，它就只处理 5 个。`SlotCount` 只在 `GenerateChildren` 打开时用于自动生成临时槽位。

拖动物品时会把整个格子的堆叠一起拿走，类似《饥荒》的整组拖动。按住 Ctrl 再拖时只会拆出 1 个物品；Mac 上 Command 也算拆分键。放到同类槽会堆叠，放到空槽会移动，普通拖到已有不同物品的槽会交换，拆分拖到已有不同物品的槽会回到原位。

每个 `InventorySlotView` 默认只显示物品图标；`Show Item Name` 和 `Show Single Item Count` 默认关闭。小背包格里不要开物品名，否则 `NameLabel` 会挤在图标上。

如果 Tab 打开的 `InventoryWindow` 可见，把 HUD 背包里的物品拖到 `CraftingSlotGroup` 的空格子上，会直接写入 `CraftingData` 并刷新结果格。Crafting 格子已经有物品时不会覆盖，HUD 物品会回到原位。

Crafting 格子也支持同样的拖动规则：普通拖整组，Ctrl 拖 1 个；可以在 Crafting 格子之间移动/堆叠，也可以拖回 HUD 背包。结果格拖到 HUD 背包成功后，会从每个参与合成的输入格里消耗 1 个物品。

槽位底图建议放在每个 `Slot` 自己的 `Theme Overrides > Styles > Panel` 里，而不是命名成 `EmptySwatch`。`EmptySwatch` 会在有物品时被脚本隐藏；如果你希望底图永远存在，就用 `PanelContainer` 的 Panel 样式当底图。

物品图标是浮层：

```text
Slot1 PanelContainer                        script: ui/Widgets/InventorySlotView.cs
  SlotContent Control
	IconRect TextureRect                    物品图标，Anchor Full Rect
	CountLabel Label                        数量，放右下角
```

`IconRect` 会由脚本自动设置成当前物品的 `ItemData.Icon`。空槽时它会清空；有物品时它显示在底图上方。

改底图：

1. 选中 `InventoryBar`。
2. 确认 `ApplyDefaultStyle` 关闭。
3. 在 `Theme Overrides > Styles > Panel` 新建或放入 `StyleBoxTexture`。
4. 如果是图集，先在 `StyleBoxTexture > Sub-Region` 里截出底图区域。
5. 背景需要拉伸时，调 `Axis Stretch` 和 `Texture Margins`。
