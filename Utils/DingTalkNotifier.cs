using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace OcelotGateway.Utils
{
    public static class DingTalkNotifier
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// 发送带加签的钉钉机器人文本消息
        /// </summary>
        /// <param name="webhookUrl">不带签名的Webhook URL</param>
        /// <param name="secret">钉钉机器人SEC开头的密钥，如果不加签请传 null 或空</param>
        /// <param name="title">文本消息内容</param>
        /// <param name="message">文本消息内容</param>
        public static async Task SendTextMessageAsync(string? webhookUrl, string? secret, string title, string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(webhookUrl) || string.IsNullOrWhiteSpace(secret))
                {
                    throw new ArgumentException("Webhook URL 和 Secret 不能为空");
                }
                string url = webhookUrl ?? "";

                // 如果配置了Secret，则计算签名
                if (!string.IsNullOrWhiteSpace(secret))
                {
                    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    string stringToSign = $"{timestamp}\n{secret}";

                    using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
                    byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
                    string sign = HttpUtility.UrlEncode(Convert.ToBase64String(hash));

                    url += $"&timestamp={timestamp}&sign={sign}";
                }
                Dictionary<string, object> markdown = new Dictionary<string, object>();
                markdown.Add("title", title);
                markdown.Add("text", $"{message}");
                var payload = new
                {
                    msgtype = "markdown",
                    markdown = markdown
                };

                var json = JsonSerializer.Serialize(payload);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, httpContent);
                response.EnsureSuccessStatusCode();

                Console.WriteLine("钉钉消息发送成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine("钉钉消息发送失败: " + ex.Message);
            }
        }
    }
}