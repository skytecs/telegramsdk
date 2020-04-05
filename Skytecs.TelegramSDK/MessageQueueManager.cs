using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Skytecs.TelegramSDK
{
    class MessageQueueManager : BackgroundService
    {
        private readonly ConcurrentDictionary<int, MessageQueue> _sessions = new ConcurrentDictionary<int, MessageQueue>();
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MessageQueueManager> _logger;

        public MessageQueueManager(IServiceProvider serviceProvider, ILogger<MessageQueueManager> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task OnUpdate(Update payload)
        {
            var message = payload.Message ?? payload.EditedMessage;

            if (message != null)
            {

                var session = _sessions.GetOrAdd(message.Chat.Id,
                    key => ActivatorUtilities.CreateInstance<MessageQueue>(_serviceProvider.CreateScope().ServiceProvider));

                session.Push(payload);
            }
            else
            {
                var click = payload.CallbackQuery;

                var session = _sessions.GetOrAdd(click.Message.Chat.Id,
                    key => ActivatorUtilities.CreateInstance<MessageQueue>(_serviceProvider.CreateScope().ServiceProvider));

                session.Push(payload);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var sessionKey in _sessions.Keys)
                {
                    if (_sessions.TryGetValue(sessionKey, out var value))
                    {
                        try
                        {
                            await value.HandleMessage();
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Exception was thrown by the message handler");
                        }
                    }
                }

                await Task.Delay(100, stoppingToken);
            }
        }

        class MessageQueue
        {
            private object _sync = new object();
            private bool _isBusy;
            private readonly SortedList<int, Update> _messages = new SortedList<int, Update>();
            private readonly IServiceProvider _scope;

            public MessageQueue(IServiceProvider scope)
            {
                _scope = scope ?? throw new ArgumentNullException(nameof(scope));
            }

            public void Push(Update update)
            {
                lock (_sync)
                {
                    _messages.Add(update.UpdateId, update);
                }
            }

            public Update Pull()
            {
                if (_messages.Keys.Count > 0)
                {
                    lock (_sync)
                    {
                        if (_messages.Keys.Count > 0)
                        {
                            var key = _messages.Keys[0];

                            var value = _messages[key];

                            _messages.Remove(key);

                            return value;
                        }
                    }
                }

                return null;
            }

            public async Task HandleMessage()
            {
                if (_isBusy)
                {
                    return;
                }

                var update = Pull();

                if (update != null)
                {
                    _isBusy = true;

                    var handler = _scope.GetService<ITelegramCallback>();

                    await handler.OnUpdate(update);

                    _isBusy = false;
                }
            }
        }
    }

    //interface IBackgroundServiceAccessor<TBackgroundService>
    //    where TBackgroundService : IHostedService
    //{
    //    TBackgroundService Service { get; }
    //}

    class BackgroundServiceAccessor<TBackgroundService> //: IBackgroundServiceAccessor<TBackgroundService>
        where TBackgroundService : IHostedService
    {
        public BackgroundServiceAccessor(IEnumerable<IHostedService> hostedServices)
        {
            Service = hostedServices == null ? 
                throw new ArgumentNullException(nameof(hostedServices)) 
                : hostedServices.OfType<TBackgroundService>().FirstOrDefault();
        }

        public TBackgroundService Service { get; }
    }
}
