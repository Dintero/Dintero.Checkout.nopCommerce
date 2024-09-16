namespace Nop.Plugin.Payments.Dintero.Domain;

public class DinteroPaymentRequest
{
    public DinteroPaymentRequest()
    {
    }
    public string grant_type { get; set; }
    public string audience { get; set; }
}
