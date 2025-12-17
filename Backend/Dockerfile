# 构建阶段
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-alpine AS build
WORKDIR /src

# 复制项目文件
COPY ["AuroraJudge.Api/AuroraJudge.Api.csproj", "AuroraJudge.Api/"]
COPY ["AuroraJudge.Application/AuroraJudge.Application.csproj", "AuroraJudge.Application/"]
COPY ["AuroraJudge.Infrastructure/AuroraJudge.Infrastructure.csproj", "AuroraJudge.Infrastructure/"]
COPY ["AuroraJudge.Domain/AuroraJudge.Domain.csproj", "AuroraJudge.Domain/"]
COPY ["AuroraJudge.Shared/AuroraJudge.Shared.csproj", "AuroraJudge.Shared/"]

# 还原依赖
RUN dotnet restore "AuroraJudge.Api/AuroraJudge.Api.csproj"

# 复制所有源代码
COPY . .

# 构建
WORKDIR "/src/AuroraJudge.Api"
RUN dotnet build "AuroraJudge.Api.csproj" -c Release -o /app/build

# 发布
FROM build AS publish
RUN dotnet publish "AuroraJudge.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 运行阶段
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-alpine AS final
WORKDIR /app

# 安装依赖
RUN apk add --no-cache icu-libs

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# 复制发布文件
COPY --from=publish /app/publish .

# 创建非 root 用户
RUN adduser -D -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

EXPOSE 8080

ENTRYPOINT ["dotnet", "AuroraJudge.Api.dll"]
