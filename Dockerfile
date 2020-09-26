FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.sln .
COPY PayoutBot/*.csproj ./PayoutBot/
COPY PayoutBot.Discord/*.csproj ./PayoutBot.Discord/
RUN dotnet restore

# copy everything else and build app
COPY PayoutBot/. ./PayoutBot/
COPY PayoutBot.Discord/. ./PayoutBot.Discord/
WORKDIR /app/PayoutBot
RUN dotnet publish -c Release -o ../out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app
COPY --from=build /app/out ./
ENTRYPOINT ["dotnet", "PayoutBot.dll"]
