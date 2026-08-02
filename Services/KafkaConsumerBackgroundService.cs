using System.Text.Json;
using Confluent.Kafka;
using Finefolio.ValuationApi.Models;

namespace Finefolio.ValuationApi.Services;

public class KafkaConsumerBackgroundService : BackgroundService
{
    private const string NetIncomeTopic = "finefolio.net-income";
    private const string AssetFundamentalsTopic = "finefolio.asset-fundamentals";

    private readonly IValuationService _valuationService;
    private readonly IAssetFundamentalsService _assetFundamentalsService;
    private readonly ILogger<KafkaConsumerBackgroundService> _logger;
    private readonly string _bootstrapServers;
    private readonly string _groupId;

    public KafkaConsumerBackgroundService(
        IValuationService valuationService,
        IAssetFundamentalsService assetFundamentalsService,
        ILogger<KafkaConsumerBackgroundService> logger,
        IConfiguration configuration)
    {
        _valuationService = valuationService;
        _assetFundamentalsService = assetFundamentalsService;
        _logger = logger;
        _bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        _groupId = configuration["Kafka:ConsumerGroupId"] ?? "valuation-api-consumer-group";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = true,
            SessionTimeoutMs = 10000,
            HeartbeatIntervalMs = 3000,
            ApiVersionRequest = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
            {
                _logger.LogError("Kafka consumer error: {Reason}. IsFatal: {IsFatal}", error.Reason, error.IsFatal);
            })
            .SetStatisticsHandler((_, json) =>
            {
                _logger.LogInformation("Kafka consumer statistics: {Statistics}", json);
            })
            .SetLogHandler((_, log) =>
            {
                _logger.LogInformation("Kafka client log [{Level}] {Message}", log.Level, log.Message);
            })
            .Build();

        consumer.Subscribe(new[] { NetIncomeTopic, AssetFundamentalsTopic });

        _logger.LogInformation(
            "Kafka consumer started. GroupId: {GroupId}, Topics: {Topics}, BootstrapServers: {BootstrapServers}",
            _groupId,
            string.Join(", ", new[] { NetIncomeTopic, AssetFundamentalsTopic }),
            _bootstrapServers);

        try
        {
            using var adminClient = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = _bootstrapServers
            }).Build();

            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(15));
            _logger.LogInformation(
                "Kafka metadata loaded. Brokers: {Brokers}, Topics: {Topics}",
                string.Join(", ", metadata.Brokers.Select(b => $"{b.Host}:{b.Port}")),
                string.Join(", ", metadata.Topics.Select(t => t.Topic).OrderBy(t => t)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Kafka metadata for broker {BootstrapServers}", _bootstrapServers);
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromSeconds(10));
                    if (result == null)
                    {
                        _logger.LogWarning(
                            "Kafka poll timed out after 10s. No new messages on topics {Topics}. Broker: {BootstrapServers}, Group: {GroupId}",
                            string.Join(", ", new[] { NetIncomeTopic, AssetFundamentalsTopic }),
                            _bootstrapServers,
                            _groupId);
                        continue;
                    }

                    _logger.LogInformation(
                        "Kafka message received. Topic: {Topic}, Partition: {Partition}, Offset: {Offset}, Key: {Key}, ValueLength: {ValueLength}",
                        result.Topic,
                        result.Partition.Value,
                        result.Offset.Value,
                        result.Message.Key,
                        result.Message.Value?.Length ?? 0);

                    await HandleMessageAsync(result, stoppingToken);
                    consumer.Commit(result);

                    _logger.LogInformation(
                        "Kafka message committed. Topic: {Topic}, Partition: {Partition}, Offset: {Offset}",
                        result.Topic,
                        result.Partition.Value,
                        result.Offset.Value);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume failed for topic {Topic}", ex.ConsumerRecord?.Topic ?? "unknown");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error while consuming Kafka message");
                }
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("Kafka consumer closed.");
        }
    }

    private async Task HandleMessageAsync(ConsumeResult<string, string> result, CancellationToken cancellationToken)
    {
        var key = result.Message.Key;
        var payload = result.Message.Value;

        _logger.LogInformation(
            "Processing Kafka message. Topic: {Topic}, Key: {Key}, PayloadLength: {PayloadLength}",
            result.Topic,
            key,
            payload?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(payload))
        {
            _logger.LogWarning("Empty payload received on topic {Topic} with key {Key}", result.Topic, key);
            return;
        }

        if (!TryParseKey(key, out var exchange, out var ticker))
        {
            _logger.LogWarning(
                "Skipping Kafka message with invalid key '{Key}' on topic {Topic}",
                key,
                result.Topic);
            return;
        }

        _logger.LogInformation("Parsed Kafka key for processing. Exchange: {Exchange}, Ticker: {Ticker}, Topic: {Topic}", exchange, ticker, result.Topic);

        switch (result.Topic)
        {
            case NetIncomeTopic:
                await HandleNetIncomeAsync(exchange, ticker, payload);
                break;

            case AssetFundamentalsTopic:
                await HandleAssetFundamentalsAsync(exchange, ticker, payload);
                break;

            default:
                _logger.LogWarning("Unhandled Kafka topic: {Topic}", result.Topic);
                break;
        }
    }

    private async Task HandleNetIncomeAsync(string exchange, string ticker, string payload)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<NetIncomeUpdateDto>(payload, JsonOptions());
            if (dto == null || dto.Year == null || dto.Value == null)
            {
                _logger.LogWarning("Ignoring invalid net income payload for {Exchange}.{Ticker}: {Payload}", exchange, ticker, payload);
                return;
            }

            var updated = await _valuationService.AddOrUpdateNetIncomeAsync(exchange, ticker, dto.Year.Value, dto.Value.Value);
            if (updated)
            {
                _logger.LogInformation("Applied net-income update for {Exchange}.{Ticker} year {Year}", exchange, ticker, dto.Year.Value);
            }
            else
            {
                _logger.LogWarning("Net-income update ignored because asset was not found: {Exchange}.{Ticker}", exchange, ticker);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON for net income topic. Key: {Exchange}.{Ticker}. Payload: {Payload}", exchange, ticker, payload);
        }
    }

    private async Task HandleAssetFundamentalsAsync(string exchange, string ticker, string payload)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<AssetFundamentalsUpdateDto>(payload, JsonOptions());
            if (dto == null || !dto.HasAnyValue)
            {
                _logger.LogWarning("Ignoring invalid asset fundamentals payload for {Exchange}.{Ticker}: {Payload}", exchange, ticker, payload);
                return;
            }

            var updated = await _assetFundamentalsService.UpdateFundamentalsAsync(exchange, ticker, dto);
            if (updated)
            {
                _logger.LogInformation("Applied asset fundamentals update for {Exchange}.{Ticker}", exchange, ticker);
            }
            else
            {
                _logger.LogWarning("Asset fundamentals update ignored because asset was not found: {Exchange}.{Ticker}", exchange, ticker);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON for asset fundamentals topic. Key: {Exchange}.{Ticker}. Payload: {Payload}", exchange, ticker, payload);
        }
    }

    private static bool TryParseKey(string? key, out string exchange, out string ticker)
    {
        exchange = string.Empty;
        ticker = string.Empty;

        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        var parts = key.Split('.', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        exchange = parts[0];
        ticker = parts[1];
        return !string.IsNullOrWhiteSpace(exchange) && !string.IsNullOrWhiteSpace(ticker);
    }

    private static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }
}
