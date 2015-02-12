﻿namespace Thycotic.MessageQueueClient.QueueClient
{
    /// <summary>
    /// Interface for an exchange provider
    /// </summary>
    public interface IExchangeProvider
    {
        /// <summary>
        /// Gets the current change.
        /// </summary>
        /// <returns></returns>
        string GetCurrentChange();

    }
}