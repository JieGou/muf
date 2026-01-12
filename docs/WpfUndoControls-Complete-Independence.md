# WpfUndoControls 完全独立化 - 架构重构完成

## 问题诊断

重构后虽然引入了接口抽象，但仍存在两个架构问题：

### 问题 1: Adapters 位置不合理
- **现状**: Adapters 在 `WpfUndoControls` 项目中
- **问题**: WpfUndoControls 仍然依赖 MonitoredUndo
- **影响**: 违背了独立化的目标

### 问题 2: `TryGetUndoRoot` 方法暴露实现细节
- **现状**: `private static UndoRoot TryGetUndoRoot(object root)` 方法直接调用 `UndoService.Current[root]`
- **问题**: 控件库不应该知道如何从 root 对象获取 UndoManager
- **影响**: 控件与 MonitoredUndo 的实现细节耦合

## 解决方案

### 1. 引入 Provider 模式

创建 `IUndoManagerProvider` 接口，将"如何从 root 获取 UndoManager"的逻辑交给调用方：

```csharp
public interface IUndoManagerProvider
{
    IUndoManager GetUndoManager(object root);
}
```

### 2. 架构调整

#### 新架构：

```
┌────────────────────────────────────────────────┐
│         WpfUndoControls (UI层)                 │
│    只依赖 Abstractions 中的接口                 │
│    - UndoButton, RedoButton                    │
│    - UndoRedoButtonBase                        │
└────────────────────────────────────────────────┘
                    ↓ 依赖
┌────────────────────────────────────────────────┐
│    WpfUndoControls.Abstractions (接口层)       │
│    - IUndoItem                                 │
│    - IUndoManager                              │
│    - IUndoManagerProvider                      │
└────────────────────────────────────────────────┘
                    ↑ 实现
        ┌───────────┴────────────┐
        │                        │
┌───────┴────────┐      ┌────────┴─────────┐
│  MonitoredUndo │      │   SimpleUndo     │
│   .WpfInt.     │      │   Manager        │
│   - Adapters   │      │   (示例)         │
│   - Provider   │      │                  │
└────────────────┘      └──────────────────┘
```

### 3. 代码实现

#### WpfUndoControls 的变化：

**添加 UndoManagerProvider 属性：**
```csharp
public IUndoManagerProvider UndoManagerProvider { get; set; }
```

**使用 Provider 替代直接调用：**
```csharp
private void HandleUndoRootChanged(object newValue)
{
    var provider = UndoManagerProvider;
    if (provider != null && newValue != null)
    {
        UndoManager = provider.GetUndoManager(newValue);
    }
    else
    {
        UndoManager = null;
    }
}
```

**移除 MonitoredUndo 依赖：**
```csharp
// 移除这些 using
// using MonitoredUndo;
// using WpfUndoControls.Adapters;

// 删除此方法
// private static UndoRoot TryGetUndoRoot(object root)
```

#### MonitoredUndo 的变化：

**新增 WpfIntegration 文件夹：**
- `IUndoItem.cs` - 接口定义
- `IUndoManager.cs` - 接口定义
- `IUndoManagerProvider.cs` - 接口定义
- `MonitoredUndoAdapters.cs` - 适配器实现

**MonitoredUndoManagerProvider 实现：**
```csharp
public class MonitoredUndoManagerProvider : IUndoManagerProvider
{
    public static readonly MonitoredUndoManagerProvider Instance 
        = new MonitoredUndoManagerProvider();

    public IUndoManager GetUndoManager(object root)
    {
        if (root == null) return null;
        
        var undoRoot = UndoService.Current[root];
        if (undoRoot == null) return null;
        
        return new MonitoredUndoManagerAdapter(undoRoot);
    }
}
```

## 使用方式对比

### 旧方式（不推荐，已移除）：

```xaml
<!-- 控件内部硬编码了 MonitoredUndo 的获取方式 -->
<undo:UndoButton UndoRoot="{Binding Document}" />
```

### 新方式 1: 直接绑定 IUndoManager（推荐）

```xaml
<undo:UndoButton UndoManager="{Binding UndoManager}" />
```

```csharp
public class MyViewModel
{
    public IUndoManager UndoManager { get; }
    
    public MyViewModel(MyDocument document)
    {
        // 使用 MonitoredUndo
        var undoRoot = UndoService.Current[document];
        UndoManager = new MonitoredUndoManagerAdapter(undoRoot);
        
        // 或使用 Provider
        UndoManager = MonitoredUndoManagerProvider.Instance
            .GetUndoManager(document);
    }
}
```

### 新方式 2: 使用 Provider（灵活）

```xaml
<undo:UndoButton 
    UndoRoot="{Binding Document}"
    UndoManagerProvider="{x:Static local:Providers.MonitoredUndoProvider}" />
```

```csharp
public static class Providers
{
    public static IUndoManagerProvider MonitoredUndoProvider 
        = MonitoredUndoManagerProvider.Instance;
}
```

### 新方式 3: 自定义实现

```csharp
public class MyCustomProvider : IUndoManagerProvider
{
    public IUndoManager GetUndoManager(object root)
    {
        // 自定义逻辑
        if (root is MyDocument doc)
        {
            return doc.UndoManager;
        }
        return null;
    }
}
```

## 架构优势

### 1. ? 完全独立
- WpfUndoControls 不再引用 MonitoredUndo
- 不再有任何硬编码的框架依赖

### 2. ? 灵活性
- 调用方决定如何获取 UndoManager
- 支持多种集成方式

### 3. ? 职责分离
- UI 控件：只负责展示和交互
- Adapters：由框架提供，负责适配
- Provider：由调用方提供，负责对象转换

### 4. ? 可测试性
- 可以轻松 Mock IUndoManagerProvider
- 单元测试不需要 MonitoredUndo

## 项目依赖关系

```
MonitoredUndo.Tests
    ↓ 引用
MonitoredUndo (netstandard2.0)
    包含: WpfIntegration/ (Adapters & Provider)

WpfUndoSample
    ↓ 引用
WpfUndoControls (net462, netcoreapp3.1, net8.0-windows)
    ↓ 引用
WpfUndoControls.Abstractions
    - IUndoItem
    - IUndoManager
    - IUndoManagerProvider

使用时：
Application
    ↓ 引用
    MonitoredUndo (提供 Adapters & Provider)
    WpfUndoControls (提供 UI 控件)
```

## 迁移指南

### 从旧版本迁移：

**步骤 1**: 更新 ViewModel，明确提供 UndoManager

```csharp
// 旧代码 - 依赖控件内部的魔法
public MyDocument Document { get; set; }

// 新代码 - 明确的依赖
public IUndoManager UndoManager { get; set; }

public MyViewModel()
{
    Document = new MyDocument();
    
    // 显式创建 UndoManager
    UndoManager = MonitoredUndoManagerProvider.Instance
        .GetUndoManager(Document);
}
```

**步骤 2**: 更新 XAML

```xaml
<!-- 旧代码 -->
<undo:UndoButton UndoRoot="{Binding Document}" />

<!-- 新代码（推荐） -->
<undo:UndoButton UndoManager="{Binding UndoManager}" />

<!-- 或使用 Provider -->
<undo:UndoButton 
    UndoRoot="{Binding Document}"
    UndoManagerProvider="{x:Static mu:MonitoredUndoManagerProvider.Instance}" />
```

## 性能考虑

Provider 模式增加了一层间接调用，但：
- 性能影响微乎其微（仅在属性变更时调用一次）
- 带来的架构优势远大于微小的性能开销

## 最佳实践

### 推荐方式：直接绑定 IUndoManager

```csharp
// ViewModel
public class DocumentViewModel
{
    public IUndoManager UndoManager { get; }
    
    public DocumentViewModel(IDocument document)
    {
        // 明确的依赖注入
        UndoManager = CreateUndoManager(document);
    }
    
    private IUndoManager CreateUndoManager(IDocument document)
    {
        if (document is MonitoredUndoDocument mudDoc)
        {
            return MonitoredUndoManagerProvider.Instance
                .GetUndoManager(mudDoc);
        }
        else
        {
            return new SimpleUndoManager();
        }
    }
}
```

```xaml
<!-- View -->
<undo:UndoButton UndoManager="{Binding UndoManager}" />
<undo:RedoButton UndoManager="{Binding UndoManager}" />
```

## 总结

通过引入 `IUndoManagerProvider` 和将 Adapters 移至 MonitoredUndo 项目：

1. **WpfUndoControls 完全独立** - 不再依赖任何具体的撤销/重做框架
2. **职责清晰** - 每个项目都有明确的职责
3. **灵活性最大化** - 支持多种集成方式
4. **符合 SOLID 原则** - 特别是依赖倒置原则

现在 WpfUndoControls 是一个真正通用的、框架无关的撤销/重做控件库！??
