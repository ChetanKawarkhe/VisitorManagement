using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

var client = new QueueClient(
    "UseDevelopmentStorage=true",
    "visitor-created",
    new QueueClientOptions
    {
        MessageEncoding = QueueMessageEncoding.Base64
    });

if (!await client.ExistsAsync())
{
    Console.WriteLine("Queue does not exist.");
    return;
}

if (args.Length > 0 &&
    args[0].Equals("send-invalid", StringComparison.OrdinalIgnoreCase))
{
    var invalidEvent = new
    {
        MessageId = Guid.NewGuid(),
        CorrelationId = Guid.NewGuid().ToString(),
        VisitorId = -1,
        EventType = "VisitorCreated",
        OccurredAt = DateTime.UtcNow
    };

    var messageBody = System.Text.Json.JsonSerializer.Serialize(
        invalidEvent);

    await client.SendMessageAsync(messageBody);

    Console.WriteLine("Invalid test message sent successfully.");
    Console.WriteLine("Expected: 5 failed attempts, then poison queue.");

    return;
}

if (args.Length > 0 &&
    args[0].Equals("send", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length != 3 ||
        !Guid.TryParse(args[1], out var messageId) ||
        !int.TryParse(args[2], out var visitorId) ||
        visitorId <= 0)
    {
        Console.WriteLine(
            "Usage: dotnet run -- send <MessageId> <VisitorId>");
        return;
    }

    var visitorEvent = new
    {
        MessageId = messageId,
        CorrelationId = Guid.NewGuid().ToString(),
        VisitorId = visitorId,
        EventType = "VisitorCreated",
        OccurredAt = DateTime.UtcNow
    };

    var messageBody = System.Text.Json.JsonSerializer.Serialize(
        visitorEvent);

    await client.SendMessageAsync(messageBody);

    Console.WriteLine("Duplicate test message sent successfully.");
    Console.WriteLine($"MessageId: {messageId}");
    Console.WriteLine($"VisitorId: {visitorId}");

    return;
}

PeekedMessage[] messages = (await client.PeekMessagesAsync(
    maxMessages: 10)).Value;

if (messages.Length == 0)
{
    Console.WriteLine(
        "Queue exists, but no messages are currently visible.");
    return;
}

foreach (var message in messages)
{
    Console.WriteLine(message.Body.ToString());
}