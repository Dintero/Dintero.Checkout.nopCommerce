namespace Nop.Plugin.Payments.Dintero.Models;

public class TransactionErrorResponse
{
    public Error Error { get; set; }
}

public class Error
{
    public string message { get; set; }
}
