# Telegram 自动化下载工具

## 最新更新 (v1.8.72)
- 🔧 修复问题
  - 修复 ChromeService 异常处理
  - 完善错误代码使用
  - 优化异常信息传递
  - 加强错误追踪能力
- ⚡️ 性能优化
  - 改进异常捕获机制
  - 优化日志记录方式
  - 完善资源释放
  - 加强代码健壮性
- 🛠️ 代码重构
  - 规范化异常处理
  - 完善错误代码定义
  - 优化服务实现
  - 改进代码结构

## 上一版本 (v1.8.71)
- 🔧 优化功能
  - 改进 Chrome 服务实现
  - 优化下载管理机制
  - 完善异常处理流程
  - 加强资源管理
- ⚡️ 性能提升
  - 改进并发下载控制
  - 优化内存使用
  - 完善资源释放
  - 加强稳定性
- 🛠️ 代码优化
  - 规范化服务实现
  - 完善异步操作
  - 优化代码结构
  - 改进错误处理

## 程序目录结构

### 源代码目录

1. **.github/**
   - `workflows/`
     - `build.yml`: CI/CD自动构建配置

2. **Commands/**
   - `RelayCommand.cs`: MVVM命令实现

3. **Constants/**
   - `ErrorCodes.cs`: 错误代码常量定义

4. **Exceptions/**
   - `TelegramAutomationException.cs`: 基础异常类
   - `ChromeException.cs`: Chrome相关异常基类
   - `ChromeDriverException.cs`: ChromeDriver相关异常
   - `LoginException.cs`: 登录相关异常处理类

5. **Helpers/**
   - `LogHelper.cs`: 日志辅助工具

6. **Models/**
   - `AppSettings.cs`: 应用配置模型
   - `DownloadConfiguration.cs`: 下载配置
   - `DownloadItem.cs`: 下载项模型
   - `SessionData.cs`: 会话数据模型

7. **Services/**
   - `ChromeService.cs`: Chrome浏览器服务
   - `DownloadManager.cs`: 下载管理服务
   - `MessageProcessor.cs`: 消息处理服务

8. **ViewModels/**
   - `ViewModelBase.cs`: MVVM基类
   - `MainViewModel.cs`: 主窗口视图模型

9. **Views/**
   - `MainWindow.xaml`: 主窗口界面
   - `MainWindow.xaml.cs`: 主窗口代码

10. **docs/**
    - `CONTRIBUTING.md`: 贡献指南
    - `FAQ.md`: 常见问题

### 配置文件
- `App.xaml`: WPF应用程序定义
- `App.xaml.cs`: 应用程序代码
- `Program.cs`: 程序入口
- `GlobalUsings.cs`: 全局 using 声明
- `AutomationController.cs`: 自动化控制器
- `appsettings.json`: 应用配置文件
- `nlog.config`: 日志配置文件
- `CHANGELOG.md`: 更新日志
- `README.md`: 项目说明
- `LICENSE`: 开源许可证
- `TelegramAutomation.csproj`: 项目文件
- `.gitignore`: Git忽略规则

### 输出目录
- `bin/`
  - `Debug/`: 调试版本输出
  - `Release/`: 发布版本输出
    - `net6.0-windows/`: .NET运行时
      - `win-x64/`: Windows平台
        - `publish/`: 发布文件

### 临时文件
- `obj/`: 编译中间文件
- `logs/`: 日志文件目录
  - `errors/`: 错误日志
  - `debug/`: 调试日志
  - `info/`: 信息日志
- `session.json`: 会话数据
- `chromedriver.exe`: Chrome驱动

### 资源文件
- `Assets/`: 图片和图标
- `Resources/`: 其他资源

## 技术栈
- **框架**: 
  - .NET 8.0 - 最新的跨平台开发框架
  - WPF (Windows Presentation Foundation) - 强大的UI框架
  - MVVM 架构模式 - 实现界面和逻辑分离
- **自动化**: 
  - Selenium WebDriver 4.18.1 - Web自动化框架
  - ChromeDriver - Chrome浏览器自动化驱动
  - InputSimulatorStandard 1.0.0 - 键盘和鼠标模拟
- **依赖注入**: 
  - Microsoft.Extensions.DependencyInjection 8.0.0 - IoC容器
  - 服务生命周期管
  - 依赖解析和注入
- **配置管理**: 
  - Microsoft.Extensions.Configuration.Json 8.0.0 - 配置框架
  - Microsoft.Extensions.Configuration.Binder 8.0.0 - 配置绑定
  - JSON 配置文件支持
  - 环境变量配置
- **日志系统**: 
  - NLog 5.2.8 - 高性能日志框架
  - 文件日志和控制台输出
  - 错误追踪和诊断
- **异步处理**: 
  - Task Parallel Library (TPL) - 异步编程
  - 多线程并发下载
  - 任务取消支持
- **序列化**: 
  - System.Text.Json 8.0.5 - JSON序列化（安全版本）
  - 配置文件处理
  - 数据存储
- **MVVM 工具**: 
  - CommunityToolkit.Mvvm 8.2.2 - MVVM 工具包
  - 命令绑定
  - 属性通知

## 系统架构

### 核心组件
1. **AutomationController**
   - 浏览器自动化控制
   - 页面导航和交互
   - 会话管理

2. **ChromeService**
   - Chrome浏览器检测
   - 版本兼容性检查
   - 驱动程序管理

3. **DownloadManager**
   - 多线程下载
   - 并发控制
   - 进度监控

4. **MessageProcessor**
   - 消息析
   - 内容提取
   - 文件分类

### 服务层设计

## 项目结构

### 1. 自化登录
- 支持手机号验证码登录
- 智能检测Chrome浏览器
- 自动匹配ChromeDriver本
- 模拟人工输入行为

### 2. 消息处
- 自动提取消息文本
- 支持多媒体内容下载
- 智能链接提取
- 文件分类存储

### 3. 下载管理
- 多线程并发下载
- 断点续传持
- 自动重试机制
- 进度实时显示

### 4. 智能存储
- 按消息ID分类
- 自动创建目录结构
- 文件去重处理
- 支持多种文件格式

## 系统要求

### 硬件要求
- 处器: 双核及以上
- 内存: 2GB以上可用内存
- 存储: 因下载内容而异
- 网络: 稳定的网络连接

### 软件要求
- 操作系统: Windows 7/10/11
- .NET 运行时: .NET 8.0 或更高版本
- 浏览器: Chrome v131.0.6778.86 或更高版本
- 本地网络需要能访问 Telegram Web

## 详细安装步骤

### 1. 环境准备
- 安装 [.NET 6.0 运行时](https://dotnet.microsoft.com/download/dotnet/6.0)
- 安装 [Chrome 浏览器](https://www.google.com/chrome/)
- 确保科学上网环境正常

### 2. 程序安装
1. 从 [Releases](https://github.com/liangjie559567/TelegramAutomation/releases) 下载最新版本
2. 解压到任意目录
3. 运行 TelegramAutomation.exe

### 3. 配置说明
1. 编辑 `appsettings.json` 文件：

## 使用指南

### 1. 登录步骤
1. 启动程序
2. 输入手机号（格式：+8613800138000）
3. 点击"获取验证码"
4. 入收到的验证码
5. 点击"登录"按钮

### 2. 下载操作
1. 输 Telegram 频道链
2. 选择保存路径
3. 点击"开始"按钮
4. 等待下载完成

### 3. 高级功能
- 支持多线程下载
- 自动重试失败任务
- 断点续传支持
- 智能文件分类

## 开发指南

### 1. 开发环境
- Visual Studio 2022
- .NET 6.0 SDK
- Git
- Chrome 浏览器

### 2. 构建步骤

## 调试说
- 日志位置：`%USERPROFILE%\Documents\TelegramAutomation\logs`
- 配置文件：`appsettings.json`
- 调试模式：Visual Studio 调试器

## 开发计划

### 近期计划 (v1.9.0)
- [ ] 添加代理服务器支持
- [ ] 优化内存使用
- [ ] 添加下载速度限制
- [ ] 支持更多文件类型

### 中期计划 (v2.0.0)
- [ ] 添加GUI配置界面
- [ ] 支持批量导入链接
- [ ] 添加定时任务功能
- [ ] 支持更多 Telegram 功能

### 长期计划
- [ ] 跨平台支持
- [ ] 插件系统
- [ ] 云同步功能
- [ ] API 接口支持

## 性能优化

### 1. 内存优化
- 及时释放资源
- 使用内存池
- 避免大对象堆

### 2. 下载优化
- 调整并发
- 使用断点续传
- 优化文件写入

### 3. 网络优化
- 使用代理服务器
- 启用压缩
- 优化请求频率

## 安全建议

### 1. 账号安全
- 定期更密码
- 启用两步验证
- 注意登录备管理

### 2. 数据安全
- 加密敏感信息
- 定期备份数据
- 清理临时文件

### 3. 网络安全
- 使用安全代理
- 避免公共网络
- 注意流量限制

## 常见问题

### 1. 环境问题
Q: Chrome 版本检测失败？
A: 请确保安装了最新版本的 Chrome 浏览器。

### 2. 运行问题
Q: 无法获取验证码？
A: 请检查网络环境是否正常。

### 3. 下载问题
Q: 下载速度很慢？
A: 可能原因：
1. 网络连接不稳定
2. 并发下载数设置过高
3. 服务器限制

## 技术支持
- 交 Issue: [Issues](https://github.com/liangjie559567/TelegramAutomation/issues)
- 技术讨论: [Discussions](https://github.com/liangjie559567/TelegramAutomation/discussions)
- 文档中心: [Wiki](https://github.com/liangjie559567/TelegramAutomation/wiki)

## 贡献指南
详见 [CONTRIBUTING.md](docs/CONTRIBUTING.md)

## 更新日志
详见 [CHANGELOG.md](CHANGELOG.md)

## 许可证
本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

## 免责声明
本工具仅供学习交流使，请勿用于非法。使工具所产生的任何结果由使用者自行承担。

## 鸣谢
- 感谢所有贡献者的支持
- 感谢使用到的所有开源项目
- 感谢提供反馈的用

## 方式
- GitHub: [@liangjie559567](https://github.com/liangjie559567)
- 邮箱: [liangasjian@vip.qq.com]

## 功能目

### 1. 登录功能
- 支持手机号验证码登录
- 智能检测Chrome浏览器
- 自动匹配ChromeDriver版本
- 模拟人工输入行为
- 会话状态持久化
- 自重机制
- 陆录验证
- 误重制

### 2. 用户界面
- 实时录状态显示
- 进度实时反馈
- 状态颜色指示
- 输入验证提示
- 错误信息展示
- 网络状态监控
- 操作按钮状态管理
- 友好的用户提示

### 3. 错误处理
- 统一的异常处理机制
- 详细的错误代码系统
- 多级日志记录
- 自动错误恢复
- 网络异常重试
- 会话过期处理
- 资源释放保护
- 错误诊断信息

### 4. 日志系统
- 分级日志记
- 错误追踪
- 调试信息
- 性能监控
- 操作审计
- 日志文件轮转
- 控制台输出
- 诊断信息收集

### 5. 网络管理
- 网络状态监控
- 自动重连机制
- 连接超时处理
- 网络错误恢复
- 会状态检查
- 网络诊断
- 连接重试策略
- 网络性能优化

### 6. 消息处理
- 自动提取消息文
- 支持多媒体内容下载
- 智能链接提取
- 文件分类存储
- 内容过滤
- 批量处理
- 进度跟踪
- 状态报告

### 7. 下载管理
- 多线程并发下载
- 断点续传支持
- 自动重试机制
- 进度实时显示
- 下载队列管理
- 文件整性校验
- 重复文件处理
- 下载速度控制

### 8. 安全特性
- 会话加密存储
- 安全的登录机制
- 资源访问控制
- 错误信息保护
- 日志安全存储
- 配置文件加密
- 敏感信息处理
- 异常安全处理

## 配置文件说明

### appsettings.json 配置项说明

#### DownloadConfig（下载配置）
- MaxConcurrentDownloads: 最大并发下载数（默认：3）
- RetryAttempts: 下载失败重试次数（默认：3）
- RetryDelay: 重试延迟时间，单位毫秒（默认：1000）
- ChunkSize: 下载分块大小，单位字节（默认：1048576，即1MB）

#### ChromeDriver（Chrome浏览器配置）
- Headless: 是否使用无头模式（默认：false）
- SearchPaths: Chrome浏览器搜索路径列表
- Options: Chrome启动选项
  - disable-gpu: 禁用GPU加速
  - no-sandbox: 禁用沙箱模式
  - disable-dev-shm-usage: 禁用/dev/shm使用
  - disable-extensions: 禁用扩展

#### 其他配置
- DefaultSavePath: 默认下载保存路径
- WaitTimeout: 等待超时时间，单位秒（默认：30）
- LoginWaitTime: 登录等待时间，单位毫秒（默认：5000）