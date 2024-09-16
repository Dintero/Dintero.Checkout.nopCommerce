using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.Dintero.Services;

/// <summary>
/// dintero http client interface
/// </summary>
public interface IDinteroHttpClient
{
    /// <summary>
    /// GET method api request.
    /// </summary>
    /// <param name="endpoint">API endpoint</param>
    /// <param name="subscriptionKey">API subscription key header</param>
    /// <param name="subscriptionValue">API subscription value header</param>
    /// <param name="timeSpan">HttpClient Timeout</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the response json
    /// </returns>
    Task<string> GetAsync(string endpoint, TimeSpan? timeSpan = null);

    /// <summary>
    /// POST method api request.
    /// </summary>
    /// <param name="endpoint">API endpoint</param>
    /// <param name="content">json string content</param>
    /// <param name="subscriptionKey">API subscription key header</param>
    /// <param name="subscriptionValue">API subscription value header</param>
    /// <param name="timeSpan">HttpClient Timeout</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the response json
    /// </returns>
    Task<HttpResponseMessage> PostAsync(string endpoint, string content, TimeSpan? timeSpan = null, string accessToken = null);

    /// <summary>
    /// PUST method api request.
    /// </summary>
    /// <param name="endpoint">API endpoint</param>
    /// <param name="content">json string content</param>
    /// <param name="timeSpan">HttpClient Timeout</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the response json
    /// </returns>
    Task<HttpResponseMessage> PutAsync(string endpoint, string content, TimeSpan? timeSpan = null, string accessToken = null);

    /// <summary>
    /// generate token
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="timeSpan"></param>
    /// <returns>generated token</returns>
    Task<string> GenerateTokenAsync(string endpoint, TimeSpan? timeSpan = null);
}
