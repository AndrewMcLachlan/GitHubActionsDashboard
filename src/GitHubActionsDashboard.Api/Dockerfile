FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY ./publish .
ENTRYPOINT ["dotnet", "GitHubActionsDashboard.Api.dll"]
