using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Skytecs.TelegramSDK
{
    class Telegram : ITelegram
    {
        private readonly string _token;

        private readonly JsonSerializer _serializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Ignore,
        };

        public Telegram(string token)
        {
            _token = string.IsNullOrWhiteSpace(token) ? throw new ArgumentNullException(nameof(token)) : token;
        }

        public async Task<bool> SetWebhook(SetWebhookRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("cache-control", "no-cache");

                using (var stringWriter = new StringWriter())
                {
                    _serializer.Serialize(stringWriter, request);
                    var content = new StringContent(stringWriter.ToString(), Encoding.UTF8, "application/json");

                    try
                    {
                        var result = await client.PostAsync($"https://tapi.skytecs.ru/bot{_token}/setWebhook", content);

                        using (var stream = await result.Content.ReadAsStreamAsync())
                        using (var textReader = new StreamReader(stream))
                        using (var jsonReader = new JsonTextReader(textReader))
                        {
                            var response = _serializer.Deserialize<TelegramResponse<bool>>(jsonReader);

                            if (!response.Ok)
                            {
                                throw new InvalidOperationException(response.Description);
                            }

                            return response.Result;
                        }
                    }
                    catch(Exception ex)
                    {
                        throw ex;
                    }
                }
            }
        }

        public async Task<WebhookInfo> GetWebhookInfo()
        {
            using (var client = new HttpClient())
            {
                var result = await client.GetAsync($"https://tapi.skytecs.ru/bot{_token}/getWebhookInfo");

                using (var stream = await result.Content.ReadAsStreamAsync())
                using (var textReader = new StreamReader(stream))
                using (var jsonReader = new JsonTextReader(textReader))
                {
                    var response = _serializer.Deserialize<TelegramResponse<WebhookInfo>>(jsonReader);

                    if (!response.Ok)
                    {
                        throw new InvalidOperationException(response.Description);
                    }

                    return response.Result;
                }
            }

        }

        public async Task<Message> SendMessage(SendMessageRequest request)
        {
            using (var client = new HttpClient())
            {
                using (var stringWriter = new StringWriter())
                {
                    _serializer.Serialize(stringWriter, request);
                    var content = new StringContent(stringWriter.ToString(), Encoding.UTF8, "application/json");
                    var result = await client.PostAsync($"https://tapi.skytecs.ru/bot{_token}/sendMessage", content);

                    using (var stream = await result.Content.ReadAsStreamAsync())
                    using (var textReader = new StreamReader(stream))
                    using (var jsonReader = new JsonTextReader(textReader))
                    {
                        var response = _serializer.Deserialize<TelegramResponse<Message>>(jsonReader);

                        if (!response.Ok)
                        {
                            throw new InvalidOperationException(response.Description);
                        }

                        return response.Result;
                    }
                }
            }
        }

        public string GetToken()
        {
            return this._token;
        }
    }

    public class SetWebhookRequest
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("max_connections")]
        public int? MaxConnections { get; set; }
    }

    public class SendMessageRequest
    {
        [JsonProperty("chat_id")]
        public int ChatId { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("parse_mode")]
        public ParseMode? ParseMode { get; set; }

        [JsonProperty("disable_web_page_preview")]
        public bool? DisableWebPagePreview { get; set; }

        [JsonProperty("disable_notification")]
        public bool? DisableNotification { get; set; }

        [JsonProperty("reply_to_message_id")]
        public int? ReplyToMessageId { get; set; }

        [JsonProperty("reply_markup")]
        public ReplayMarkup ReplayMarkup { get; set; }
    }

    public abstract class ReplayMarkup
    {

    }

    public class ReplyKeyboardMarkup : ReplayMarkup
    {
        [JsonProperty("keyboard")]
        public ICollection<ICollection<KeyboardButton>> Keyboard { get; set; }

        [JsonProperty("resize_keyboard")]
        public bool? ResizeKeyboard {get;set;}

        [JsonProperty("one_time_keyboard")]
        public bool? OneTimeKeyboard { get; set; }

        [JsonProperty("selective")]
        public bool? Selective { get; set; }
    }

    public class InlineKeyboardMarkup : ReplayMarkup
    {
        [JsonProperty("inline_keyboard")]
        public ICollection<ICollection<InlineKeyboardButton>> InlineKeyboard { get; set; }
    }

    public class InlineKeyboardButton
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("callback_data")]
        public string CallbackData { get; set; }

        [JsonProperty("switch_inline_query")]
        public string SwitchInlineQuery { get; set; }

        [JsonProperty("switch_inline_query_current_chat")]
        public string SwitchInlineQueryCurrentChat { get; set; }

        [JsonProperty("callback_game")]
        public CallbackGame CallbackGame { get; set; }

        [JsonProperty("pay")]
        public bool? Pay { get; set; }
    }

    public class CallbackGame
    {

    }

    public class KeyboardButton
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("request_contact")]
        public bool? RequestContact { get; set; }

        [JsonProperty("request_location")]
        public bool? RequestLocation { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ParseMode
    {
        [EnumMember(Value = "Markdown")]
        Markdown,
        [EnumMember(Value = "HTML")]
        Html
    }

    public class TelegramResponse<TPayload>
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }

        [JsonProperty("result")]
        public TPayload Result { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

    }

    public class WebhookInfo
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("has_custom_certificate")]
        public bool HasCustomCertificate { get; set; }

        [JsonProperty("pending_update_count")]
        public int PendingUpdateCount { get; set; }

        [JsonProperty("max_connections")]
        public int MaxConnections { get; set; }
    }
}