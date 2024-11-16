# Telegram 自动化下载工具

这是一个基于 .NET 6 开发的 Windows 桌面应用程序，用于自动化下载 Telegram 网页版频道中的消息内容和文件。

## 功能特点

- Telegram 账户登录管理
  - 支持手机号码验证登录
  - 验证码自动发送
  - 登录状态持久化
  - 会话自动恢复
- 自动化浏览 Telegram 网页版频道
  - 自动滚动加载更多内容
  - 智能检测页面底部
  - 自动处理加载失败
- 自动提取并保存消息内容
  - 保存消息文本
  - 提取消息中的链接
  - 自动下载压缩包文件
  - 支持断点续传
- 文件管理
  - 支持多种压缩格式
  - 按消息 ID 分类存储
  - 自定义下载路径
  - 磁盘空间检查
- 下载控制
  - 多线程并发下载
  - 实时显示下载进度
  - 支持暂停/继续下载
  - 自动重试失败任务
  - 下载队列管理
- 用户界面
  - 清晰的操作界面
  - 实时日志显示
  - 详细的错误提示
  - 状态栏信息更新
  - 进度条显示

## 开发环境

- Visual Studio 2022 或 JetBrains Rider
- .NET 6.0 SDK
- Windows 10/11
- Git

## 项目结构

```
TelegramDownloads/
├── 消息ID1/
│   ├── message.txt      # 消息文本内容
│   ├── links.txt        # 消息中的链接
│   └── 下载的文件.zip    # 下载的压缩包文件
├── 消息ID2/
│   ├── message.txt
│   ├── links.txt
│   └── 下载的文件.rar
└── logs/                # 程序运行日志
    └── 2024-03-14.log
```

## 部署方法

### 方法一：从源码编译

1. 克隆仓库
```
git clone https://github.com/liangjie559567/TelegramAutomation.git
```

2. 安装依赖
```
dotnet restore
```

3. 编译项目
```
dotnet build --configuration Release
```

4. 运行程序
```
dotnet run --project TelegramAutomation
```

### 方法二：直接使用发布版本

1. 从 [Releases](https://github.com/liangjie559567/TelegramAutomation/releases) 页面下载最新版本
2. 解压文件
3. 运行 TelegramAutomation.exe

## 注意事项

- 首次使用时需要手动登录 Telegram
- 确保有足够的磁盘空间存储下载的文件
- 下载大文件时请保持网络稳定
- 程序会自动创建必要的文件夹
- 日志文件保存在"我的文档/TelegramAutomation/logs"目录下

## 许可证

本项目采用 MIT 许可证，详见 [LICENSE](LICENSE) 文件。

## 更新日志

详见 [CHANGELOG.md](CHANGELOG.md) 文件。

## 贡献指南

欢迎提交 Issue 和 Pull Request 来帮助改进这个项目。

1. Fork 本仓库
2. 创建你的特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交你的更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开一个 Pull Request

## 问题反馈

如果你发现任何问题或有改进建议，请在 [Issues](https://github.com/liangjie559567/TelegramAutomation/issues) 页面提交。