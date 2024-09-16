namespace Nop.Plugin.Payments.Dintero.Domain;

public class DinteroPaymentResponse
{
    public DinteroPaymentResponse()
    {
    }
    public string access_token { get; set; }
    public int expires_in { get; set; }
    public string token_type { get; set; }
}
