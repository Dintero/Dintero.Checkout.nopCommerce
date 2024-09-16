namespace PTX.Plugin.Payments.Dintero.Domain;

/// <summary>
/// Represents the constants
/// </summary>
public static class PluginDefaults
{
    /// <summary>
    /// Gets the system name string
    /// </summary>
    public const string SystemName = "Payments.Dintero";

    /// <summary>
    /// Gets the dintero response identifier string
    /// </summary>
    public const string DinteroTranscationId = "Dintero transcationId";

    /// <summary>
    /// Gets the dintero session identifier string
    /// </summary>
    public const string DinteroSessionId = "Dintero sessionId";

    /// <summary>
    /// Gets the dintero response identifier string
    /// </summary>
    public const string DinteroCustomerTranscationId = "DinteroCustomertranscationId";

    /// <summary>
    /// Gets the dintero customer's last order
    /// </summary>
    public const string DinteroCustomerLastOrder = "DinteroCustomersLastOrder";

    /// <summary>
    /// Gets the header host string
    /// </summary>
    public const string HEADER_HOST = "host";

    /// <summary>
    /// Gets the header signature string
    /// </summary>
    public const string HEADER_SIGNATURE = "Signature";

    /// <summary>
    /// Gets the header digest string
    /// </summary>
    public const string HEADER_DIGEST = "Digest";

    /// <summary>
    /// Gets the header v-c-account-id string
    /// </summary>
    public const string HEADER_V_C_ACCOUNT_ID = "v-c-account-id";

    /// <summary>
    /// Gets the header date string
    /// </summary>
    public const string HEADER_DATE = "date";

    /// <summary>
    /// Gets the header content type string
    /// </summary>
    public const string Content_Type = "application/json";

    /// <summary>
    /// Gets the header v-c-date string
    /// </summary>
    public const string HEADER_V_C_DATE = "v-c-date";

    /// <summary>
    /// Gets a dintero resource table name
    /// </summary>
    public const string Dintero_PAYMENTCARD = "Dintero_PaymentCard";

    public const string DINTERO_RETURN_URL = "{0}Plugins/DinteroHandler/IPNHandler?id={1}&sessionId={2}";

    public const string DINTERO_CALLBACK_URL = "{0}Plugins/DinteroHandler/CallBackHandler?id={1}&sessionId={2}";

    public const string DinteroOrderSessionId = "DinteroOrderSessionId";
    public const string DinteroOrderSessionIdGeneratedTime = "DinteroOrderSessionIdGeneratedTime";
    public const int DinteroOrderSessionExiredTime = 10;

}
