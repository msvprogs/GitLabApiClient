﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GitLabApiClient.Http
{
    internal class GitLabApiRequestor
    {
        private readonly HttpClient _client;

        public GitLabApiRequestor(HttpClient client) => _client = client;

        public async Task<T> Put<T>(string url, object data)
        {
            StringContent content = SerializeToString(data, false);
            var responseMessage = await _client.PutAsync(TryFixApiUrl(url), content);
            await EnsureSuccessStatusCode(responseMessage);
            return await ReadResponse<T>(responseMessage);
        }

        public async Task<T> Post<T>(string url, object data = null)
        {
            StringContent content = SerializeToString(data, true);
            var responseMessage = await _client.PostAsync(TryFixApiUrl(url), content);
            await EnsureSuccessStatusCode(responseMessage);
            return await ReadResponse<T>(responseMessage);
        }

        public async Task Delete(string url)
        {
            var responseMessage = await _client.DeleteAsync(TryFixApiUrl(url));
            await EnsureSuccessStatusCode(responseMessage);
        }

        public async Task<T> Get<T>(string url)
        {
            var responseMessage = await _client.GetAsync(TryFixApiUrl(url));
            await EnsureSuccessStatusCode(responseMessage);
            return await ReadResponse<T>(responseMessage);
        }

        public async Task<Tuple<T, HttpResponseHeaders>> GetWithHeaders<T>(string url)
        {
            var responseMessage = await _client.GetAsync(TryFixApiUrl(url));
            await EnsureSuccessStatusCode(responseMessage);
            return Tuple.Create(await ReadResponse<T>(responseMessage), responseMessage.Headers);
        }

        private static async Task EnsureSuccessStatusCode(HttpResponseMessage responseMessage)
        {
            if (responseMessage.IsSuccessStatusCode)
                return;

            string errorResponse = await responseMessage.Content.ReadAsStringAsync();
            throw new GitLabException(errorResponse ?? "");
        }

        private static async Task<T> ReadResponse<T>(HttpResponseMessage responseMessage)
        {
            string response = await responseMessage.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<T>(response);
            return result;
        }

        private static StringContent SerializeToString(object data, bool ignoreNullValues)
        {
            string serializedObject = ignoreNullValues ?
                JsonConvert.SerializeObject(data, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                }) :
                JsonConvert.SerializeObject(data);

            var content = data != null ?
                new StringContent(serializedObject) :
                new StringContent(string.Empty);

            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return content;
        }

        private static string TryFixApiUrl(string url)
        {
            if (!url.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                url = "/" + url;

            if (!url.StartsWith("/api/v4", StringComparison.OrdinalIgnoreCase))
                url = "/api/v4" + url;

            return url;
        }
    }
}