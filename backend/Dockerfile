FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TransLearner.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish TransLearner.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5171
EXPOSE 5171

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TransLearner.dll"]
