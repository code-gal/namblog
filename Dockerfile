# 多阶段构建：先构建后端，再整合前端
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# 复制 csproj 文件并还原依赖（利用 Docker 缓存层）
COPY ["NamBlog.API/NamBlog.API.csproj", "NamBlog.API/"]
RUN dotnet restore "NamBlog.API/NamBlog.API.csproj"

# 复制所有源代码
COPY NamBlog.API/ NamBlog.API/

# 构建应用
WORKDIR /src/NamBlog.API
RUN dotnet build "NamBlog.API.csproj" -c Release -o /app/build

# 发布应用
FROM build AS publish
RUN dotnet publish "NamBlog.API.csproj" -c Release -o /app/publish --no-restore

# 运行时阶段：使用轻量级运行时镜像
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

# 复制发布的应用
COPY --from=publish /app/publish .

# 复制前端静态文件到 wwwroot
COPY NamBlog.Web/ ./wwwroot/

# 复制默认配置和资源到 wwwroot（首次启动时会被移动到 data 目录）
COPY NamBlog.API/wwwroot/config/ ./wwwroot/config/
COPY NamBlog.API/wwwroot/images/ ./wwwroot/images/

# 创建数据目录（供挂载）
RUN mkdir -p /app/data

# 暴露端口
EXPOSE 8080

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

# 启动应用
ENTRYPOINT ["dotnet", "NamBlog.API.dll"]
