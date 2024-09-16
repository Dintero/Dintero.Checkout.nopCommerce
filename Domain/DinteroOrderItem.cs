using System.Collections.Generic;

namespace Nop.Plugin.Payments.Dintero.Domain;

public class DinteroOrderItem
{
    public DinteroOrderItem()
    {
        discount_lines = new List<OrderItemDiscountlines>();
    }
    public int ItemId { get; set; }
    public string id { get; set; }
    public string line_id { get; set; }
    public string description { get; set; }
    public int quantity { get; set; }
    public decimal amount { get; set; }
    public decimal vat_amount { get; set; }
    public decimal vat { get; set; }
    public IList<OrderItemDiscountlines> discount_lines { get; set; }
}

public class OrderItemDiscountlines
{
    public int ItemId { get; set; }
    public decimal amount { get; set; }
    public decimal percentage { get; set; }
    public string discount_type { get; set; }
    public string discount_id { get; set; }
    public string description { get; set; }
    public int line_id { get; set; }
    public string discount_code { get; set; }
}
