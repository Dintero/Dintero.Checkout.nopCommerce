using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Domain.Logging;
using Nop.Core.Http;
using Nop.Plugin.Payments.Dintero.Domain;
using Nop.Plugin.Payments.Dintero.Infrastructure;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using PTX.Plugin.Payments.Dintero.Domain;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.Dintero.Services;

/// <summary>
/// Represents the HTTP client to request music tribe
/// </summary>
public partial class DinteroHttpClient : IDinteroHttpClient
{
    #region Fields

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISettingService _settingService;
    private readonly ILogger _logger;
    private readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public DinteroHttpClient(IHttpClientFactory httpClientFactory,
        ISettingService settingService,
        ILogger logger,
        IWorkContext workContext)
    {
        _httpClientFactory = httpClientFactory;
        _settingService = settingService;
        _logger = logger;
        _workContext = workContext;
    }


    #endregion

    #region Methods

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
    public async Task<string> GetAsync(string endpoint, TimeSpan? timeSpan = null)
    {
        var dinteroPaySettings = await _settingService.LoadSettingAsync<DinteroPaymentSettings>();
        if (string.IsNullOrEmpty(dinteroPaySettings.AccessToken)
        || dinteroPaySettings.TokenExpiresInDatetimeUTC == null
        || dinteroPaySettings.TokenExpiresInDatetimeUTC < DateTime.UtcNow)
            dinteroPaySettings.AccessToken = await GenerateTokenAsync("auth/token");
        

        var baseUrl = dinteroPaySettings.UseSandbox ? dinteroPaySettings.SandboxURL ?? string.Empty : dinteroPaySettings.ProductionURL ?? string.Empty;
        var apiUrl = $"https://{baseUrl.TrimEnd('/')}/{endpoint}";

        var clientId = dinteroPaySettings.ClientId;
        var secretKey = dinteroPaySettings.SecretKey;

        var httpClient = _httpClientFactory.CreateClient(NopHttpDefaults.DefaultHttpClient);
        if (string.IsNullOrEmpty(dinteroPaySettings.AccessToken))
        {
            var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(clientId + ":" + secretKey));
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");
        }
        else
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + dinteroPaySettings.AccessToken);


        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Dintero-System-Name", "nopCommerce ");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Dintero-System-Plugin-Name", "Payments.Dintero");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Dintero-System-Version", "4.30");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Dintero-System-Plugin-Version", "1.00");

        if (timeSpan != null)
            httpClient.Timeout = timeSpan.Value;

        var response = await httpClient.GetAsync(apiUrl);
        return await response.Content.ReadAsStringAsync();
    }

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
    public async Task<HttpResponseMessage> PostAsync(string endpoint, string content, TimeSpan? timeSpan = null, string accessToken = null)
    {

        var dinteroPaySettings = await _settingService.LoadSettingAsync<DinteroPaymentSettings>();
        if (string.IsNullOrEmpty(dinteroPaySettings.AccessToken)
        || dinteroPaySettings.TokenExpiresInDatetimeUTC == null
        || dinteroPaySettings.TokenExpiresInDatetimeUTC < DateTime.UtcNow)
            accessToken = await GenerateTokenAsync("auth/token");
        else
            accessToken = dinteroPaySettings.AccessToken;

        var baseUrl = dinteroPaySettings.UseSandbox ? dinteroPaySettings.SandboxURL ?? string.Empty : dinteroPaySettings.ProductionURL ?? string.Empty;
        var apiUrl = $"https://{baseUrl.TrimEnd('/')}/{endpoint}";

        var clientId = dinteroPaySettings.ClientId;
        var secretKey = dinteroPaySettings.SecretKey;

        var httpClient = _httpClientFactory.CreateClient(NopHttpDefaults.DefaultHttpClient);

        if (string.IsNullOrEmpty(accessToken))
        {
            var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(clientId + ":" + secretKey));
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");
        }
        else
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + accessToken);

        
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Dintero-System-Name", "nopCommerce ");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Dintero-System-Plugin-Name", "Payments.Dintero");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Dintero-System-Version", "4.30");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Dintero-System-Plugin-Version", "1.00");

        var httpContent = new StringContent(content, Encoding.UTF8, PluginDefaults.Content_Type);

        if (timeSpan != null)
            httpClient.Timeout = timeSpan.Value;

        return await httpClient.PostAsync(apiUrl, httpContent);

    }

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
    public async Task<HttpResponseMessage> PutAsync(string endpoint, string content, TimeSpan? timeSpan = null, string accessToken = null)
    {
        var dinteroPaySettings = await _settingService.LoadSettingAsync<DinteroPaymentSettings>();
        if (string.IsNullOrEmpty(dinteroPaySettings.AccessToken)
        || dinteroPaySettings.TokenExpiresInDatetimeUTC == null
        || dinteroPaySettings.TokenExpiresInDatetimeUTC < DateTime.UtcNow)
            accessToken = await GenerateTokenAsync("auth/token");
        else
            accessToken = dinteroPaySettings.AccessToken;

        var baseUrl = dinteroPaySettings.UseSandbox ? dinteroPaySettings.SandboxURL ?? string.Empty : dinteroPaySettings.ProductionURL ?? string.Empty;
        var apiUrl = $"https://{baseUrl.TrimEnd('/')}/{endpoint}";

        var clientId = dinteroPaySettings.ClientId;
        var secretKey = dinteroPaySettings.SecretKey;

        var httpClient = _httpClientFactory.CreateClient(NopHttpDefaults.DefaultHttpClient);

        if (string.IsNullOrEmpty(accessToken))
        {
            var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes(clientId + ":" + secretKey));
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");
        }
        else
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer " + accessToken);


        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Dintero-System-Name", "nopCommerce ");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Dintero-System-Plugin-Name", "Payments.Dintero");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Dintero-System-Version", "4.30");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Dintero-System-Plugin-Version", "1.00");

        var httpContent = new StringContent(content, Encoding.UTF8, PluginDefaults.Content_Type);

        if (timeSpan != null)
            httpClient.Timeout = timeSpan.Value;

        return await httpClient.PutAsync(apiUrl, httpContent);

    }


    /// <summary>
    /// Generate token api request.
    /// </summary>
    /// <param name="endpoint">API endpoint</param>
    /// <param name="timeSpan">HttpClient Timeout</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the response json
    /// </returns>
    public async Task<string> GenerateTokenAsync(string endpoint, TimeSpan? timeSpan = null)
    {
        var _dinteroPaymentSettings = await _settingService.LoadSettingAsync<DinteroPaymentSettings>();

        var accountId = _dinteroPaymentSettings.AccountId;// _dinteroPaymentSettings.UseSandbox ? $"T{_dinteroPaymentSettings.AccountId}" : $"T{_dinteroPaymentSettings.AccountId}";
        var baseURL = _dinteroPaymentSettings.UseSandbox ? _dinteroPaymentSettings.SandboxAuthEndpoint ?? string.Empty : _dinteroPaymentSettings.ProductionAuthEndpoint ?? string.Empty;
        var audienceUrl = _dinteroPaymentSettings.UseSandbox ? _dinteroPaymentSettings.SandboxAuthAudience ?? string.Empty : _dinteroPaymentSettings.ProductionAuthAudience ?? string.Empty;
        var apiUrl = $"https://{baseURL.TrimEnd('/')}/accounts/{accountId}/{endpoint}";

        var clientId = _dinteroPaymentSettings.ClientId;
        var secretKey = _dinteroPaymentSettings.SecretKey;

        var httpClient = _httpClientFactory.CreateClient(NopHttpDefaults.DefaultHttpClient);
        var base64authorization = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{secretKey}"));
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Basic {base64authorization}");

        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Dintero-System-Name", "nopCommerce ");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Dintero-System-Plugin-Name", "Payments.Dintero");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Dintero-System-Version", "4.30");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Dintero-System-Plugin-Version", "1.00");

        if (timeSpan != null)
            httpClient.Timeout = timeSpan.Value;

        string token = "";
        try
        {
            //get auth token
            var dinteroTokenRequest = new
            {
                grant_type = "client_credentials",
                audience = $"https://{audienceUrl}/accounts/{accountId}"
            };
            var content = JsonConvert.SerializeObject(dinteroTokenRequest);
            var httpContent = new StringContent(content, Encoding.UTF8, "application/json");

            if (timeSpan != null)
                httpClient.Timeout = timeSpan.Value;

            var response = await httpClient.PostAsync(apiUrl, httpContent);
            if (response.IsSuccessStatusCode)
            {
                var dinteroAuthTokenResponse = JsonConvert.DeserializeObject<DinteroPaymentResponse>(await response.Content.ReadAsStringAsync());
                token = dinteroAuthTokenResponse.access_token;
                if (!string.IsNullOrEmpty(token))
                {
                    _dinteroPaymentSettings.AccessToken = token;
                    _dinteroPaymentSettings.TokenExpiresIn = dinteroAuthTokenResponse.expires_in;
                    _dinteroPaymentSettings.TokenExpiresInDatetimeUTC = DateTime.UtcNow.AddSeconds(dinteroAuthTokenResponse.expires_in);

                    await _settingService.SaveSettingAsync(_dinteroPaymentSettings, settings => settings.AccessToken, clearCache: false);
                    await _settingService.SaveSettingAsync(_dinteroPaymentSettings, x => x.TokenExpiresIn, clearCache: false);
                    await _settingService.SaveSettingAsync(_dinteroPaymentSettings, x => x.TokenExpiresInDatetimeUTC, clearCache: false);

                    await _settingService.ClearCacheAsync();
                }
            }
            else
               await _logger.InsertLogAsync(LogLevel.Error, "Dintero payment plugn response body to access token", await response.Content.ReadAsStringAsync(), await _workContext.GetCurrentCustomerAsync());
        }
        catch (Exception ex)
        {
            await _logger.ErrorAsync(ex.Message, ex, await _workContext.GetCurrentCustomerAsync());
        }
        finally
        {
            httpClient.Dispose();
        }

        return token;
    }

    #endregion
}
