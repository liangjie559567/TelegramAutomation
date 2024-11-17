# Telegram 自动化下载工具

一个基于 Selenium 的 Telegram 频道内容自动化下载工具。

## 主要特性

- 🚀 自动化下载 Telegram 频道内容
- 💾 支持多种文件类型下载
- 🔄 智能重试机制
- 🔒 安全的浏览器配置
- 📝 详细的日志记录
- 🎯 简单易用的界面

## 系统要求

- Windows 操作系统
- .NET 6.0 或更高版本
- Chrome 浏览器 (122.0 或更高版本)

## 安装说明

1. 从 [Releases](https://github.com/liangjie559567/TelegramAutomation/releases) 页面下载最新版本
2. 解压下载的文件
3. 运行 TelegramAutomation.exe

## 使用方法

1. 启动程序
2. 输入 Telegram 频道链接
3. 选择保存路径
4. 点击开始按钮

## 项目结构

```
TelegramAutomation/
├── Commands/                    # 命令模式实现
│   └── RelayCommand.cs         # 通用命令实现类
│
├── Models/                      # 数据模型
│   ├── AppSettings.cs          # 应用程序配置
│   └── DownloadConfiguration.cs # 下载配置
│
├── Services/                    # 业务服务
│   ├── DownloadManager.cs      # 下载管理器
│   └── MessageProcessor.cs      # 消息处理器
│
├── Themes/                      # 主题资源
│   └── Default.xaml            # 默认主题样式
│
├── ViewModels/                  # MVVM 视图模型
│   ├── MainViewModel.cs        # 主窗口视图模型
│   └── ViewModelBase.cs        # 视图模型基类
│
├── Views/                       # 视图层
│   ├── MainWindow.xaml         # 主窗口界面
│   └── MainWindow.xaml.cs      # 主窗口代码
│
├── AutomationController.cs      # 自动化控制器
├── App.xaml                     # 应用程序定义
├── App.xaml.cs                 # 应用程序入口
├── nlog.config                 # NLog 配置文件
├── appsettings.json           # 应用程序配置文件
└── TelegramAutomation.csproj  # 项目文件
```

## 模块说明

### 1. 核心模块 (Core)
- **AutomationController**: 自动化控制核心，管理浏览器操作和登录流程
- **Models**: 定义数据结构和配置模型
- **Services**: 提供下载和消息处理服务

### 2. 用户界面 (UI)
- **Views**: WPF 界面定义
  - MainWindow.xaml: 主窗口界面
  - MainWindow.xaml.cs: 主窗口代码
- **ViewModels**: MVVM 模式的视图模型实现
- **Commands**: 命令模式实现，处理用户操作

### 3. 配置管理
- **AppSettings**: 应用程序配置管理
- **DownloadConfiguration**: 下载相关配置
- **nlog.config**: 日志配置
- **appsettings.json**: 应用程序配置文件

### 4. 功能模块

#### 4.1 登录管理
- **手机号码验证**
  - 格式自动检查
  - 智能输入延迟
  - 状态实时反馈
- **验证码处理**
  - 安全输入处理
  - 自动清理机制
  - 详细错误提示
- **登录状态维护**
  - 多重验证机制
  - 自动恢复处理
  - 会话状态管理

#### 4.2 下载管理
- 多线程下载
- 进度跟踪
- 断点续传
- 文件分类存储

#### 4.3 消息处理
- 消息文本提取
- 链接提取
- 文件识别和下载

#### 4.4 自动化控制
- 浏览器操作
- 页面导航
- 元素定位和交互

### 5. 工具和辅助

#### 5.1 日志系统
- **操作日志**
  - 自动归档管理
  - 按日期分类
  - 保留策略控制
- **错误日志**
  - 独立错误日志
  - 详细异常信息
  - 长期保留策略
- **调试日志**
  - 实时控制台输出
  - 格式化显示
  - 日志级别控制

#### 5.2 异常处理
- 全局异常捕获
- 重试机制
- 错误恢复

## 技术栈

- **.NET 6.0**: 基础框架
- **WPF**: 用户界面
- **Selenium**: 网页自动化
- **NLog**: 日志管理
- **InputSimulator**: 输入模拟
- **ChromeDriver**: 浏览器控制

## 设计模式

- **MVVM**: 界面架构模式
- **命令模式**: 用户操作处理
- **依赖注入**: 模块解耦
- **观察者模式**: 状态更新
- **工厂模式**: 对象创建

## 扩展性

项目采用模块化设计，便于扩展：
1. 可以添加新的下载处理器
2. 可以扩展消息处理方式
3. 可以添加新的自动化功能
4. 可以自定义配置项

## 性能优化

1. 使用异步操作
2. 实现并发下载
3. 资源自动释放
4. 内存管理优化
5. 日志性能优化

需要我详细解释任何部分吗？

## 依赖说明

- **WindowsInput (6.4.1)**: 键盘输入模拟
- **NLog (5.2.8)**: 日志管理
- **Selenium.WebDriver (4.18.1)**: 浏览器自动化
- **System.Text.Json (8.0.0)**: JSON 处理
- **Microsoft.Extensions.Configuration.Json (8.0.0)**: 配置管理