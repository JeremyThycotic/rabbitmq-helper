﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Thycotic.Logging;

namespace Thycotic.MemoryMq.Subsystem
{
    /// <summary>
    /// Message dispatcher
    /// </summary>
    public class MessageDispatcher : IDisposable
    {
        private readonly ExchangeDictionary _exchange;
        private readonly BindingDictionary _bindings;
        private readonly ClientDictionary _clientDictionary;
        private CancellationTokenSource _cts;
        private Task _monitoringTask;

        private readonly ILogWriter _log = Log.Get(typeof(MessageDispatcher));

        private LogCorrelation _correlation = LogCorrelation.Create();

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageDispatcher"/> class.
        /// </summary>
        /// <param name="exchange">The exchange dictionary.</param>
        /// <param name="bindings">The bindings.</param>
        /// <param name="clientDictionary">The client dictionary.</param>
        public MessageDispatcher(ExchangeDictionary exchange, BindingDictionary bindings, ClientDictionary clientDictionary)
        {
            _exchange = exchange;
            _bindings = bindings;
            _clientDictionary = clientDictionary;

        }

        private void MonitorAndDispatch()
        {
            do
            {
                if (!_exchange.IsEmpty)
                {
                    _exchange.Mailboxes.ToList().ForEach(mailbox =>
                    {
                        string queueName;
                        if (!_bindings.TryGetBinding(mailbox.RoutingSlip, out queueName))
                        {
                            //no binding for the routing slip
                            return;
                        }

                        if (mailbox.Queue.IsEmpty)
                        {
                            //nothing in the queue
                            return;
                        }

                        MemoryMqServerClientProxy clientProxy;
                        if (!_clientDictionary.TryGetClient(queueName, out clientProxy))
                        {
                            //no client for the queue
                            return;
                        }


                        MemoryMqDeliveryEventArgs body;
                        if (!mailbox.Queue.TryDequeue(out body))
                        {
                            //nothing in the queue
                            return;
                        }

                        clientProxy.Callback.SendMessage(body);
                    });

                }
                Thread.Sleep(100);

            } while (!_cts.IsCancellationRequested);
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            Stop();

            _log.Debug("Staring message monitoring");

            _cts = new CancellationTokenSource();
            _monitoringTask = Task.Factory.StartNew(MonitorAndDispatch);
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            if ((_cts == null) || (_monitoringTask == null))
            {
                return;
            }

            var drainTask = Task.Factory.StartNew(() =>
            {
                _log.Info("Draining exchange...");

                while (!_exchange.IsEmpty)
                {
                    _log.Info("Waiting for queues to drain...");
                    Thread.Sleep(1000);
                }
            });

            //TODO: Make configurable
            drainTask.Wait(TimeSpan.FromSeconds(30));

            _log.Debug("Stopping message monitoring");

            _cts.Cancel();

            _monitoringTask.Wait();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Stop();
        }
    }
}
