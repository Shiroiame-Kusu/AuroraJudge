# Aurora Judge - 项目状态报告

## 📋 项目完成度检查

### ✅ 后端 (Backend) - ASP.NET Core

| 模块 | 状态 | 说明 |
|------|------|------|
| API 层 (Controllers) | ✅ 完成 | Auth, Problems, Submissions, Contests, Admin |
| 应用层 (Services) | ✅ 完成 | 所有业务服务实现完整 |
| 领域层 (Entities) | ✅ 完成 | User, Role, Permission, Problem, Contest, Submission 等 |
| 基础设施层 | ✅ 完成 | EF Core, Repositories, DbContext |
| 认证授权 | ✅ 完成 | JWT + RBAC + PBAC 双重权限模型 |
| Swagger/OpenAPI | ✅ 完成 | 完整的 API 文档 |

### ✅ 前端 (Frontend) - React + Vite

| 模块 | 状态 | 说明 |
|------|------|------|
| 页面组件 | ✅ 完成 | Home, Login, Register, Problems, Contests, Submissions, Rankings, Profile, Admin |
| API 服务 | ✅ 完成 | authService, problemService, contestService, submissionService |
| 状态管理 | ✅ 完成 | Zustand stores (auth, ui) |
| UI 组件 | ✅ 完成 | Ant Design + TailwindCSS |
| 路由 | ✅ 完成 | React Router v7 |

### ✅ 判题机 (Judger)

| 模块 | 状态 | 说明 |
|------|------|------|
| JudgeService | ✅ 完成 | 多语言支持 (C, C++, Java, Python) |
| SandboxRunner | ✅ 完成 | 进程隔离、资源限制 |
| RabbitMQ 集成 | ✅ 完成 | 消息队列消费与结果发布 |
| 心跳机制 | ✅ 完成 | 判题机状态上报 |

---

## 🔧 API 对齐修复

以下 API 路径不匹配问题已修复:

| 问题 | 修复方案 |
|------|----------|
| 前端使用 `/api/*`，后端使用 `/api/v1/*` | Vite 代理配置添加路径重写 |
| `/auth/me` vs `/auth/profile` | 前端改为 `/auth/profile` |
| `/contests/:id/ranking` vs `/contests/:id/standings` | 前端改为 `/contests/:id/standings` |
| 缺少 `unregister`, `getProblems`, `getAnnouncements` | 已添加到 contestService |

---

## 🚀 Rider 运行配置

已创建以下运行配置:

### 复合运行配置 (一键启动)
- **名称**: `AuroraJudge Full Stack`
- **包含**: Backend API + Judger Service + Frontend Dev Server

### 单独运行配置
1. **Backend API** - 后端 API 服务 (http://localhost:5000)
2. **Judger Service** - 判题机服务
3. **Frontend Dev Server** - 前端开发服务器 (http://localhost:3000)
4. **Docker Services** - 基础设施服务 (PostgreSQL, Redis, RabbitMQ, MinIO)

---

## 📦 启动顺序

### 方式一: 使用 Rider 复合配置

1. 在 Rider 中选择运行配置 `AuroraJudge Full Stack`
2. 点击运行按钮 (或按 Shift+F10)
3. 三个服务将同时启动

### 方式二: 使用启动脚本

```bash
# 启动所有服务
./start-dev.sh all

# 或分别启动
./start-dev.sh infra      # 仅启动基础设施
./start-dev.sh backend    # 仅启动后端
./start-dev.sh judger     # 仅启动判题机
./start-dev.sh frontend   # 仅启动前端

# 停止所有服务
./start-dev.sh stop
```

### 方式三: 使用 Docker Compose

```bash
# 启动所有服务（生产模式）
docker-compose up -d

# 仅启动基础设施（开发模式）
docker-compose up -d postgres redis rabbitmq minio
```

---

## 🔗 服务访问地址

| 服务 | URL |
|------|-----|
| 前端 | http://localhost:3000 |
| API | http://localhost:5000 |
| Swagger | http://localhost:5000/swagger |
| RabbitMQ 管理 | http://localhost:15672 (guest/guest) |
| MinIO 控制台 | http://localhost:9001 (aurora/aurora123456) |

---

## 🔑 默认账号

管理员账号在首次启动的 Setup 向导中创建，不再提供硬编码默认账号。

---

## ⚠️ 注意事项

1. **首次运行前**: 确保 Docker 服务已启动，基础设施服务 (PostgreSQL, Redis, RabbitMQ) 可用
2. **数据库**: 后端不会在“未初始化状态”自动建表；首次运行请通过 Setup 向导初始化数据库并创建管理员
3. **判题机**: 需要 RabbitMQ 服务运行才能正常工作
4. **前端代理**: 开发模式下前端通过 Vite 代理转发 API 请求到后端

---

## 📁 项目结构

```
AuroraJudge/
├── Backend/                        # 后端服务
│   ├── AuroraJudge.Api/            # API 层
│   ├── AuroraJudge.Application/    # 应用层
│   ├── AuroraJudge.Domain/         # 领域层
│   ├── AuroraJudge.Infrastructure/ # 基础设施层
│   └── AuroraJudge.Shared/         # 共享层
├── Frontend/                       # 前端应用
│   └── src/
│       ├── components/
│       ├── pages/
│       ├── services/
│       └── stores/
├── Judger/                         # 判题机
│   ├── AuroraJudge.Judger/
│   └── AuroraJudge.Judger.Contracts/
├── docker-compose.yml              # Docker 编排
├── start-dev.sh                    # 开发启动脚本
└── AuroraJudge.sln                 # 解决方案文件
```
