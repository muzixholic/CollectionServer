# 멀티 스테이지 빌드를 사용한 ASP.NET Core 10 Containerfile

# 빌드 스테이지
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# 프로젝트 파일 복사 및 복원
COPY ["src/CollectionServer.Api/CollectionServer.Api.csproj", "src/CollectionServer.Api/"]
COPY ["src/CollectionServer.Core/CollectionServer.Core.csproj", "src/CollectionServer.Core/"]
COPY ["src/CollectionServer.Infrastructure/CollectionServer.Infrastructure.csproj", "src/CollectionServer.Infrastructure/"]
RUN dotnet restore "src/CollectionServer.Api/CollectionServer.Api.csproj"

# 소스 코드 복사 및 빌드
COPY . .
WORKDIR "/src/src/CollectionServer.Api"
RUN dotnet build "CollectionServer.Api.csproj" -c Release -o /app/build

# 게시 스테이지
FROM build AS publish
RUN dotnet publish "CollectionServer.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 런타임 스테이지
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# 보안: 비 루트 사용자로 실행
RUN groupadd -r appuser && useradd -r -g appuser appuser
USER appuser

COPY --from=publish /app/publish .

# Health Check 구성
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CollectionServer.Api.dll"]
