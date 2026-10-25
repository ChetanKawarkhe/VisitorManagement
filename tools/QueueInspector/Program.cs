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

PeekedMessage[] messages = (await client.PeekMessagesAsync(
    maxMessages: 10)).Value;

if (messages.Length == 0)
{
    Console.WriteLine("Queue exists, but no messages are currently visible.");
    return;
}

foreach (var message in messages)
{
    Console.WriteLine(message.Body.ToString());
}