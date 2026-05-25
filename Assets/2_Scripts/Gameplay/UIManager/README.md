# UI 管理模块（UIManager）

基于 YGZFrameWork 的面板栈管理系统，配合 CanvasManager 的层级系统。

## 文件清单

| 文件 | 职责 |
|------|------|
| UIManager.cs | 面板栈管理、配置驱动、Open/Close/ClearAll |
| BasePanel.cs | 面板基类，带生命周期和动画接口 |
| PanelConfigData.cs | ScriptableObject 配置，定义每个面板的预制体和层级 |
| UIMessages.cs | 内部消息常量（EventDispatcher int-based） |
| UICommands.cs | Facade 命令模式，将字符串消息映射到操作 |
| PanelConfig.json | 5 个示例配置（主菜单/设置/背包/角色属性/投骰界面） |

## 核心能力

- **OpenPanel(panelId)** — 按配置加载预制体到对应 Canvas 层
- **CloseTopPanel()** — 返回导航，关闭栈顶
- **独占面板** — 打开时自动关闭同层级其他面板
- **缓存复用** — 关闭后隐藏，不重复 Instantiate
- **生命周期** — OnOpen / OnClose / OnPause / OnResume / OnBackButton

## 集成步骤

1. 在 StartUpCommand.cs 中注册 UIManager：
   ```csharp
   AppFacade.Instance.AddManager<UIManager>(ManagerName.UI);
   ```
2. 创建 ScriptableObject 配置（右键菜单 YGZFrameWork/PanelConfig）
3. 在 UIManager.InitDataM() 中加载配置表
4. 从按钮或代码调用：
   ```csharp
   UIManager.Instance.OpenPanel("MainMenu");
   UIManager.Instance.CloseTopPanel();
   ```

## 与 CanvasManager 的配合

- CanvasManager 管理 Canvas 层级（Background/Main/Popup/Top）
- UIManager 管理面板实例（挂载到对应层级下）
- 两者通过 AppFacade 的消息总线解耦
