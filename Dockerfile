# Estágio 1: Base (O ambiente de execução)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Estágio 2: Build (O ambiente de compilação)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia apenas os arquivos de projeto primeiro para otimizar o cache.
COPY ["src/Fgc.Users.Api/Fgc.Users.Api.csproj", "src/Fgc.Users.Api/"]
COPY ["src/Fgc.Users.Application/Fgc.Users.Application.csproj", "src/Fgc.Users.Application/"]
COPY ["src/Fgc.Users.Domain/Fgc.Users.Domain.csproj", "src/Fgc.Users.Domain/"]
COPY ["src/Fgc.Users.Infrastructure/Fgc.Users.Infrastructure.csproj", "src/Fgc.Users.Infrastructure/"]
RUN dotnet restore "src/Fgc.Users.Api/Fgc.Users.Api.csproj"

# Copia o restante do código-fonte e compila o projeto.
COPY . .
WORKDIR "/src/src/Fgc.Users.Api"
RUN dotnet build "Fgc.Users.Api.csproj" -c Release -o /app/build

# Estágio 3: Publicação (Gera a versão final otimizada)
FROM build AS publish
RUN dotnet publish "Fgc.Users.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Estágio 4: Final (Cria a imagem final, a caixa que vai para a nuvem)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fgc.Users.Api.dll"]