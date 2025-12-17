# 构建阶段
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-alpine AS build
WORKDIR /src

# 复制项目文件
COPY ["AuroraJudge.Judger/AuroraJudge.Judger.csproj", "AuroraJudge.Judger/"]
COPY ["AuroraJudge.Judger.Contracts/AuroraJudge.Judger.Contracts.csproj", "AuroraJudge.Judger.Contracts/"]

# 还原依赖
RUN dotnet restore "AuroraJudge.Judger/AuroraJudge.Judger.csproj"

# 复制所有源代码
COPY . .

# 发布 Native AOT
WORKDIR "/src/AuroraJudge.Judger"
RUN dotnet publish "AuroraJudge.Judger.csproj" -c Release -o /app/publish

# 运行阶段
FROM alpine:3.19 AS final
WORKDIR /app

# 安装判题所需的编译器和运行时
RUN apk add --no-cache \
    gcc \
    g++ \
    libc-dev \
    openjdk17-jdk \
    python3 \
    libgcc \
    libstdc++ \
    && ln -s /usr/bin/python3 /usr/bin/python

# 安装 isolate 沙盒 (从源码编译)
RUN apk add --no-cache git make libcap-dev && \
    git clone https://github.com/ioi/isolate.git /tmp/isolate && \
    cd /tmp/isolate && \
    make install && \
    rm -rf /tmp/isolate && \
    apk del git make

# 复制发布文件
COPY --from=build /app/publish .

# 创建工作目录
RUN mkdir -p /tmp/aurora-judge && chmod 777 /tmp/aurora-judge

ENTRYPOINT ["./AuroraJudge.Judger"]
