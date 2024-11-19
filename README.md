# Telegram 自动化下载工具

## 最新更新 (v1.8.1)
- 🚀 优化浏览器自动化和 Chrome 检测
- ⚡️ 完善错误处理和日志记录
- 🛡️ 增强程序稳定性和可靠性

## 技术栈
- **框架**: 
  - .NET 6.0 - 现代化的跨平台开发框架
  - WPF (Windows Presentation Foundation) - 强大的UI框架
  - MVVM 架构模式 - 实现界面和逻辑分离
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
- 自动匹配ChromeDriver版本
- 模拟人工输入行为

### 2. 消息处理
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

## 开发指南

### 1. 环境设置