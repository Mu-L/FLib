# UI框架
## 模块
- ### 模块UI层
- - 执行顺序
- - - `IUIOpenParamable.SetUIParam` 设置参数
- - - `IUIDataLoadable.LoadData` 加载数据
- - - `UIBase.OnBegin` 资源加载完成
- - - `UIBase.OnBeginFinish` 界面动画表现完成
- - - `UIBase.OnActiveChanged` 当界面显示隐藏时
- - 模块UI的基础UIBase
- - - 实现接口`IUIOpenParamable`可以表示打开该模块需要指定类型的参数
- - - 实现接口`IUIDataLoadable`可以表示打开该模块需要先等待加载数据
- - 主UI继承`ModuleUIBase`
- - - 模块主UI可以包含多个
- - - 实现接口`IUIOpenParamable`可以表示打开该模块需要指定类型的参数
- ### 模块公共数据服务层
- - 子类继承`ModuleService`
- - 定义标志`Module.Service(模块名称)`
- - 其他地方可以通过`AService.Instance`获取调用
- - 默认继承事件系统, 可以通过`this.Dispatch`对外抛出事件
