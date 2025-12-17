# AuroraJudge - Online Judge System Architecture

## 1. 系统概述

AuroraJudge 是一个生产级、高性能、可扩展的在线评测系统（Online Judge），采用微服务架构设计，由三个独立部署的子系统组成：

- **Backend**: ASP.NET Core 后端服务
- **Frontend**: Vite + React 现代化前端
- **Judger**: .NET Native AOT 高性能判题器

## 2. 整体架构图

```
                                    ┌─────────────────────────────────────────────────────────────┐
                                    │                        Load Balancer                         │
                                    │                      (Nginx/Traefik)                        │
                                    └─────────────────────────────────────────────────────────────┘
                                                              │
                    ┌─────────────────────────────────────────┼─────────────────────────────────────────┐
                    │                                         │                                         │
                    ▼                                         ▼                                         ▼
        ┌───────────────────┐                     ┌───────────────────┐                     ┌───────────────────┐
        │     Frontend      │                     │      Backend      │                     │   Admin Panel     │
        │  (Vite + React)   │                     │  (ASP.NET Core)   │                     │     (React)       │
        │                   │                     │                   │                     │                   │
        │ - User Interface  │────HTTP/REST───────▶│ - RESTful API     │◀───HTTP/REST───────│ - System Config   │
        │ - Code Editor     │                     │ - Authentication  │                     │ - User Management │
        │ - Dark Mode       │                     │ - Authorization   │                     │ - Problem Editor  │
        │ - Responsive      │                     │ - Business Logic  │                     │                   │
        └───────────────────┘                     └─────────┬─────────┘                     └───────────────────┘
                                                            │
                    ┌───────────────────────────────────────┼───────────────────────────────────────┐
                    │                                       │                                       │
                    ▼                                       ▼                                       ▼
        ┌───────────────────┐               ┌───────────────────────────┐               ┌───────────────────┐
        │    PostgreSQL     │               │      Redis Cluster        │               │    RabbitMQ       │
        │                   │               │                           │               │                   │
        │ - User Data       │               │ - Session Cache           │               │ - Judge Queue     │
        │ - Problems        │               │ - Leaderboard Cache       │               │ - Result Queue    │
        │ - Submissions     │               │ - Rate Limiting           │               │ - Event Queue     │
        │ - Contests        │               │ - Real-time Data          │               │                   │
        └───────────────────┘               └───────────────────────────┘               └─────────┬─────────┘
                                                                                                  │
                                                                                                  ▼
                                                                            ┌─────────────────────────────────────┐
                                                                            │          Judger Cluster             │
                                                                            │       (.NET Native AOT)             │
                                                                            │                                     │
                                                                            │  ┌─────────┐ ┌─────────┐ ┌─────────┐│
                                                                            │  │ Judger  │ │ Judger  │ │ Judger  ││
                                                                            │  │   #1    │ │   #2    │ │   #N    ││
                                                                            │  └────┬────┘ └────┬────┘ └────┬────┘│
                                                                            │       │          │          │      │
                                                                            │       ▼          ▼          ▼      │
                                                                            │  ┌─────────────────────────────────┐│
                                                                            │  │     Sandbox (nsjail/isolate)   ││
                                                                            │  │   - Namespace Isolation        ││
                                                                            │  │   - Resource Limits (cgroups)  ││
                                                                            │  │   - Seccomp Filtering          ││
                                                                            │  └─────────────────────────────────┘│
                                                                            └─────────────────────────────────────┘
                                                                                              │
                                                                                              ▼
                                                                            ┌─────────────────────────────────────┐
                                                                            │         Test Data Storage          │
                                                                            │     (MinIO / Local Filesystem)     │
                                                                            └─────────────────────────────────────┘
```

## 3. 模块划分

### 3.1 Backend 模块结构

```
AuroraJudge.Backend/
├── AuroraJudge.Api/                    # Web API 层
│   ├── Controllers/                    # API 控制器
│   ├── Middlewares/                    # 中间件
│   ├── Filters/                        # 过滤器
│   └── Program.cs                      # 入口点
├── AuroraJudge.Application/            # 应用服务层
│   ├── Commands/                       # CQRS 命令
│   ├── Queries/                        # CQRS 查询
│   ├── Services/                       # 应用服务
│   ├── DTOs/                           # 数据传输对象
│   └── Validators/                     # 验证器
├── AuroraJudge.Domain/                 # 领域层
│   ├── Entities/                       # 领域实体
│   ├── ValueObjects/                   # 值对象
│   ├── Enums/                          # 枚举
│   ├── Events/                         # 领域事件
│   └── Interfaces/                     # 领域接口
├── AuroraJudge.Infrastructure/         # 基础设施层
│   ├── Persistence/                    # 数据持久化
│   ├── Identity/                       # 身份认证
│   ├── Messaging/                      # 消息队列
│   ├── Caching/                        # 缓存
│   └── Storage/                        # 文件存储
└── AuroraJudge.Shared/                 # 共享库
    ├── Constants/                      # 常量
    ├── Extensions/                     # 扩展方法
    └── Utils/                          # 工具类
```

### 3.2 Frontend 模块结构

```
AuroraJudge.Frontend/
├── src/
│   ├── components/                     # 通用组件
│   │   ├── ui/                         # 基础 UI 组件
│   │   ├── layout/                     # 布局组件
│   │   └── business/                   # 业务组件
│   ├── pages/                          # 页面组件
│   │   ├── auth/                       # 认证页面
│   │   ├── problems/                   # 题目页面
│   │   ├── contests/                   # 比赛页面
│   │   ├── submissions/                # 提交页面
│   │   └── admin/                      # 管理后台
│   ├── hooks/                          # 自定义 Hooks
│   ├── stores/                         # 状态管理
│   ├── services/                       # API 服务
│   ├── utils/                          # 工具函数
│   ├── types/                          # TypeScript 类型
│   └── styles/                         # 全局样式
├── public/                             # 静态资源
└── package.json
```

### 3.3 Judger 模块结构

```
AuroraJudge.Judger/
├── AuroraJudge.Judger/                 # 主判题服务
│   ├── Workers/                        # 判题工作器
│   ├── Sandbox/                        # 沙箱管理
│   ├── Compilers/                      # 编译器适配
│   ├── Runners/                        # 运行器
│   ├── Checkers/                       # 结果校验器
│   └── Program.cs                      # AOT 入口
├── AuroraJudge.Judger.Contracts/       # 判题契约
│   ├── Messages/                       # 消息定义
│   └── Enums/                          # 枚举定义
└── testlib/                            # Testlib 库支持
```

## 4. 核心数据模型

### 4.1 用户与权限模型

```csharp
// 用户实体
public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string? Avatar { get; set; }
    public string? Bio { get; set; }
    public UserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // 统计信息
    public int SolvedCount { get; set; }
    public int SubmissionCount { get; set; }
    public int Rating { get; set; }
    
    // 导航属性
    public ICollection<UserRole> UserRoles { get; set; }
    public ICollection<UserPermission> UserPermissions { get; set; }
}

// 角色实体
public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; }  // 系统内置角色不可删除
    public ICollection<RolePermission> RolePermissions { get; set; }
}

// 权限实体
public class Permission
{
    public Guid Id { get; set; }
    public string Code { get; set; }        // e.g., "problem.create"
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; }    // 权限分类
}
```

### 4.2 题目模型

```csharp
public class Problem
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }      // Markdown
    public string InputFormat { get; set; }
    public string OutputFormat { get; set; }
    public string? Hint { get; set; }
    public string? Source { get; set; }
    
    // 限制
    public int TimeLimit { get; set; }           // ms
    public int MemoryLimit { get; set; }         // KB
    public int StackLimit { get; set; }          // KB
    
    // 评测配置
    public JudgeMode JudgeMode { get; set; }     // Standard/SpecialJudge/Interactive
    public string? SpecialJudgeCode { get; set; }
    public string? SpecialJudgeLanguage { get; set; }
    
    // 状态
    public ProblemVisibility Visibility { get; set; }
    public ProblemDifficulty Difficulty { get; set; }
    
    // 统计
    public int SubmissionCount { get; set; }
    public int AcceptedCount { get; set; }
    
    // 关系
    public Guid CreatorId { get; set; }
    public User Creator { get; set; }
    public ICollection<TestCase> TestCases { get; set; }
    public ICollection<ProblemTag> ProblemTags { get; set; }
}

public class TestCase
{
    public Guid Id { get; set; }
    public Guid ProblemId { get; set; }
    public int Order { get; set; }
    public string InputPath { get; set; }
    public string OutputPath { get; set; }
    public int Score { get; set; }              // 用于部分分评测
    public bool IsSample { get; set; }          // 是否为样例
}
```

### 4.3 提交模型

```csharp
public class Submission
{
    public Guid Id { get; set; }
    public Guid ProblemId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ContestId { get; set; }
    
    public string Code { get; set; }
    public string Language { get; set; }
    public int CodeLength { get; set; }
    
    // 评测结果
    public JudgeStatus Status { get; set; }
    public int? Score { get; set; }
    public int? TimeUsed { get; set; }          // ms
    public int? MemoryUsed { get; set; }        // KB
    public string? CompileMessage { get; set; }
    public string? JudgeMessage { get; set; }
    
    public DateTime SubmittedAt { get; set; }
    public DateTime? JudgedAt { get; set; }
    
    // 导航
    public Problem Problem { get; set; }
    public User User { get; set; }
    public Contest? Contest { get; set; }
    public ICollection<JudgeResult> JudgeResults { get; set; }
}

public class JudgeResult
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public int TestCaseOrder { get; set; }
    
    public JudgeStatus Status { get; set; }
    public int TimeUsed { get; set; }
    public int MemoryUsed { get; set; }
    public int Score { get; set; }
    public string? Message { get; set; }
}
```

### 4.4 比赛模型

```csharp
public class Contest
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime? FreezeTime { get; set; }   // 封榜时间
    
    public ContestType Type { get; set; }        // ACM/OI/IOI
    public ContestVisibility Visibility { get; set; }
    public string? Password { get; set; }        // 私有比赛密码
    
    public bool IsRated { get; set; }
    public int? RatingFloor { get; set; }
    public int? RatingCeiling { get; set; }
    
    public Guid CreatorId { get; set; }
    public User Creator { get; set; }
    
    public ICollection<ContestProblem> ContestProblems { get; set; }
    public ICollection<ContestParticipant> Participants { get; set; }
}

public class ContestProblem
{
    public Guid ContestId { get; set; }
    public Guid ProblemId { get; set; }
    public string Label { get; set; }           // A, B, C...
    public int Order { get; set; }
    public int? Score { get; set; }             // OI 模式分值
}
```

## 5. 权限模型设计

### 5.1 双重权限模型

系统采用 **RBAC (Role-Based Access Control)** 与 **PBAC (Permission-Based Access Control)** 结合的双重模型：

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              权限验证流程                                         │
│                                                                                 │
│   User ──────┬──────────────────────────────────────────────────────────────┐   │
│              │                                                              │   │
│              ▼                                                              ▼   │
│   ┌──────────────────┐                                      ┌───────────────────┐
│   │   UserRoles      │                                      │  UserPermissions  │
│   │                  │                                      │  (直接授权)        │
│   │ User ──◆── Role  │                                      │                   │
│   └────────┬─────────┘                                      │ User ──◆── Perm   │
│            │                                                └─────────┬─────────┘
│            ▼                                                          │         │
│   ┌──────────────────┐                                                │         │
│   │ RolePermissions  │                                                │         │
│   │                  │                                                │         │
│   │ Role ──◆── Perm  │                                                │         │
│   └────────┬─────────┘                                                │         │
│            │                                                          │         │
│            └──────────────────────┬───────────────────────────────────┘         │
│                                   │                                             │
│                                   ▼                                             │
│                        ┌─────────────────────┐                                  │
│                        │   权限验证器         │                                  │
│                        │   HasPermission()   │                                  │
│                        └─────────────────────┘                                  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### 5.2 权限点设计

```csharp
public static class Permissions
{
    // 用户管理
    public const string UserView = "user.view";
    public const string UserCreate = "user.create";
    public const string UserEdit = "user.edit";
    public const string UserDelete = "user.delete";
    public const string UserBan = "user.ban";
    
    // 题目管理
    public const string ProblemView = "problem.view";
    public const string ProblemViewHidden = "problem.view.hidden";
    public const string ProblemCreate = "problem.create";
    public const string ProblemEdit = "problem.edit";
    public const string ProblemDelete = "problem.delete";
    public const string ProblemRejudge = "problem.rejudge";
    
    // 比赛管理
    public const string ContestView = "contest.view";
    public const string ContestCreate = "contest.create";
    public const string ContestEdit = "contest.edit";
    public const string ContestDelete = "contest.delete";
    public const string ContestManageParticipants = "contest.participants";
    
    // 提交管理
    public const string SubmissionView = "submission.view";
    public const string SubmissionViewAll = "submission.view.all";
    public const string SubmissionViewCode = "submission.view.code";
    public const string SubmissionRejudge = "submission.rejudge";
    
    // 系统管理
    public const string SystemSettings = "system.settings";
    public const string SystemAuditLog = "system.audit";
    public const string SystemJudger = "system.judger";
    public const string RoleManage = "role.manage";
    public const string PermissionManage = "permission.manage";
}
```

### 5.3 默认角色

| 角色 | 说明 | 权限 |
|------|------|------|
| SuperAdmin | 超级管理员 | 所有权限 |
| Admin | 管理员 | 除系统核心设置外的所有权限 |
| ProblemSetter | 出题人 | 题目、比赛的创建和管理 |
| Moderator | 版主 | 用户管理、内容审核 |
| User | 普通用户 | 基础功能：查看、提交 |
| Guest | 访客 | 仅查看公开内容 |

## 6. 判题流程时序

```
┌──────┐    ┌─────────┐    ┌──────────┐    ┌─────────┐    ┌──────────┐    ┌────────┐
│Client│    │ Backend │    │ RabbitMQ │    │ Judger  │    │ Sandbox  │    │Storage │
└──┬───┘    └────┬────┘    └────┬─────┘    └────┬────┘    └────┬─────┘    └───┬────┘
   │             │              │               │              │              │
   │ Submit Code │              │               │              │              │
   │────────────▶│              │               │              │              │
   │             │              │               │              │              │
   │             │ Validate     │               │              │              │
   │             │──────────────│               │              │              │
   │             │              │               │              │              │
   │             │ Save Submission              │              │              │
   │             │──────────────│               │              │              │
   │             │              │               │              │              │
   │             │ Publish JudgeTask            │              │              │
   │             │─────────────▶│               │              │              │
   │             │              │               │              │              │
   │  202 Accepted              │               │              │              │
   │◀────────────│              │               │              │              │
   │             │              │               │              │              │
   │             │              │ Consume Task  │              │              │
   │             │              │──────────────▶│              │              │
   │             │              │               │              │              │
   │             │              │               │ Get TestData │              │
   │             │              │               │─────────────────────────────▶│
   │             │              │               │              │              │
   │             │              │               │◀─────────────────────────────│
   │             │              │               │              │              │
   │             │              │               │ Compile Code │              │
   │             │              │               │─────────────▶│              │
   │             │              │               │              │              │
   │             │              │               │◀─────────────│              │
   │             │              │               │              │              │
   │             │              │               │──────────────────────────────│
   │             │              │               │ For each TestCase:          │
   │             │              │               │                             │
   │             │              │               │ Run in Sandbox              │
   │             │              │               │─────────────▶│              │
   │             │              │               │              │              │
   │             │              │               │ Execute      │              │
   │             │              │               │              │──────────────▶
   │             │              │               │              │              │
   │             │              │               │              │◀─────────────│
   │             │              │               │              │              │
   │             │              │               │◀─────────────│              │
   │             │              │               │  Result      │              │
   │             │              │               │              │              │
   │             │              │               │ Compare/SPJ  │              │
   │             │              │               │──────────────│              │
   │             │              │──────────────────────────────│              │
   │             │              │               │              │              │
   │             │              │ Publish Result│              │              │
   │             │              │◀──────────────│              │              │
   │             │              │               │              │              │
   │             │ Update Submission            │              │              │
   │             │◀─────────────│               │              │              │
   │             │              │               │              │              │
   │ WebSocket   │              │               │              │              │
   │◀────────────│              │               │              │              │
   │  (Result)   │              │              │              │              │
```

## 7. API 设计规范

### 7.1 RESTful API 约定

```
基础路径: /api/v1

认证相关:
POST   /api/v1/auth/register              注册
POST   /api/v1/auth/login                 登录
POST   /api/v1/auth/logout                登出
POST   /api/v1/auth/refresh               刷新令牌
GET    /api/v1/auth/profile               获取个人信息
PUT    /api/v1/auth/profile               更新个人信息

题目相关:
GET    /api/v1/problems                   题目列表
GET    /api/v1/problems/{id}              题目详情
POST   /api/v1/problems                   创建题目
PUT    /api/v1/problems/{id}              更新题目
DELETE /api/v1/problems/{id}              删除题目
POST   /api/v1/problems/{id}/rejudge      重判题目

提交相关:
GET    /api/v1/submissions                提交列表
GET    /api/v1/submissions/{id}           提交详情
POST   /api/v1/submissions                创建提交
POST   /api/v1/submissions/{id}/rejudge   重判提交

比赛相关:
GET    /api/v1/contests                   比赛列表
GET    /api/v1/contests/{id}              比赛详情
POST   /api/v1/contests                   创建比赛
PUT    /api/v1/contests/{id}              更新比赛
POST   /api/v1/contests/{id}/register     报名比赛
GET    /api/v1/contests/{id}/standings    排行榜
GET    /api/v1/contests/{id}/problems     比赛题目

管理接口:
GET    /api/v1/admin/users                用户管理
GET    /api/v1/admin/roles                角色管理
GET    /api/v1/admin/permissions          权限管理
GET    /api/v1/admin/settings             系统设置
GET    /api/v1/admin/judgers              判题机管理
GET    /api/v1/admin/audit-logs           审计日志
```

### 7.2 响应格式

```json
// 成功响应
{
  "success": true,
  "data": { ... },
  "message": null,
  "timestamp": "2024-01-01T00:00:00Z"
}

// 分页响应
{
  "success": true,
  "data": {
    "items": [ ... ],
    "total": 100,
    "page": 1,
    "pageSize": 20,
    "totalPages": 5
  },
  "message": null,
  "timestamp": "2024-01-01T00:00:00Z"
}

// 错误响应
{
  "success": false,
  "data": null,
  "message": "错误信息",
  "errors": {
    "field": ["验证错误信息"]
  },
  "errorCode": "VALIDATION_ERROR",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

## 8. 判题器设计

### 8.1 沙箱隔离机制

```
┌─────────────────────────────────────────────────────────────────┐
│                        Judger Process                           │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐  │
│  │                    Sandbox Manager                         │  │
│  │                                                           │  │
│  │  ┌─────────────────────────────────────────────────────┐  │  │
│  │  │              nsjail / isolate                       │  │  │
│  │  │                                                     │  │  │
│  │  │  ┌─────────────────────────────────────────────┐   │  │  │
│  │  │  │           Isolated Container                 │   │  │  │
│  │  │  │                                             │   │  │  │
│  │  │  │  • PID Namespace    (进程隔离)              │   │  │  │
│  │  │  │  • NET Namespace    (网络隔离)              │   │  │  │
│  │  │  │  • MNT Namespace    (文件系统隔离)          │   │  │  │
│  │  │  │  • UTS Namespace    (主机名隔离)            │   │  │  │
│  │  │  │  • IPC Namespace    (进程间通信隔离)        │   │  │  │
│  │  │  │  • USER Namespace   (用户隔离)              │   │  │  │
│  │  │  │                                             │   │  │  │
│  │  │  │  • cgroups v2       (资源限制)              │   │  │  │
│  │  │  │    - CPU Time Limit                         │   │  │  │
│  │  │  │    - Memory Limit                           │   │  │  │
│  │  │  │    - Process Count                          │   │  │  │
│  │  │  │    - File Size                              │   │  │  │
│  │  │  │                                             │   │  │  │
│  │  │  │  • Seccomp          (系统调用过滤)          │   │  │  │
│  │  │  │                                             │   │  │  │
│  │  │  │  ┌───────────────────────────────────────┐  │   │  │  │
│  │  │  │  │         User Program                  │  │   │  │  │
│  │  │  │  │                                       │  │   │  │  │
│  │  │  │  │  stdin ◀── input.txt                 │  │   │  │  │
│  │  │  │  │  stdout ──▶ output.txt               │  │   │  │  │
│  │  │  │  │  stderr ──▶ error.txt                │  │   │  │  │
│  │  │  │  │                                       │  │   │  │  │
│  │  │  │  └───────────────────────────────────────┘  │   │  │  │
│  │  │  └─────────────────────────────────────────────┘   │  │  │
│  │  └─────────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### 8.2 支持的编程语言

| 语言 | 编译器/解释器 | 编译命令 | 运行命令 |
|------|--------------|----------|----------|
| C | GCC 13 | `gcc -O2 -std=c17 -o main main.c -lm` | `./main` |
| C++ | G++ 13 | `g++ -O2 -std=c++20 -o main main.cpp` | `./main` |
| Java | OpenJDK 21 | `javac Main.java` | `java Main` |
| Python | Python 3.12 | - | `python3 main.py` |
| Rust | Rustc 1.75 | `rustc -O -o main main.rs` | `./main` |
| Go | Go 1.21 | `go build -o main main.go` | `./main` |
| C# | .NET 8 | `dotnet build` | `dotnet run` |
| JavaScript | Node.js 20 | - | `node main.js` |

### 8.3 Special Judge 支持

```cpp
// testlib 风格 Special Judge 示例
#include "testlib.h"

int main(int argc, char* argv[]) {
    registerTestlibCmd(argc, argv);
    
    // inf: 输入文件
    // ouf: 用户输出
    // ans: 标准答案
    
    double expected = ans.readDouble();
    double actual = ouf.readDouble();
    
    if (abs(expected - actual) < 1e-6) {
        quitf(_ok, "Correct");
    } else {
        quitf(_wa, "Expected %.6f, got %.6f", expected, actual);
    }
    
    return 0;
}
```

## 9. 部署方案

### 9.1 Docker Compose (开发/小规模部署)

```yaml
version: '3.8'

services:
  frontend:
    build: ./AuroraJudge.Frontend
    ports:
      - "3000:80"
    depends_on:
      - backend

  backend:
    build: ./AuroraJudge.Backend
    ports:
      - "5000:80"
    environment:
      - ConnectionStrings__Default=Host=postgres;Database=aurorajudge;Username=postgres;Password=postgres
      - Redis__Connection=redis:6379
      - RabbitMQ__Host=rabbitmq
    depends_on:
      - postgres
      - redis
      - rabbitmq

  judger:
    build: ./AuroraJudge.Judger
    privileged: true
    environment:
      - RabbitMQ__Host=rabbitmq
    volumes:
      - testdata:/data/testdata
    depends_on:
      - rabbitmq

  postgres:
    image: postgres:16
    environment:
      - POSTGRES_DB=aurorajudge
      - POSTGRES_PASSWORD=postgres
    volumes:
      - pgdata:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    volumes:
      - redisdata:/data

  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "15672:15672"

volumes:
  pgdata:
  redisdata:
  testdata:
```

### 9.2 Kubernetes (生产部署)

```yaml
# 使用 Helm Chart 或 Kustomize 进行部署
# 支持水平扩展、自动伸缩、滚动更新
# 判题器使用 DaemonSet 或带有 GPU/特权的 Pod
```

## 10. 技术栈总结

| 层级 | 技术选型 |
|------|----------|
| 前端框架 | Vite + React 18 + TypeScript |
| UI 组件库 | Tailwind CSS + Shadcn/ui |
| 状态管理 | Zustand / TanStack Query |
| 代码编辑器 | Monaco Editor |
| 后端框架 | ASP.NET Core 8+ |
| ORM | Entity Framework Core |
| 身份认证 | ASP.NET Core Identity + JWT |
| 数据库 | PostgreSQL 16 |
| 缓存 | Redis 7 |
| 消息队列 | RabbitMQ / Redis Streams |
| 判题器 | .NET 8 Native AOT |
| 沙箱 | nsjail / isolate |
| 容器化 | Docker + Kubernetes |
| CI/CD | GitHub Actions |
| 监控 | Prometheus + Grafana |
| 日志 | Serilog + ELK Stack |
