# Telegram 自动化下载工具

## 项目介绍
这是一个基于 Selenium WebDriver 的 Telegram Web 自动化工具，专门用于自动化下载 Telegram 频道内容。本工具采用 .NET 8.0 开发，使用 WPF 框架构建界面，支持 Windows 平台。工具可以自动登录、验证码输入、频道切换、智能消息分组和批量文件下载等功能。

## 最新功能 (v2.3.0)
- 🚀 支持 Telegram Web K 版本自动登录
  - 自动检测登录状态
  - 智能会话保持
  - 异常自动恢复
- 📱 手机号验证码登录
  - 自动填充手机号
  - 验证码智能识别
  - 登录状态持久化
- 🔍 智能频道访问
  - 自动搜索定位频道
  - 支持直接链接访问
  - 频道状态检测
- 💫 智能消息处理
  - 消息自动分组
  - 关联文件智能匹配
  - 文本内容提取
- 📥 高级下载功能
  - 多线程并发下载
  - 断点续传支持
  - 下载进度实时显示
  - 自动重试机制
- 📁 文件智能管理
  - 按消息分组保存
  - 智能文件命名
  - 重复文件处理
  - 支持多种文件格式
- 🛡️ 稳定性保障
  - 异常自动恢复
  - 操作重试机制
  - 详细日志记录
  - 状态实时监控

## 项目结构
```
TelegramAutomation/
├── Services/                    # 核心服务实现
│   ├── ChromeService.cs        # Chrome 浏览器自动化服务
│   ├── FileDownloadService.cs  # 文件下载核心服务
│   ├── DownloadService.cs      # 下载任务管理服务
│   ├── MessageProcessor.cs     # 消息处理服务
│   ├── ScrollHelper.cs         # 页面滚动辅助服务
│   └── DownloadManager.cs      # 下载管理器服务
├── Models/                     # 数据模型
│   ├── AppSettings.cs         # 应用程序配置模型
│   ├── MessageContent.cs      # 消息内容模型
│   ├── DownloadItem.cs        # 下载项模型
│   ├── MessageCache.cs        # 消息缓存模型
│   ├── ScrollStatus.cs        # 滚动状态模型
│   ├── SessionData.cs         # 会话数据模型
│   ├── DOMRect.cs            # DOM 元素位置模型
│   └── DownloadConfiguration.cs # 下载配置模型
├── ViewModels/                # MVVM 视图模型
│   └── LoginViewModel.cs      # 登录视图模型
├── Helpers/                   # 辅助工具类
│   └── LogHelper.cs          # 日志辅助工具
├── Constants/                 # 常量定义
│   └── ErrorCodes.cs         # 错误代码常量
├── Exceptions/                # 自定义异常
│   ├── ChromeException.cs    # Chrome 相关异常
│   ├── LoginException.cs     # 登录相关异常
│   ├── ChromeDriverException.cs # ChromeDriver 异常
│   └── TelegramAutomationException.cs # 通用异常基类
├── Program.cs                 # 程序入口点
├── GlobalUsings.cs           # 全局 using 声明
├── TelegramAutomation.csproj # 项目配置文件
├── appsettings.json         # 应用程序配置文件
├── nlog.config              # NLog 日志配置
└── CHANGELOG.md             # 更新日志
```

### 核心模块说明

#### 1. 服务层 (Services/)
- **ChromeService**: Chrome 浏览器控制和自动化核心
- **FileDownloadService**: 文件下载实现和管理
- **DownloadService**: 下载任务调度和控制
- **MessageProcessor**: Telegram 消息解析和处理
- **ScrollHelper**: 页面滚动和内容加载
- **DownloadManager**: 并发下载管理

#### 2. 数据模型 (Models/)
- **AppSettings**: 应用配置数据结构
- **MessageContent**: 消息内容数据模型
- **DownloadItem**: 下载项数据结构
- **MessageCache**: 消息缓存管理
- **ScrollStatus**: 滚动状态追踪
- **SessionData**: 会话状态管理
- **DownloadConfiguration**: 下载配置管理

#### 3. 视图模型 (ViewModels/)
- **LoginViewModel**: 登录界面交互逻辑

#### 4. 辅助工具 (Helpers/)
- **LogHelper**: 日志记录工具

#### 5. 异常处理 (Exceptions/)
- **ChromeException**: 浏览器相关异常
- **LoginException**: 登录过程异常
- **ChromeDriverException**: 驱动程序异常
- **TelegramAutomationException**: 基础异常类

#### 6. 配置文件
- **appsettings.json**: 应用程序配置
- **nlog.config**: 日志系统配置
- **TelegramAutomation.csproj**: 项目依赖配置

## 核心功能详解

### 1. 智能登录系统
- ✨ 多方式登录支持
  - 手机号验证码登录
  - 会话状态持久化
  - 自动会话恢复
- 🔄 Chrome 智能管理
  - 自动检测安装路径
  - 版本兼容性检查
  - 配置自动优化
- 🎯 ChromeDriver 自动化
  - 版本智能匹配
  - 自动下载安装
  - 配置自动更新
- 🖱️ 智能操作模拟
  - 人工操作模拟
  - 智能延时控制
  - 随机等待间隔
- 💾 会话管理
  - 登录状态保持
  - 异常自动恢复
  - 会话状态监控

### 2. 频道内容管理
- 🔍 智能内容定位
  - 精确频道搜索
  - 消息智能分组
  - 关联内容匹配
- 📝 消息处理
  - 文本内容提取
  - 链接自动收集
  - 文件信息提取
- 📱 频道操作
  - 自动频道切换
  - 状态实时检测
  - 异常自动处理
- 🔄 内容同步
  - 增量内容获取
  - 历史消息加载
  - 实时内容更新

### 3. 文件下载系统
- 📥 下载核心功能
  - 多线程并发下载
  - 断点续传支持
  - 大文件分片下载
  - 网络异常重试
- 📁 文件组织管理
  - 智能分组存储
  - 文件名优化
  - 目录结构管理
  - 重复文件处理
- 📊 下载任务管理
  - 任务队列管理
  - 优先级控制
  - 并发数限制
  - 失败任务重试
- 🔍 文件完整性
  - 下载验证
  - 文件校验
  - 损坏修复
  - 重新下载

### 4. 自动化控制系统
- 🎮 智能操作控制
  - 精确元素定位
  - 动作链管理
  - 操作序列优化
- ⌨️ 输入控制
  - 智能输入模拟
  - 特殊字符处理
  - 输入验证
- 🖱️ 页面交互
  - 智能滚动控制
  - 元素状态检测
  - 视图自动刷新
- 📊 状态监控
  - 实时状态追踪
  - 异常检测
  - 性能监控

## 技术架构

### 1. 开发框架
- **.NET 8.0**
  - 目标框架: net8.0-windows
  - 语言版本: C# 12.0
  - 运行时: .NET Runtime 8.0.0
  - 开发工具: Visual Studio 2022

- **WPF (Windows Presentation Foundation)**
  - UI框架版本: 8.0.0
  - XAML开发模式
  - MVVM架构模式
  - 响应式界面设计

### 2. 核心技术栈
- **Selenium 自动化**
  - Selenium.WebDriver (v4.18.1)
    - WebDriver协议实现
    - 元素定位与操作
    - 事件监听与触发
    - 异步操作支持
  - DotNetSeleniumExtras.WaitHelpers (v3.11.0)
    - 显式等待机制
    - 条件判断支持
    - 超时处理机制
  - WebDriverManager (v2.17.2)
    - 驱动程序管理
    - 版本自动匹配
    - 配置自动化

- **配置管理**
  - Microsoft.Extensions.Configuration.Json (v8.0.0)
    - JSON配置支持
    - 分层配置管理
    - 环境变量集成
    - 动态配置更新
  - Microsoft.Extensions.Configuration.Binder (v8.0.1)
    - 强类型配置绑定
    - 配置验证机制
    - 默认值处理

- **日志系统**
  - NLog (v5.2.8)
    - 多目标日志记录
    - 异步日志支持
    - 日志级别控制
    - 日志格式化
    - 文件滚动策略
    - 性能优化

- **MVVM框架**
  - CommunityToolkit.Mvvm (v8.2.2)
    - MVVM基础设施
    - 属性更改通知
    - 命令绑定系统
    - 消息通信机制
    - 依赖属性支持

### 3. 开发规范

#### 代码规范
- **命名规范**
  - 类名: PascalCase (如: FileDownloadService)
  - 方法名: PascalCase (如: ProcessDownload)
  - 私有字段: _camelCase (如: _logger)
  - 属性: PascalCase (如: DownloadStatus)
  - 接口: I前缀 (如: IDownloadService)
  - 常量: UPPER_CASE (如: MAX_RETRY_COUNT)

- **文件组织**
  - 每个类一个文件
  - 文件名与类名一致
  - 按功能模块分目录
  - 相关文件放在同一目录

- **注释规范**
  - 类和公共方法必须添加文档注释
  - 复杂逻辑需要添加行内注释
  - 使用英文编写注释
  - 及时更新注释内容

#### 异常处理
- **异常分层**
  - 自定义异常继承基础异常类
  - 异常信息包含错误代码
  - 提供详细的异常描述
  - 记录异常堆栈信息

- **日志记录**
  - 错误日志包含异常详情
  - 分级别记录日志信息
  - 关键操作记录INFO日志
  - 调试信息使用DEBUG级别

#### 性能优化
- **资源管理**
  - 使用 using 语句管理资源
  - 及时释放大对象
  - 避免内存泄漏
  - 控制并发数量

- **并发处理**
  - 使用异步编程模型
  - 避免死锁情况
  - 合理使用线程池
  - 控制并发粒度

### 4. 自动化测试
- **单元测试**
  - 使用 MSTest 框架
  - 测试覆盖核心逻辑
  - 模拟外部依赖
  - 验证边界条件

- **集成测试**
  - 测试组件交互
  - 验证配置加载
  - 检查异常处理
  - 性能指标测试

### 5. 部署要求
- **运行环境**
  - Windows 10/11
  - .NET 8.0 Runtime
  - Chrome 浏览器 90+
  - 4GB+ 可用内存

- **依赖组件**
  - Chrome WebDriver
  - 必要的系统DLL
  - 配置文件完整性
  - 足够的磁盘空间

### 6. 安全性考虑
- **数据安全**
  - 敏感信息加密存储
  - 配置文件访问控制
  - 临时文件安全处理
  - 用户数据保护

- **运行安全**
  - 权限最小化原则
  - 异常安全处理
  - 资源访问控制
  - 并发安全保障

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
1. 克隆仓库到本地
2. 安装必要的依赖包
3. 配置开发环境
4. 编译运行项目

### 2. 使用说明
1. 启动程序
2. 输入手机号登录
3. 填写验证码
4. 输入目标频道
5. 选择下载内容
6. 等待下载完成

### 3. 配置说明
- 在 `appsettings.json` 中配置：
  - 下载路径
  - 并发数
  - 超时设置
  - 重试策略
  - 日志级别

## 更新日志
请查看 [CHANGELOG.md](CHANGELOG.md) 获取详细更新记录。