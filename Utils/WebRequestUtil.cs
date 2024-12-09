using System.Net.Http.Headers;
using System.Text;

namespace dotnet_api.Utils
{
    public static class WebRequestUtil
    {
        public static async Task<string> PostRequest(string url, string? token, string json)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    if (!string.IsNullOrWhiteSpace(token))
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var data = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(url, data);

                    return response.Content.ReadAsStringAsync().Result;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<string> GetRequest(string url, string? token = null)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    if (!string.IsNullOrWhiteSpace(token))
                        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    var response = await httpClient.GetStringAsync(new Uri(url));

                    return response;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
