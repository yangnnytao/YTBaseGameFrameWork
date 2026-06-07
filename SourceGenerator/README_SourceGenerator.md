# YGZFrameWork CfgTool Source Generator 使用说明

## 概述

编译时自动扫描所有 `CfgToolBase<TKey, TData>` 子类，生成 `CfgToolRegistry.TouchAllInstances()`，
彻底消除 `CfgToolManager.LoadAll()` 的硬编码问题。

**核心价值**：新增配置表 = 只新建一个类文件，**零改任何现有代码**。

## 微信小游戏 / IL2CPP 兼容性

- ✅ **零反射**：编译时生成显式代码，运行时无 `GetTypes()` / `Activator.CreateInstance()`
- ✅ **零裁剪风险**：IL2CPP 看到显式类型引用，不会裁剪配置表类
- ✅ **零包体增加**：无反射元数据，不增加 wasm 包体
- ✅ **零启动损耗**：无运行时扫描，启动速度最优

## 目录结构

```
YTBaseGameFrameWork/
├── SourceGenerator/                          ← Source Generator 项目（在 Unity 外编译）
│   ├── YGZFrameWork.CfgTool.SourceGenerator.csproj
│   ├── CfgToolSourceGenerator.cs
│   └── build.bat
│
└── Assets/
    └── 2_Scripts/
        └── Framework/
            └── ConfigTool/
                ├── Editor/                     ← Source Generator dll 放置处
                │   └── YGZFrameWork.CfgTool.SourceGenerator.dll
                │
                ├── Base/
                │   └── CfgToolBase.cs          ← 基类（已存在）
                ├── CfgData/
                │   └── HeroBaseCfgData.cs    ← 示例配置表（已修改）
                └── Manager/
                    └── CfgToolManager.cs       ← 管理器（已修改）
```

## 编译步骤

### 1. 安装 .NET SDK

确保已安装 .NET SDK 5.0 或更高版本：

```bash
dotnet --version
```

未安装请前往：https://dotnet.microsoft.com/download

### 2. 编译 Source Generator

双击运行 `SourceGenerator/build.bat`，或命令行执行：

```bash
cd SourceGenerator
dotnet build -c Release
```

### 3. 集成到 Unity

在 `Assets/` 目录下创建 `csc.rsp`，添加内容：

```
-analyzer:Assets/2_Scripts/Framework/ConfigTool/Editor/YGZFrameWork.CfgTool.SourceGenerator.dll
```

重启 Unity 即可生效。

> 注：不需要 asmdef。你的项目目前脚本数量不多，统一在 `Assembly-CSharp` 里即可避免 `Singleton` 等基类的跨程序集引用问题。

## 新增配置表流程

**以前**（硬编码时代）：
1. 新建 `ItemCfgData.cs` + `ItemCfgTool` 类
2. 修改 `ECfgToolType` 枚举，加 `cfg_Item`
3. 修改 `CfgToolManager.LoadAll()`，加一行 `ItemCfgTool.mInstance`

**现在**（Source Generator 时代）：
1. 新建 `ItemCfgData.cs` + `ItemCfgTool` 类
2. **什么都不用改，编译自动生成**

### 示例：新增 Item 配置表

```csharp
// Assets/2_Scripts/Framework/ConfigTool/CfgData/ItemCfgData.cs
using YGZFrameWork;

public class ItemCfgData : CfgBase<int>
{
    public string name;
    public int type;
    public int quality;
}

public class ItemCfgTool : CfgToolBase<int, ItemCfgData>
{
    protected override string mTableName => "cfg_item";

    private static ItemCfgTool _instance;
    public static ItemCfgTool Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ItemCfgTool();
                CfgToolManager.Instance.NewCfgTool(ECfgToolType.cfg_Item, _instance);
            }
            return _instance;
        }
    }
}
```

编译后，Source Generator 会自动生成：

```csharp
// CfgToolRegistry.g.cs（自动生成，不可手动修改）
namespace YGZFrameWork
{
    public static partial class CfgToolRegistry
    {
        public static void TouchAllInstances()
        {
            _ = HeroBaseCfgTool.Instance;
            _ = ItemCfgTool.Instance;        // ← 自动新增这一行
        }
    }
}
```

## 生成的代码位置

Source Generator 生成的代码不会出现在磁盘上，而是作为编译时中间产物注入到编译流程中。

在 IDE（Rider / VS）中可以通过以下方式查看：
- **Rider**：Solution Explorer → Dependencies → Analyzers → YGZFrameWork.CfgTool.SourceGenerator → CfgToolRegistry.g.cs
- **VS**：Solution Explorer → Dependencies → Analyzers

## 故障排查

### Source Generator 未触发

1. 确认 dll 已正确放置到 `Editor/` 目录
2. 确认 asmdef 的 `precompiledReferences` 包含 dll 文件名
3. 在 Unity 中 **Assets → Reimport All** 强制刷新

### 生成的代码缺少某个配置表

1. 确认该类继承自 `CfgToolBase<TKey, TData>`（不是 `CfgToolClass` 或其他）
2. 确认类不是 `abstract`
3. 确认类有静态属性 `Instance` 或 `mInstance`
4. 检查 IDE 的 Error List，看 Source Generator 是否有诊断信息

### IL2CPP 编译报错

Source Generator 生成的是纯 C# 代码，和手写代码对 IL2CPP 来说没有区别。
如果报错，请检查：
1. 配置表类是否有无参构造函数
2. `mTableName` 返回的 JSON 文件名是否正确

## 技术细节

### Source Generator 扫描规则

1. 遍历当前 Compilation 的所有 SyntaxTree
2. 找到所有 `ClassDeclarationSyntax` 且 `BaseList != null`
3. 检查类的 `BaseType` 链中是否包含 `CfgToolBase<TKey, TData>`
4. 检查类是否有静态属性 `mInstance` 或 `Instance`
5. 生成 `CfgToolRegistry.TouchAllInstances()` 方法体

### 为什么用 `_ = Xxx.Instance;`

- 触发单例属性的 getter，执行懒加载和 `NewCfgTool` 注册
- 丢弃返回值，避免 CS0219 警告（unused variable）
- 显式类型引用，IL2CPP 不会裁剪

## 后续优化方向

1. **废弃 `ECfgToolType` 枚举**：改为用 `Type` 作为 `_cfgToolDic` 的 key，新增表无需改枚举
2. **延迟加载改为显式加载**：`CfgToolBase` 构造函数不自动加载，由 `CfgToolManager.LoadAll()` 统一调用 `Load()`
3. **Source Generator 同时生成枚举**：如果仍需保留 `ECfgToolType`，可以让 Source Generator 一并生成

---

*生成日期：2025-06-07*
