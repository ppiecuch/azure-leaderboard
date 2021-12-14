using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;


[assembly: FunctionsStartup(typeof(Leaderboard.Startup))]


namespace Leaderboard.Model
{
    public class EventStopwatch
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public string LeaderboardName { get; set; }
        public string Name { get; set; }
        public string Remarks { get; set; }
        public DateTimeOffset AddedDateTime { get; set; }
        public DateTimeOffset? StartedDateTime { get; set; }
        public List<DateTimeOffset> LapsDateTime { get; set; } = new List<DateTimeOffset>();
        public DateTimeOffset? StoppedDateTime { get; set; }
    }

    public class LeaderboardRecord
    {
        public LeaderboardRecord()
        {

        }

        public LeaderboardRecord(EventStopwatch stopwatch, int leaderboardSubIndex, TimeSpan timeSinceLast, TimeSpan timeSinceStart)
        {
            EventId = stopwatch.EventId;
            LeaderboardName = stopwatch.LeaderboardName;
            SubIndex = leaderboardSubIndex;
            PlayerName = stopwatch.Name;
            Remarks = stopwatch.Remarks;
            Score = -(long)timeSinceLast.TotalMilliseconds;
            ScoreDisplay = $"{timeSinceLast.Hours:00}:{timeSinceLast.Minutes:00}:{timeSinceLast.Seconds:00}.{timeSinceLast.Milliseconds:000}";
            TimeSinceStart = (long)timeSinceStart.TotalMilliseconds;
            TimeSinceLast = (long)timeSinceLast.TotalMilliseconds;
        }

        public Guid EventId { get; set; }
        public string PlayerName { get; set; }
        public string Remarks { get; set; }
        public string LeaderboardName { get; set; }
        public int SubIndex { get; set; }
        public long Score { get; set; }
        public string ScoreDisplay { get; set; }
        public long TimeSinceStart { get; set; }
        public long TimeSinceLast { get; set; }
    }

    public class LeaderboardRecordEntity : TableEntity
    {
        public LeaderboardRecordEntity()
        {
        }

        public LeaderboardRecordEntity(LeaderboardRecord record)
        {
            PartitionKey = record.EventId.ToString("N");
            RowKey = Guid.NewGuid().ToString("N");
            EventId = record.EventId;
            PlayerName = record.PlayerName;
            LeaderboardName = record.LeaderboardName;
            SubIndex = record.SubIndex;
            Score = record.Score;
            ScoreDisplay = record.ScoreDisplay;
            TimeSinceStart = record.TimeSinceStart;
            TimeSinceLast = record.TimeSinceLast;
        }

        public LeaderboardRecord ToRecord()
        {
            return new LeaderboardRecord
            {
                EventId = EventId,
                LeaderboardName = LeaderboardName,
                SubIndex = SubIndex,
                PlayerName = PlayerName,
                Score = Score,
                ScoreDisplay = ScoreDisplay,
                TimeSinceStart = TimeSinceStart,
                TimeSinceLast = TimeSinceLast
            };
        }

        public Guid EventId { get; set; }
        public string PlayerName { get; set; }
        public string LeaderboardName { get; set; }
        public int SubIndex { get; set; }
        public long Score { get; set; }
        public string ScoreDisplay { get; set; }
        public long TimeSinceStart { get; set; }
        public long TimeSinceLast { get; set; }
    }
}


namespace Leaderboard.Services
{
    public class LeaderboardUpdateService
    {
        private readonly string _databaseName;
        private readonly string _containerName;
        private readonly Uri _containerUri;

        public LeaderboardUpdateService(string databaseName, string containerName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentException(nameof(databaseName));
            }

            _databaseName = databaseName;

            if (string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentException(nameof(containerName));
            }

            _containerName = containerName;
            _containerUri = UriFactory.CreateDocumentCollectionUri(_databaseName, _containerName);
        }
    }
}


namespace Leaderboard
{
    public static class GetLeaderboardRecordById
    {
        [FunctionName("GetLeaderboardRecordById")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "game/{category}/{id}")] HttpRequest req,
            [CosmosDB(databaseName:"%cosmosDatabaseName%",
                collectionName: "%cosmosContainerName%",
                ConnectionStringSetting = "cosmosConnectionString",
                Id = "{id}",
                PartitionKey = "{category}")] Model.LeaderboardRecord record,
                ILogger log)
        {
            return new OkObjectResult(record);
        }
    }
}


namespace Leaderboard
{
    public static class GetTopLeaderboardRecords
    {
        [FunctionName("GetTopLeaderboardRecords")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            [CosmosDB(ConnectionStringSetting = "cosmosConnectionString")] IDocumentClient documentClient,
            ILogger log)
        {
            var database = Environment.GetEnvironmentVariable("cosmosDatabaseName");
            var container = Environment.GetEnvironmentVariable("cosmosContainerName");
            var containerUri = UriFactory.CreateDocumentCollectionUri(database, container);

            string continuationToken = null;
            var documents = new List<Model.LeaderboardRecord>();

            do
            {
                var feed = await documentClient.ReadDocumentFeedAsync(containerUri,
                    new FeedOptions {MaxItemCount = 25, RequestContinuation = continuationToken});
                continuationToken = feed.ResponseContinuation;

                foreach (var doc in feed)
                {
                    var docString = doc.ToString();
                    var product = JsonConvert.DeserializeObject<Model.LeaderboardRecord>(docString);
                    documents.Add(product);
                }

            } while (continuationToken != null);


            return new OkObjectResult(documents);
        }
    }
}


namespace Leaderboard
{
    public static class CreateLeaderboardRecord
    {
        [FunctionName("CreateLeaderboardRecord")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, methods: "post", Route = null)]
            HttpRequest req,
            [CosmosDB(databaseName: "%cosmosDatabaseName%",
                collectionName: "%ordersContainerName%",
                ConnectionStringSetting = "cosmosConnectionString")] IAsyncCollector<Model.LeaderboardRecord> recordToSave,
                ILogger log)
        {
            var record = JsonConvert.DeserializeObject<Model.LeaderboardRecord>(await req.ReadAsStringAsync());
            await recordToSave.AddAsync(record);

            return new NoContentResult();
        }
    }
}


namespace Leaderboard
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var database = Environment.GetEnvironmentVariable("cosmosDatabaseName");
            var container = Environment.GetEnvironmentVariable("cosmosContainerName");
            builder.Services.AddLogging();
            builder.Services.AddSingleton<Services.LeaderboardUpdateService>(_ => new Services.LeaderboardUpdateService(database, container));
        }
    }
}
