# Telegram 自动化下载工具

## 最新更新 (v1.8.21)
- 🔧 修复输入框无法输入问题
- ⚡️ 优化 MVVM 数据绑定
- 🛠️ 改进用户界面交互
- 📝 完善输入验证机制

## 上一版本 (v1.8.20)
- 🎨 完善用户界面功能
- ⚡️ 优化按钮事件处理
- 🛠️ 改进输入验证机制
- 📝 完善状态反馈

## 程序目录结构

### 目录说明

1. **.github/**
   - `workflows/build.yml`: CI/CD自动构建配置，包含测试和发布流程
   - 自动化测试和部署配置
   - GitHub Actions 工作流定义

2. **Commands/**
   - `RelayCommand.cs`: MVVM命令实现
   - 包含所有命令处理相关的类
   - 实现命令模式的具体命令

3. **docs/**
   - `CONTRIBUTING.md`: 项目贡献指南
   - `FAQ.md`: 常见问题解答
   - 技术文档和使用说明

4. **Models/**
   - `AppSettings.cs`: 应用程序配置模型
   - `DownloadConfiguration.cs`: 下载配置模型
   - 其他数据模型类定义

5. **Services/**
   - `ChromeService.cs`: Chrome浏览器检测和管理
   - `DownloadManager.cs`: 文件下载管理和并发控制
   - `MessageProcessor.cs`: Telegram消息解析和处理
   - 核心业务逻辑服务实现

6. **Themes/**
   - `Default.xaml`: 默认主题样式定义
   - 自定义控件样式
   - 全局资源字典

7. **ViewModels/**
   - `ViewModelBase.cs`: MVVM基类实现
   - `MainViewModel.cs`: 主窗口的视图模型
   - 其他视图模型类

8. **Views/**
   - `MainWindow.xaml`: 主窗口的XAML界面定义
   - `MainWindow.xaml.cs`: 主窗口的代码后置
   - 用户界面实现

9. **根目录文件**
   - `App.xaml/cs`: 应用程入口和全局配置
   - `appsettings.json`: 应用序配置文件
   - `AutomationController.cs`: 自动化控制核心类
   - `nlog.config`: NLog日志框架配置
   - `Program.cs`: 程序入口点
   - `CHANGELOG.md`: 版本更新记录
   - `README.md`: 项目说明文档
   - `TelegramAutomation.csproj`: 项目定义文件

### 文件说明

1. **配置文件**
   - `appsettings.json`: 应用程序主配置
   - `nlog.config`: 日志配置
   - `.gitignore`: Git忽略规则

2. **核心文件**
   - `AutomationController.cs`: 自动化控制器
   - `Program.cs`: 应用程序入口
   - `App.xaml`: WPF应用程序定义

3. **项目文件**
   - `TelegramAutomation.csproj`: 项目配置
   - `packages.config`: NuGet包配置
   - `*.sln`: 解决方案文件

### 资源文件
- `Assets/`: 图片和图标资源
- `Themes/`: 主题和样式文件
- `Resources/`: 其他资源文件

### 输出目录
- `bin/`: 编译输出目录
- `obj/`: 中间文件目录
- `publish/`: 发布输出目录

### 日志目录
- `logs/`: 应用程序日志
  - `errors/`: 错误日志
  - `debug/`: 调试日志
  - `info/`: 信息日志

## 技术栈
- **框架**: 
  - .NET 6.0 - 现代化的跨平台开发框架
  - WPF (Windows Presentation Foundation) - 强大的UI框架
  - MVVM 架构模式 - 实现界面和逻分离
- **自动化**: 
  - Selenium WebDriver 4.18.1 - Web自动化框架
  - ChromeDriver - Chrome浏览器自动化驱动
  - InputSimulator - 键盘和鼠标模拟
- **依赖注入**: 
  - Microsoft.Extensions.DependencyInjection - IoC容器
  - 服务生命周期管理
  - 依赖解析和注入
- **配置管理**: 
  - Microsoft.Extensions.Configuration - 配置框架
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
  - System.Text.Json - JSON序列化
  - 配置文件处理
  - 数据存储

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
   - 消息解析
   - 内容提取
   - 文件分类

### 服务层设计

## 项目结构

### 1. 自动化登录
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
- 断点续传支持
- 自动重试机制
- 进度实时显示

### 4. 智能存储
- 按消息ID分类
- 自动创建目录结构
- 文件去重处理
- 支持多种文件格式

## 系统要求

### 硬件要求
- 处理器: 双核及以上
- 内存: 2GB以上可用内存
- 存储: 因下载内容而异
- 网络: 稳定的网络连接

### 软件要求
- 操作系统: Windows 7/10/11
- .NET 运行时: .NET 6.0 或更高版本
- 浏览器: Chrome v131.0.6778.86 或更高版本
- 科学上网环境（首次运行需要）

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
1. 输 Telegram 频道链接
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

## 调试说明
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
- 调整并发数
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
- 提交 Issue: [Issues](https://github.com/liangjie559567/TelegramAutomation/issues)
- 技术讨论: [Discussions](https://github.com/liangjie559567/TelegramAutomation/discussions)
- 文档中心: [Wiki](https://github.com/liangjie559567/TelegramAutomation/wiki)

## 贡献指南
详见 [CONTRIBUTING.md](docs/CONTRIBUTING.md)

## 更新日志
详见 [CHANGELOG.md](CHANGELOG.md)

## 许可证
本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

## 免责声明
本工具仅供学习交流使用，请勿用于非法用途。使用工具所产生的任何结果由使用者自行承担。

## 鸣谢
- 感谢所有贡献者的支持
- 感谢使用到的所有开源项目
- 感谢提供反馈的用

## 联系方式
- GitHub: [@liangjie559567](https://github.com/liangjie559567)
- 邮箱: [your-email@example.com]