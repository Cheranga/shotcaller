﻿<div align="center">
<img src="shotcaller.png" alt="ShotCaller" width="150px"/>

# ShotCaller

Typed and Named Message Handlers for Azure Service Bus


[![Latest Release](https://img.shields.io/github/v/release/Cheranga/shotcaller?sort=semver)](https://github.com/Cheranga/shotcaller/releases/latest)
[![Build and Test](https://github.com/Cheranga/shotcaller/actions/workflows/ci-pipeline.yml/badge.svg)](https://github.com/Cheranga/shotcaller/actions/workflows/ci-pipeline.yml)
[![Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/Cheranga/39e81c066bc22668168352b8484ef7df/raw/coverage-badge.json)](https://github.com/Cheranga/shotcaller/actions/workflows/restore-build-test.yml)

</div>

# :tada: Welcome to ShotCaller

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

This allows you to define your message types and their destinations easily, 
and use them throughout your application without needing to worry about the 
underlying details of the Azure Service Bus.

:point_right: Support for multiple Azure Service Bus namespaces.

When you have multiple Azure Service Bus namespaces, you can register publishers for each namespace

:point_right: Totally upto the user how the connection to the service bus should be handled

The library does not dictate how you would like to connect to the Azure Service Bus.

Whether you want to use a connection string, managed identity, or any other method, is totally up to you.

When you're configuring a publisher set the `GetServiceBusClientFunc` on how you want to connect to the 
Azure Service Bus.

:point_right: Dependency injection support for easy registration and usage of publishers.

Using `Microsoft.Extensions.DependencyInjection`, you can easily register your publishers 
or the `IServiceBusFactory` to get the publishers.

:point_right: Discriminated union types for handling message publication results, allowing you to easily handle success and failure
cases.

The operations are always returned with `OperationResponse` type which is a discriminated union type.
Using it's `Result` property which is of type `OperationResult`, 
you can decide how to handle the result of the message publication.

If it was a success, you can log the success or do any other operation you want.

If it was a failure, whether to throw an exception or just log the error is totally up to you.

:point_right: Support for configuring publishers with options such as the destination queue or topic, message options,
serialization options, and more.

If you need more fine-grained control over the message publication, 
you can configure the publishers with options.

See example below on how to configure the publisher when the destination is session enabled

```csharp
var services = new ServiceCollection().AddLogging().RegisterServiceBus();

services
    .RegisterServiceBusPublisher<CreateOrderMessage>()
    .Configure(config =>
    {
        config.GetServiceBusClientFunc = () =>
            new ServiceBusClient("[CONNECTION STRING]");
        config.PublishTo = SessionOrdersQueue;
        config.SerializerOptions = _serializerOptions;
        // Configure the message options for session enabled queue
        config.MessageOptions = (message, busMessage) => busMessage.SessionId = message.SessionId;
    });


```

:point_right: Ability to publish messages as batches

This is the default behavior of the library.
When you publish messages, they are automatically batched together and sent to the Azure Service Bus.

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

## :dart::dart: Working with multiple Azure Service Bus Namespaces

There's nothing special about using multiple Azure Service Bus namespaces.
Because the approach taken in here, you just need to register the publisher with the appropriate namespace. 

```csharp

// Registering the Shotcaller Azure Service Bus library
var services = new ServiceCollection().AddLogging().RegisterServiceBus();

// Now you can start registering the publishers with different namespaces.
// Registering a publisher for a message type as a typed client.
// The publisher will use the connection string to connect to the Azure Service Bus namespace.
services
    .RegisterServiceBusPublisher<CreateOrderMessage>("A")
    .Configure(config =>
    {
        config.GetServiceBusClientFunc = () => new ServiceBusClient(connectionString1);
        config.PublishTo = JustOrdersQueue;
        config.SerializerOptions = _serializerOptions;
    });

// Registering a publisher for a message type as a named client.
// The publisher will use managed identity to connect to the Azure Service Bus namespace.
services
    .RegisterServiceBusPublisher<OrderCreatedEvent>("B")
    .Configure(config =>
    {
        config.GetServiceBusClientFunc = () => new ServiceBusClient("[azure service bus namespace name]", 
                                                                    new ManagedIdentityCredential());
        config.PublishTo = JustOrdersQueue;
        config.SerializerOptions = _serializerOptions;
    });

```
## :handshake: Contributing

We welcome contributions to ShotCaller! If you have ideas, suggestions, or bug fixes, 
please open an issue or submit a pull request.

:high_brightness: Once you clone the repository, make sure to run the below commands so the tooling is set up correctly:

```bash

# Installing the .NET tools, CSharpier for code formatting and Husky for git hooks
dotnet tool restore

# Installing the Git hooks for Husky
dotnet husky install

# Optional, making sure the git hooks are set up correctly
dotnet husky run
```

:high_brightness: Before you submit the PR, make sure to run the tests and ensure that the code is well-documented.

## :innocent: Icons
Icons are from [Juicy Fish](https://www.flaticon.com/free-icons/target)


