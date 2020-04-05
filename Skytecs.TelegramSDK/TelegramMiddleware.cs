using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Skytecs.TelegramSDK
{
    static class TelegramMiddleware
    {
        private static readonly JsonSerializer Serializer = new JsonSerializer();

        public static void Map(IApplicationBuilder builder)
        {
            builder.Run(async context =>
            {
                string body;
                Task task;

                using (var streamReader = new StreamReader(context.Request.Body))
                {
                    body = await streamReader.ReadToEndAsync();

                    Console.WriteLine(body);
                }
                
                using (var jsonReader = new JsonTextReader(new StringReader(body)))
                {
                    var payload = Serializer.Deserialize<Update>(jsonReader);

                    var queueManager = builder.ApplicationServices.GetService<BackgroundServiceAccessor<MessageQueueManager>>();

                    task = queueManager.Service.OnUpdate(payload);

                    context.Response.StatusCode = 200;
                    using (var streamWriter = new StreamWriter(context.Response.Body))
                    {
                        streamWriter.Write("True");
                    }

                    await task;
                }
            });

        }
    }

    public class From
    {

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("is_bot")]
        public bool IsBot { get; set; }

        [JsonProperty("first_name")]
        public string FirstName { get; set; }

        [JsonProperty("last_name")]
        public string LastName { get; set; }
    }

    public class Chat
    {

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("all_members_are_administrators")]
        public bool AllMembersAreAdministrators { get; set; }
    }

    public class Message
    {
        public Message()
        {
            Entities = new List<MessageEntity>();
        }

        [JsonProperty("message_id")]
        public int MessageId { get; set; }

        [JsonProperty("reply_to_message")]
        public Message ReplyToMessage { get; set; }

        [JsonProperty("from")]
        public From From { get; set; }

        [JsonProperty("chat")]
        public Chat Chat { get; set; }

        [JsonProperty("date")]
        public int Date { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("entities")]
        public ICollection<MessageEntity> Entities { get; private set; }

        public SendMessageRequest CreateReply(string text)
        {
            return new SendMessageRequest
            {
                ChatId = Chat.Id,
                ReplyToMessageId = MessageId,
                Text = text
            };
        }

        public bool HasCommand(string commandName)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                throw new ArgumentNullException(nameof(commandName));
            }

            return Entities.Any(x => x.Type == MessageEntityType.BotCommand && Text.Substring(x.Offset + 1, x.Length - 1).ToLowerInvariant() == commandName.ToLowerInvariant());
        }
    }

    public class MessageEntity
    {

        [JsonProperty("offset")]
        public int Offset { get; set; }

        [JsonProperty("length")]
        public int Length { get; set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public MessageEntityType Type { get; set; }
    }

    public enum MessageEntityType
    {
        [EnumMember(Value = "mention")]
        Mention,
        [EnumMember(Value = "hashtag")]
        Hashtag,
        [EnumMember(Value = "cashtag")]
        Cashtag,
        [EnumMember(Value = "bot_command")]
        BotCommand,
        [EnumMember(Value = "url")]
        Url,
        [EnumMember(Value = "email")]
        Email,
        [EnumMember(Value = "phone_number")]
        PhoneNumber,
        [EnumMember(Value = "bold")]
        Bold,
        [EnumMember(Value = "italic")]
        Italic,
        [EnumMember(Value = "code")]
        Code,
        [EnumMember(Value = "pre")]
        Pre,
        [EnumMember(Value = "text_link")]
        TextLink,
        [EnumMember(Value = "text_mention")]
        TextMention
    }

    public class CallbackQuery
    {
        public CallbackQuery()
        {
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("from")]
        public From From { get; set; }

        [JsonProperty("message")]
        public Message Message { get; set; }

        [JsonProperty("inline_message_id")]
        public string InlineMessageId { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }
    }


    public class Update
    {

        [JsonProperty("update_id")]
        public int UpdateId { get; set; }

        [JsonProperty("message")]
        public Message Message { get; set; }

        [JsonProperty("edited_message")]
        public Message EditedMessage { get; set; }

        [JsonProperty("callback_query")]
        public CallbackQuery CallbackQuery { get; set; }

    }
}
