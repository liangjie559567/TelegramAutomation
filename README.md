# Telegram 自动化下载工具

## 项目介绍
这是一个基于 Selenium WebDriver 的 Telegram Web 自动化工具，可以实现自动登录、验证码输入、频道切换等功能。本工具采用 .NET 8.0 开发，使用 WPF 框架构建界面，支持 Windows 平台。

## 最新功能 (v2.2.4)
- 🚀 支持 Telegram Web K 版本自动登录
- 📱 手机号验证码登录
- 🔍 自动搜索并进入指定频道
- 💫 模拟真实用户操作
- 🛡️ 稳定可靠的自动化控制

## 项目结构
TelegramAutomation/
├── Constants/ # 常量定义
│ └── ErrorCodes.cs # 错误代码常量定义，包含所有错误类型
├── Exceptions/ # 自定义异常
│ └── ChromeException.cs # Chrome相关异常处理类
├── Models/ # 数据模型
│ └── AppSettings.cs # 应用程序配置模型类
├── Services/ # 服务实现
│ └── ChromeService.cs # Chrome浏览器服务实现，处理浏览器自动化
├── ViewModels/ # MVVM视图模型
│ └── LoginViewModel.cs # 登录视图模型，处理登录相关操作
├── App.xaml # WPF应用程序定义
├── App.xaml.cs # WPF应用程序代码
├── GlobalUsings.cs # 全局 using 声明
├── Program.cs # 程序入口点，包含主要逻辑
├── TelegramAutomation.csproj # 项目文件，包含包引用和配置
├── appsettings.json # 应用程序配置文件
└── nlog.config # NLog日志配置文件

### 1. 登录功能
- ✨ 支持手机号验证码登录
  - 自动输入手机号
  - 等待用户输入验证码
  - 自动填入验证码
- 🔄 智能检测 Chrome 浏览器
  - 自动查找 Chrome 安装路径
  - 版本兼容性检查
- 🎯 自动匹配 ChromeDriver 版本
  - 自动下载匹配版本
  - 智能版本管理
- 🖱️ 模拟真实人工操作
  - 智能延时控制
  - 随机操作间隔
- 💾 会话状态持久化
  - 自动保存登录状态
  - 异常恢复机制
- 🔁 自动重试机制
  - 网络错误重试
  - 操作失败重试

### 2. 频道管理
- 🔍 智能搜索频道
  - 支持模糊搜
  - 精确匹配功能
- 🎯 精确定位目标频道
  - 多重定位策略
  - 智能元素识别
- 📱 自动进入指定频道
  - 一键切换频道
  - 状态自动验证
- 🔄 状态自动检测
  - 实时状态监控
  - 异常自动处理
- 🛡️ 操作失败自动重试
  - 智能重试策略
  - 错误恢复机制

### 3. 自动化控制
- 🎮 模拟真实用户行为
  - 智能操作延时
  - 自然交互模拟
- ⌨️ 智能键盘输入
  - 模拟人工输入
  - 随机输入间隔
- 🖱️ 精确鼠标控制
  - 智能元素定位
  - 平滑移动控制
- 📊 状态实时反馈
  - 详细日志记录
  - 操作状态监控
- 🔍 智能元素定位
  - 多策略定位
  - 自动等待机制

## 技术栈详情

### 核心框架
- **.NET 8.0**: 目标框架
- **WPF**: Windows Presentation Foundation (net8.0-windows)

### 自动化控制
- **Selenium.WebDriver** (v4.18.1)
  - 浏览器自动化核心框架
  - Chrome 浏览器控制
  - 元素定位与操作
- **DotNetSeleniumExtras.WaitHelpers** (v3.11.0)
  - 提供显式等待条件
  - 智能元素定位
- **WebDriverManager** (v2.17.2)
  - ChromeDriver 自动管理
  - 版本兼容性处理

### 配置管理
- **Microsoft.Extensions.Configuration.Json** (v8.0.0)
  - JSON 配置文件支持
  - 配置绑定功能
- **Microsoft.Extensions.Configuration.Binder** (v8.0.1)
  - 配置对象绑定
  - 类型转换支持

### 日志系统
- **NLog** (v5.2.8)
  - 文件日志记录
  - 控制台日志输出
  - 调试日志支持

### JSON 处理
- **System.Text.Json** (v8.0.5)
  - JSON 序列化
  - 配置文件处理

### MVVM 支持
- **CommunityToolkit.Mvvm** (v8.2.2)
  - MVVM 架构支持
  - 命令绑定
  - 属性通知

## 系统要求

### 环境要求
- Windows 10/11 操作系统
- .NET 8.0 SDK 或更高版本
- Google Chrome 浏览器 (90.0.0.0 或更高版本)
- Visual Studio 2022 或更高版本 (用于开发)

### 硬件要求
- CPU: 双核处理器或更高
- 内存: 4GB RAM 或更高
- 硬盘: 至少 1GB 可用空间
- 网络: 稳定的互联网连接

## 快速开始

### 1. 安装步骤

### 环境要求
- Windows 10/11
- .NET 8.0 SDK
- Google Chrome 浏览器
- Visual Studio 2022 或更高版本

### 安装步骤
1. 克隆仓库