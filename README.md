# Aurora Judge - Online Judge System

[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-blue)](https://react.dev/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-blue)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

Aurora Judge 是一个现代化的在线评测系统 (Online Judge)，采用前后端分离架构，支持多种编程语言，提供完整的题目管理、比赛系统和用户权限管理功能。

## ✨ 特性

- 🚀 **高性能判题**：基于 .NET 10 Native AOT 的判题机，支持多语言 (C/C++/Java/Python)
- 🔒 **安全沙盒**：使用 isolate 沙盒进行代码隔离执行
- 👥 **双权限模型**：RBAC (基于角色) + PBAC (基于权限) 混合权限系统
- 🏆 **比赛系统**：支持公开/私有/练习赛多种比赛模式
- 📊 **实时排名**：比赛期间实时更新排行榜
- 🎨 **现代化前端**：React 18 + Ant Design + TailwindCSS
- 📝 **富文本支持**：Markdown + LaTeX 数学公式渲染
- 🔍 **代码高亮**：Monaco Editor 代码编辑器

## 🏗️ 系统架构

```
┌─────────────────────────────────────────────────────────────────┐
│                         Frontend                                  │
│                    (React + Vite + TypeScript)                    │
└───────────────────────────┬─────────────────────────────────────┘
                            │ HTTP/WebSocket
┌───────────────────────────▼─────────────────────────────────────┐
│                         Backend API                               │
│                   (ASP.NET Core 10 + Clean Architecture)          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │ Auth Service │  │Problem Service│  │ JudgerDispatch      │   │
│  └──────────────┘  └──────────────┘  │ (任务调度服务)        │   │
│                                      └──────────────────────┘   │
└───────┬──────────────────┬─────────────────────┬────────────────┘
        │                  │                     │ HTTP API
┌───────▼──────┐  ┌───────▼──────┐      ┌───────▼──────────────┐
│  PostgreSQL  │  │    Redis     │      │   Judger 1/2/3...    │
│   (数据库)    │  │ (可选-缓存)   │      │  (连接、获取、上报)  │
└──────────────┘  └──────────────┘      └──────────────────────┘
```

### 评测架构

- **Backend** 作为任务调度中心，管理 Judger 节点和任务队列
- **Judger** 通过 HTTP API 连接 Backend：
  1. 使用 JudgerId + Secret 进行身份认证
  2. 定期发送心跳保持连接
  3. 主动拉取待评测任务
  4. 评测完成后上报结果
- 支持动态增减 Judger 节点，无需依赖 RabbitMQ

## 📁 项目结构

```
AuroraJudge/
├── src/
│   ├── Backend/                    # 后端服务
│   │   ├── AuroraJudge.Api/        # API 层 (Controllers, Middleware)
│   │   ├── AuroraJudge.Application/# 应用层 (Services, DTOs)
│   │   ├── AuroraJudge.Domain/     # 领域层 (Entities, Interfaces)
│   │   ├── AuroraJudge.Infrastructure/ # 基础设施层 (EF Core, Repositories)
│   │   └── AuroraJudge.Shared/     # 共享层 (Constants, Models)
│   │
│   ├── Judger/                     # 判题机
│   │   ├── AuroraJudge.Judger/     # 判题核心服务
│   │   └── AuroraJudge.Judger.Contracts/ # 判题契约
│   │
│   └── Frontend/                   # 前端应用
│       └── src/
│           ├── components/         # 通用组件
│           ├── pages/              # 页面组件
│           ├── services/           # API 服务
│           └── stores/             # 状态管理
│
├── docker-compose.yml              # Docker 编排
└── ARCHITECTURE.md                 # 架构文档
```

## 🚀 快速开始

### 环境要求

- Docker & Docker Compose
- Node.js 20+ (开发前端)
- .NET 9 SDK (开发后端)

### 使用 Docker Compose 启动

```bash
# 克隆项目
git clone https://github.com/yourusername/aurora-judge.git
cd aurora-judge

# 配置（可选）
# 你可以先直接启动服务，然后通过前端的 Setup 向导生成/写入 backend.conf 并初始化数据库。
# 如需手动配置，也可以编辑 backend.conf / judger.conf。

# 启动所有服务
docker-compose up -d

# 查看日志
docker-compose logs -f
```

服务启动后：
- 前端：http://localhost
- API：http://localhost:5000
- Swagger 文档：http://localhost:5000/swagger

首次启动时：
- 打开前端页面，会自动进入 Setup 向导
- 在 Setup 中配置数据库连接、创建管理员账户、创建默认 Judger，并写入 backend.conf

### 本地开发

#### 后端

```bash
cd Backend/AuroraJudge.Api
dotnet restore
dotnet run
```

#### 前端

```bash
cd Frontend
npm install
npm run dev
```

#### 判题机

```bash
cd Judger/AuroraJudge.Judger
dotnet run
```

## ⚙️ 配置说明

### Backend 配置 (backend.conf)

```ini
[database]
host = localhost
port = 5432
name = aurorajudge
user = postgres
password = your_password

[jwt]
secret = your_32_char_or_longer_secret_key_here

[redis]
connection =  # 留空使用内存缓存

[judge]
mode = auto  # auto/rabbitmq/inprocess

[storage]
type = Local
local_path = ./data

[cors]
origins = http://localhost:5173,http://localhost:3000
```

### Judger 配置 (judger.conf)

```ini
[judger]
mode = http  # http 或 rabbitmq
name = judger-1
work_dir = /tmp/aurora-judge
max_concurrent_tasks = 4

[http]
backend_url = http://localhost:5000
judger_id =   # 从 Backend 注册获取
secret =      # 从 Backend 注册获取
poll_interval_ms = 1000
```

## 🔐 管理员账号

管理员账号在首次启动的 Setup 向导中创建，不再提供硬编码默认账号。

## 📖 API 文档

启动后端服务后访问：http://localhost:5000/swagger

## 🛠️ 技术栈

### 后端
- .NET 10 + ASP.NET Core
- Entity Framework Core 9
- PostgreSQL 16
- Redis 7 (可选)
- JWT Authentication

### 前端
- React 19
- TypeScript 5.7
- Vite 6
- Ant Design 5
- TailwindCSS 3
- Zustand (状态管理)
- Monaco Editor (代码编辑器)

### 判题机
- .NET 10 Native AOT
- HTTP API 通信
- isolate 沙盒

## 📄 License

GNU GPLv3 License - 详见 [LICENSE](LICENSE) 文件
