using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Skytecs.TelegramSDK
{
    public static class TelegramExtensions
    {
        public static IServiceCollection AddTelegram<THandler>(this IServiceCollection services, Action<TelegramConnectionSettings> configureConnection)
            where THandler : class, ITelegramCallback
        {
            var settings = new TelegramConnectionSettings();

            configureConnection(settings);

            var serviceCollection = (services ?? throw new ArgumentNullException(nameof(services)))
                   .AddSingleton<IHostedService, MessageQueueManager>()
                   .AddSingleton<ITelegram>(s => new Telegram(settings.Token))
                   .AddSingleton(settings)
                   .AddTransient<BackgroundServiceAccessor<MessageQueueManager>>()
                   .AddTransient<ITelegramCallback, THandler>();

            return serviceCollection;
        }

        public static IApplicationBuilder UseTelegram(this IApplicationBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            var api = builder.ApplicationServices.GetService<ITelegram>();
            var settings = builder.ApplicationServices.GetService<TelegramConnectionSettings>();

            builder.Map(new Uri(settings.WebhookUrl).AbsolutePath, TelegramMiddleware.Map);

            api.SetWebhook(new SetWebhookRequest { Url = settings.WebhookUrl });

            return builder;
        }
    }
}
