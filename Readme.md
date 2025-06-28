<div align="center">
<img src="target.png" alt="ShotCaller" width="150px"/>

# ShotCaller

Typed and Named Message Handlers for Azure Service Bus


[![Latest Release](https://img.shields.io/github/v/release/Cheranga/shotcaller?sort=semver)](https://github.com/Cheranga/shotcaller/releases/latest)
[![Build and Test](https://github.com/Cheranga/shotcaller/actions/workflows/ci-pipeline.yml/badge.svg)](https://github.com/Cheranga/shotcaller/actions/workflows/ci-pipeline.yml)
[![Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/Cheranga/39e81c066bc22668168352b8484ef7df/raw/coverage-badge.json)](https://github.com/Cheranga/shotcaller/actions/workflows/restore-build-test.yml)

</div>

# :tada: Welcome to Shotcaller

Shotcaller for Azure Service Bus is simply a library to use typed or named message handlers.
In V1, we introduce the typed and named publishers, and in a later version we will include the
support for both typed and named readers.

Inspiration for this library comes from Microsoft's typed and named HTTP clients.
When using the HTTP client library, you are given the flexibility to use these client types
appropriately. Even providing a factory operation through the `IHttpClientFactory` interface.

## :books: Context

When using Azure Service Bus, you often need to send messages to a queue or topic.

There can be occurrences where you integrate with multiple queues or topics, or even sometimes
to integrate with multiple Azure Service Bus namespaces.

Using typed, named publishers allows you to define your message types and their destinations easily, and use
them throughout your application without needing to worry about the underlying details of the Azure Service Bus.

## :sparkles: Features

:point_right: Typed and named message publishers for a given message type.

:point_right: Support for multiple Azure Service Bus namespaces.

:point_right: Support using connection strings or any `TokenCredential` implementation to authenticate with 
Azure Service Bus.

:point_right: Dependency injection support for easy registration and usage of publishers.

:point_right: Discriminated union types for handling message publication results, allowing you to easily handle success and failure
cases.

:point_right: Support for configuring publishers with options such as the destination queue or topic, message options,
serialization options, and more.

:point_right: Ability to publish messages as batches

## :runner: Getting Started

* Install the NuGet package `Shotcaller.Azure.ServiceBus.Messaging`

* Register your message publishers in the dependency injection container.

```csharp

// Registering the Shotcaller Azure Service Bus library
var services = new ServiceCollection().RegisterServiceBus();

// Configure a typed publisher
services
    .RegisterServiceBusPublisher<CreateOrderMessage>()
    .Configure(config =>
    {
        config.GetServiceBusClientFunc = () =>
            new ServiceBusClient(_serviceBusFixture.GetConnectionString());
        config.PublishTo = SessionOrdersQueue;
    });

// Configure a named publisher
services
    .RegisterServiceBusPublisher<OrderCreatedEvent>("order created event publisher")
    .Configure(config =>
    {
        config.GetServiceBusClientFunc = () =>
            new ServiceBusClient(_serviceBusFixture.GetConnectionString());
        config.PublishTo = SessionOrdersQueue;
    });

```

* Inject the publishers into your services or controllers.

```csharp

internal class OrderPublisherBackgroundService(
    IServiceBusPublisher<CreateOrderMessage> orderPublisher,
    ILogger<OrderPublisherBackgroundService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var order = new CreateOrderMessage
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow
        };
        
        await orderPublisher.PublishAsync(order, stoppingToken);
    }
}
```

You can also use the factory method to get registered publishers either by type or by name.

```csharp

internal class OrderPublisherBackgroundService(
    IServiceBusFactory factory,
    ILogger<OrderPublisherBackgroundService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Get the publishers
        var orderPublisher = factory.GetPublisher<CreateOrderMessage>();
        var orderCreatedPublisher = factory.GetPublisher<OrderCreatedEvent>("order created event publisher");
        
        var order = new CreateOrderMessage
        {
            OrderId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            OrderDate = DateTime.UtcNow
        };
        
        var orderCreated = new OrderCreatedEvent
        {
            OrderId = order.OrderId,
            PublishedAt = DateTime.UtcNow
        };
        
        
        await orderPublisher.PublishAsync(order, stoppingToken);
        await orderCreatedPublisher.PublishAsync(orderCreated, stoppingToken);
    }
}

```

## :dart: Handling Message Publications

We are using discriminated union types to return the result of the message publication.

```csharp

var operation = await orderPublisher.PublishAsync(orders, stoppingToken);
_ = operation.Result switch
{
    SuccessResult _ => LogSuccess(topicName),
    FailedResult failure => LogFailure(topicName, failure),
    _ => LogError(topicName),
};

```

## :innocent: Icons
Icons are from [Juicy Fish](https://www.flaticon.com/free-icons/target)


