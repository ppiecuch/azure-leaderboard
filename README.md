### Azure Leaderboard Solution

Simple one-file Leaderboard system based on Azure Functions (Notice, this branch is configured with __Azure Continuos Deployment__ - each change and commit is automatically uploaded and deployed to Azure Functions).

#### Building
  - ```azfunc start --build```: build and start local functions
  - ```dotnet build azleaderboard.csproj -t:Rebuild```: rebuild-only project locally
  - ```dotnet build azleaderboard.csproj -t:Rebuild -v:n```: rebuild project with verbose messages

  Local storage requires Docker build of __azurite__:

  ```docker build -t azure-storage/azurite:v2 .```

#### Reference
 1. https://docs.microsoft.com/en-us/gaming/azure/reference-architectures/leaderboard-non-relational
 2. https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-cosmosdb-v2-input?tabs=csharp
 3. https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-cosmosdb-v2-output?tabs=csharp
 4. https://codetraveler.io/2021/05/28/creating-azure-functions-using-net-5/

#### Examples
 1. https://github.com/PacktPublishing/Developing-Microsoft-Azure-Solutions/tree/master/ch08/LeaderboardFunction
 2. https://github.com/nguyenquyhy/Flight-Events/blob/main/FlightEvents.Data.AzureStorage/AzureTableLeaderboardStorage.cs
 3. https://github.com/christopherhouse/Cosmos-Functions-Demo

---
KomSoft Oprogramowanie (c) 2019,2020,2021,2022
