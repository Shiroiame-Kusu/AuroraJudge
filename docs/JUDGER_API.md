# Judger API 文档

本文档描述 Judger 节点与 Backend 的通信协议。

## 概述

Judger 通过 HTTP API 与 Backend 通信，主要流程：
1. **注册** - 管理员在 Backend 注册新 Judger，获取 JudgerId 和 Secret
2. **连接** - Judger 启动时使用凭证连接 Backend
3. **心跳** - 定期发送心跳保持连接状态
4. **拉取任务** - 主动轮询获取待评测任务
5. **上报结果** - 评测完成后上报结果

## API 端点

### 1. 注册 Judger (管理员)

```http
POST /api/judger/register
Authorization: Bearer <admin_token>
Content-Type: application/json

{
    "name": "judger-1",
    "maxConcurrentTasks": 4,
    "supportedLanguages": ["c", "cpp", "java", "python", "go", "rust"]
}
```

**响应：**
```json
{
    "judgerId": "550e8400-e29b-41d4-a716-446655440000",
    "name": "judger-1",
    "secret": "abc123...",  // 仅显示一次！
    "message": "Judger 注册成功，请保存好 Secret，此密钥只显示一次"
}
```

### 2. 连接 Backend

```http
POST /api/judger/connect
Content-Type: application/json

{
    "judgerId": "550e8400-e29b-41d4-a716-446655440000",
    "secret": "abc123..."
}
```

**响应：**
```json
{
    "judgerId": "550e8400-e29b-41d4-a716-446655440000",
    "name": "judger-1",
    "maxConcurrentTasks": 4,
    "message": "连接成功"
}
```

### 3. 心跳

建议每 30 秒发送一次心跳。超过 1 分钟无心跳将被标记为离线。

```http
POST /api/judger/heartbeat
Content-Type: application/json

{
    "judgerId": "550e8400-e29b-41d4-a716-446655440000",
    "secret": "abc123..."
}
```

**响应：**
```json
{
    "status": "ok",
    "pendingTasks": 5,
    "currentTasks": 2
}
```

### 4. 获取任务

```http
POST /api/judger/fetch
Content-Type: application/json

{
    "judgerId": "550e8400-e29b-41d4-a716-446655440000",
    "secret": "abc123..."
}
```

**响应（有任务）：**
```json
{
    "hasTask": true,
    "task": {
        "taskId": "...",
        "submissionId": "...",
        "code": "#include <stdio.h>\nint main() {...}",
        "language": "cpp",
        "timeLimit": 1000,
        "memoryLimit": 256,
        "judgeMode": "Standard",
        "specialJudgeCode": null,
        "testCases": [
            {
                "order": 1,
                "inputPath": "problems/1001/testcases/1.in",
                "outputPath": "problems/1001/testcases/1.out",
                "score": 10
            }
        ]
    }
}
```

**响应（无任务）：**
```json
{
    "hasTask": false,
    "task": null
}
```

### 5. 上报结果

```http
POST /api/judger/report
Content-Type: application/json

{
    "judgerId": "550e8400-e29b-41d4-a716-446655440000",
    "secret": "abc123...",
    "submissionId": "...",
    "status": "Accepted",
    "score": 100,
    "timeUsed": 15,
    "memoryUsed": 1024,
    "compileMessage": null,
    "judgeMessage": null,
    "testResults": [
        {
            "order": 1,
            "status": "Accepted",
            "timeUsed": 15,
            "memoryUsed": 1024,
            "score": 10
        }
    ]
}
```

**状态枚举：**
- `Pending` - 等待评测
- `Judging` - 评测中
- `Accepted` - 通过
- `WrongAnswer` - 答案错误
- `TimeLimitExceeded` - 超时
- `MemoryLimitExceeded` - 超内存
- `RuntimeError` - 运行时错误
- `CompileError` - 编译错误
- `OutputLimitExceeded` - 输出超限
- `SystemError` - 系统错误
- `PartiallyAccepted` - 部分通过

### 6. 查看 Judger 状态 (管理员)

```http
GET /api/judger/status
Authorization: Bearer <admin_token>
```

**响应：**
```json
{
    "judgers": [
        {
            "id": "...",
            "name": "judger-1",
            "status": "Online",
            "currentTasks": 2,
            "maxConcurrentTasks": 4,
            "lastHeartbeat": "2024-01-01T00:00:00Z",
            "supportedLanguages": ["c", "cpp", "java", "python"]
        }
    ],
    "pendingTasks": 5
}
```

### 7. 移除 Judger (管理员)

```http
DELETE /api/judger/{judgerId}
Authorization: Bearer <admin_token>
```

## Judger 配置

Judger 端需要配置以下信息：

```json
// appsettings.json
{
    "Judger": {
        "Mode": "http",           // http 或 rabbitmq (兼容旧版)
        "Name": "judger-1",
        "WorkDir": "/tmp/aurora-judge",
        "MaxConcurrentTasks": 4,
        "BackendUrl": "http://backend:5000",
        "PollIntervalMs": 1000,
        "JudgerId": "从后台注册获取",
        "Secret": "从后台注册获取"
    }
}
```

## 测试数据获取

测试数据路径存储在 `testCases[].inputPath` 和 `testCases[].outputPath` 中。

Judger 需要从存储服务（MinIO 或本地文件系统）下载测试数据：
- 如果使用 MinIO：通过 MinIO API 下载
- 如果使用本地存储：直接读取文件

## 错误处理

- 认证失败返回 `401 Unauthorized`
- 当前任务数达到上限时，`fetch` 返回 `hasTask: false`
- Judger 离线（心跳超时）时，其正在运行的任务会被重新入队

## 任务重试

- 任务被分配后如果 Judger 离线，会自动重新入队
- 最多重试 3 次
- 超过重试次数的任务会被标记为失败
