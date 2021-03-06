﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using System.Text;
using SendGrid.Resources;
using System.Net;
using Newtonsoft.Json.Linq;

namespace SendGrid
{
    public class Client
    {
        private string _apiKey;
        private string _userName;
        private string _password;
        public APIKeys ApiKeys;
        public UnsubscribeGroups UnsubscribeGroups;
        public Suppressions Suppressions;
        public GlobalSuppressions GlobalSuppressions;
        public GlobalStats GlobalStats;
        public Templates Templates;
        public Versions Versions;
        public Batches Batches;
        public string Version;
        private Uri _baseUri;
        private const string MediaType = "application/json";
        private enum Methods
        {
            GET, POST, PATCH, DELETE
        }

        /// <summary>
        ///     Create a client that connects to the SendGrid Web API
        /// </summary>
        /// <param name="apiKey">Your SendGrid API Key</param>
        /// <param name="baseUri">Base SendGrid API Uri</param>
        public Client(string apiKey, string baseUri = "https://api.sendgrid.com/")
        {
            _baseUri = new Uri(baseUri);
            _apiKey = apiKey;
            Initialize();   
        }

        /// <summary>
        ///     Create a client that connects to the SendGrid Web API
        /// </summary>
        /// <param name="username">Your SendGrid Username</param>
        /// <param name="password">Your SendGrid Password</param>
        /// <param name="baseUri">Base SendGrid API Uri</param>
        public Client(string userName, string password, string baseUri = "https://api.sendgrid.com/")
        {
            _baseUri = new Uri( baseUri );
            _userName = userName;
            _password = password;
            Initialize();
        }

        private void Initialize()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            ApiKeys = new APIKeys( this );
            UnsubscribeGroups = new UnsubscribeGroups( this );
            Suppressions = new Suppressions( this );
            GlobalSuppressions = new GlobalSuppressions( this );
            GlobalStats = new GlobalStats( this );
            Templates = new Templates( this );
            Versions = new Versions( this );
            Batches = new Batches( this );
        }

        /// <summary>
        ///     Create a client that connects to the SendGrid Web API
        /// </summary>
        /// <param name="method">HTTP verb, case-insensitive</param>
        /// <param name="endpoint">Resource endpoint, do not prepend slash</param>
        /// <param name="data">An JObject representing the resource's data</param>
        /// <returns>An asyncronous task</returns>
        private async Task<HttpResponseMessage> RequestAsync(Methods method, string endpoint, JObject data)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    client.BaseAddress = _baseUri;
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaType));
                    if (!string.IsNullOrWhiteSpace(_userName) && !string.IsNullOrWhiteSpace(_password))
                    {
                        var byteArray = Encoding.ASCII.GetBytes(string.Format("{0}:{1}", _userName, _password));
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                    }
                    else
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                    }
                    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "sendgrid/" + Version + ";csharp");

                    switch (method)
                    {
                        case Methods.GET:
                            return await client.GetAsync(endpoint);
                        case Methods.POST:
                            return await client.PostAsJsonAsync(endpoint, data);
                        case Methods.PATCH:
                            endpoint = _baseUri + endpoint;
                            StringContent content = new StringContent(data.ToString(), Encoding.UTF8, MediaType);
                            HttpRequestMessage request = new HttpRequestMessage
                            {
                                Method = new HttpMethod("PATCH"),
                                RequestUri = new Uri(endpoint),
                                Content = content
                            };
                            return await client.SendAsync(request);
                        case Methods.DELETE:
                            return await client.DeleteAsync(endpoint);
                        default:
                            HttpResponseMessage response = new HttpResponseMessage();
                            response.StatusCode = HttpStatusCode.MethodNotAllowed;
                            var message = "{\"errors\":[{\"message\":\"Bad method call, supported methods are GET, POST, PATCH and DELETE\"}]}";
                            response.Content = new StringContent(message);
                            return response;
                    }
                }
                catch (Exception ex)
                {
                    HttpResponseMessage response = new HttpResponseMessage();
                    string message;
                    message = (ex is HttpRequestException) ? ".NET HttpRequestException" : ".NET Exception";
                    message = message + ", raw message: \n\n";
                    response.Content = new StringContent(message + ex.Message);
                    return response;
                }
            }
        }

        /// <param name="endpoint">Resource endpoint, do not prepend slash</param>
        /// <returns>The resulting message from the API call</returns>
        public async Task<HttpResponseMessage> Get(string endpoint)
        {
            return await RequestAsync(Methods.GET, endpoint, null);
        }

        /// <param name="endpoint">Resource endpoint, do not prepend slash</param>
        /// <param name="data">An JObject representing the resource's data</param>
        /// <returns>The resulting message from the API call</returns>
        public async Task<HttpResponseMessage> Post(string endpoint, JObject data)
        {
            return await RequestAsync(Methods.POST, endpoint, data);
        }

        /// <param name="endpoint">Resource endpoint, do not prepend slash</param>
        /// <returns>The resulting message from the API call</returns>
        public async Task<HttpResponseMessage> Delete(string endpoint)
        {
            return await RequestAsync(Methods.DELETE, endpoint, null);
        }

        /// <param name="endpoint">Resource endpoint, do not prepend slash</param>
        /// <param name="data">An JObject representing the resource's data</param>
        /// <returns>The resulting message from the API call</returns>
        public async Task<HttpResponseMessage> Patch(string endpoint, JObject data)
        {
            return await RequestAsync(Methods.PATCH, endpoint, data);
        }
    }
}
