﻿namespace ShotCaller.Azure.ServiceBus.Messaging.Models;

internal static class ErrorMessages
{
    public const string MessagePublishError = "An error occurred while publishing the message to the service bus.";
    public const string TooManyMessagesInBatch = "Too many messages in the batch";
}
