FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-buster AS build
WORKDIR /src
COPY ["BaseMiCakeApplication/BaseMiCakeApplication.csproj", "BaseMiCakeApplication/"]
RUN dotnet restore "BaseMiCakeApplication/BaseMiCakeApplication.csproj"
COPY . .
WORKDIR "/src/BaseMiCakeApplication"
RUN dotnet build "BaseMiCakeApplication.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BaseMiCakeApplication.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BaseMiCakeApplication.dll"]