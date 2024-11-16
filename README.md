# Telegram 自动化下载工具

这是一个基于 .NET 6 开发的 Windows 桌面应用程序，用于自动化下载 Telegram 网页版频道中的消息内容和文件。

## 功能特点

- 自动化浏览 Telegram 网页版频道
- 自动提取并保存消息文本内容
- 自动下载压缩包文件（支持 .zip、.rar、.7z、.tar、.gz 格式）
- 保存消息中的所有链接
- 按消息 ID 分类存储所有内容
- 实时显示下载进度和操作日志
- 支持暂停/继续下载任务
- 支持自定义下载路径
- 自动重试失败的下载
- 多线程并发下载提升效率

## 系统要求

- Windows 操作系统
- .NET 6.0 或更高版本
- Google Chrome 浏览器
- 稳定的网络连接
- 足够的磁盘空间

## 安装说明

1. 下载并安装 [.NET 6.0 Runtime](https://dotnet.microsoft.com/download/dotnet/6.0)
2. 确保已安装 Google Chrome 浏览器
3. 从 [Releases](https://github.com/liangjie559567/TelegramAutomation/releases) 页面下载最新版本
4. 解压到任意目录

## 使用方法

1. 运行 TelegramAutomation.exe
2. 在程序界面输入 Telegram 频道的 URL
3. 选择文件保存路径（默认为"我的文档/TelegramDownloads"）
4. 点击"开始"按钮启动自动化任务
5. 在打开的浏览器窗口中手动登录 Telegram（首次使用时）
6. 程序将自动开始下载内容
7. 可以随时点击"停止"按钮暂停任务

## 文件存储结构

下载的内容按以下结构保存：

## 部署方法

### 方法一：从源码编译

1. 克隆仓库