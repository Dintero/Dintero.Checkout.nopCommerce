namespace Nop.Plugin.Payments.Dintero.Domain;

public class ErrorResponse
{
    public ErrorResponse()
    {
        error = new Error();
    }
    public Error error { get; set; }
    public string message { get; set; }
}
public class Error
{
    public string message { get; set; }
}
