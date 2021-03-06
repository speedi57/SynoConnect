﻿using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Synology.Classes;
using Synology.Interfaces;
using Synology.Utilities;
using System.Collections.Generic;
using System.Net;
using RestSharp;

namespace Synology
{
    /// <summary>
    /// 
    /// </summary>
    internal static class SynologyConnectionExtension
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private static ISynologyRequest ResolveRequest(this ISynologyConnection connection, Type t) => connection.ServiceProvider.GetService(t) as ISynologyRequest;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <returns></returns>
        private static T ResolveRequest<T>(this ISynologyConnection connection) where T : ISynologyRequest => (T)ResolveRequest(connection, typeof(T));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        private static ISynologyApi ResolveApi(this ISynologyConnection connection, Type t) => connection.ServiceProvider.GetService(t) as ISynologyApi;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <returns></returns>
        private static T ResolveApi<T>(this ISynologyConnection connection) where T : ISynologyApi => (T)ResolveApi(connection, typeof(T));

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="cgi"></param>
        /// <param name="api"></param>
        /// <param name="version"></param>
        /// <param name="method"></param>
        /// <param name="additionalParams"></param>
        /// <returns></returns>
        private static string GetApiUrl(this ISynologyConnection connection, string cgi, string api, int version, string method, QueryStringParameter[] additionalParams = null)
        {
            var url = new QueryStringManager(cgi);

            additionalParams = additionalParams ?? new QueryStringParameter[] { };

            url.AddParameters(additionalParams.Concat(new[] {
                new QueryStringParameter("_sid", connection.GetSid()),
                new QueryStringParameter("api", api),
                new QueryStringParameter("version", version),
                new QueryStringParameter("method", method)
            }).Where(t => t.MinVersion <= version));

            var res = url.ToString();

            connection.Logger.LogDebug($"Created API Url for GET: {res}");

            return res;
        }

        /// <summary>
        /// Get Uri for post operations
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="cgi">CGI path handling the request</param>
        /// <param name="api">API name handling the request</param>
        /// <param name="version">Version of the api</param>
        /// <param name="method">Method of the API</param>
        /// <returns>The Uri object where the request has to be sent</returns>
        private static Uri PostApiUrl(this ISynologyConnection connection, string cgi, string api, int version, string method)
        {
            var url = new QueryStringManager(cgi);
            url.AddParameters(new[] {
                new QueryStringParameter("_sid", connection.GetSid()),
            });
            var res = url.ToString();

            connection.Logger.LogDebug($"Created API Url for POST: {res}");

            return new Uri(connection.Client.BaseAddress, res);
        }

        private static async Task<T> GenericGetDataFromApiAsync<T>(this ISynologyConnection connection, string cgi, string api, int version, string method, QueryStringParameter[] additionalParams = null) where T : ResultData
        {
            var url = GetApiUrl(connection, cgi, api, version, method, additionalParams);
            var response = await connection.Client.GetStreamAsync(url);

            using (var reader = new StreamReader(response))
            {
                var json = await reader.ReadToEndAsync();

                connection.Logger.LogDebug($"Response JSON for {api} v.{version} with method {method} [cgi: {cgi}]: {json}");

                return JsonConvert.DeserializeObject<T>(json);
            }
        }
        private static StringContent StringToStringContent(string text)
        {
            var temp = new StringContent(text);
            temp.Headers.ContentType = null;
            return temp;
        }
        private static async Task<T> GenericPostDataFromApiAsync<T>(this ISynologyConnection connection, string cgi, string api, int version, string method, FormParameter[] additionalParams = null) where T : ResultData
        {

            var uri = PostApiUrl(connection, cgi, api, version, method);
            var client = new RestClient(uri);
            var request = new RestRequest(Method.POST);
            request.AddParameter("_sid", connection.GetSid());
            request.AddParameter("api", api);
            request.AddParameter("version", version);
            request.AddParameter("method", method);

            foreach (var temp in additionalParams)
            {
                if (temp is FileFormDataParameter)
                    request.AddFile("file", temp.Value,  "application/x-bittorrent");
                else if (!temp.Empty)
                    request.AddParameter(temp.Name , temp.Value);
            }
            var result = await client.ExecuteAsync(request);
            var data = result.Content;
            connection.Logger.LogDebug(data);

            return JsonConvert.DeserializeObject<T>(data);


        }

        /// <summary>
        /// Performs an asynchronous post request to the Synology API
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="cgi">CGI path handling the request</param>
        /// <param name="api">API name handling the request</param>
        /// <param name="version">Version of the api</param>
        /// <param name="method">Method of the API</param>
        /// <param name="additionalParams">Parameters of the request</param>
        /// <returns>Result of the request</returns>
        internal static async Task<ResultData> PostDataFromApiAsync(this ISynologyConnection connection, string cgi, string api, int version, string method, FormParameter[] additionalParams = null) => await GenericPostDataFromApiAsync<ResultData>(connection, cgi, api, version, method, additionalParams);

        /// <summary>
        /// Performs an asynchronous post request to the Synology API
        /// </summary>
        /// <typeparam name="T">Type of result</typeparam>
        /// <param name="connection"></param>
        /// <param name="cgi">CGI path handling the request</param>
        /// <param name="api">API name handling the request</param>
        /// <param name="version">Version of the api</param>
        /// <param name="method">Method of the API</param>
        /// <param name="additionalParams">Parameters of the request</param>
        /// <returns>Result of the request and the specific API/Method response</returns>
        internal static async Task<ResultData<T>> PostDataFromApiAsync<T>(this ISynologyConnection connection, string cgi, string api, int version, string method, FormParameter[] additionalParams = null) => await GenericPostDataFromApiAsync<ResultData<T>>(connection, cgi, api, version, method, additionalParams);

        internal static T Request<T>(this ISynologyConnection connection) where T : ISynologyRequest => ResolveRequest<T>(connection);

        internal static T Api<T>(this ISynologyConnection connection) where T : ISynologyApi => ResolveApi<T>(connection);

        internal static async Task<ResultData<T>> GetDataFromApiAsync<T>(this ISynologyConnection connection, string cgi, string api, int version, string method, QueryStringParameter[] additionalParams = null) => await GenericGetDataFromApiAsync<ResultData<T>>(connection, cgi, api, version, method, additionalParams);

        internal static async Task<ResultData> GetDataFromApiAsync(this ISynologyConnection connection, string cgi, string api, int version, string method, QueryStringParameter[] additionalParams = null) => await GenericGetDataFromApiAsync<ResultData>(connection, cgi, api, version, method, additionalParams);

        internal static string GetSid(this ISynologyConnection connection)
        {
            var sidContainer = connection.ServiceProvider.GetService<SidContainer>() as SidContainer;

            return sidContainer?.Sid;
        }

        internal static void SetSid(this ISynologyConnection connection, string value)
        {
            if (connection.ServiceProvider.GetService<SidContainer>() is SidContainer sidContainer)
                sidContainer.Sid = value;
        }
    }
}