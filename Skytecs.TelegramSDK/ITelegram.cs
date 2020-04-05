using System.Threading.Tasks;

namespace Skytecs.TelegramSDK {
    public interface ITelegram {
        Task<bool> SetWebhook(SetWebhookRequest request);
        Task<Message> SendMessage(SendMessageRequest request);
        Task<WebhookInfo> GetWebhookInfo();
        string GetToken();
    }
}