using Newtonsoft.Json;
using System.Collections.Generic;

namespace Nop.Plugin.Payments.Dintero.Domain;

public class DinteroOrderSessionRequest
{
    public DinteroOrderSessionRequest()
    {
        //configuration = new Configuration();
        order = new Order();
        url = new Url();
    }

    //public Configuration configuration { get; set; }
    public Order order { get; set; }
    public Url url { get; set; }

    public string profile_id { get; set; }
    public Customer customer { get; set; }
    public class Creditcard
    {
        public bool enabled { get; set; }
    }

    public class Payex
    {
        public Payex()
        {
            creditcard = new Creditcard();
        }
        public Creditcard creditcard { get; set; }
    }

    public class Vipps
    {
        public bool enabled { get; set; }
    }

    public class Invoice
    {
        public bool enabled { get; set; }
        public string type { get; set; }
    }

    public class Collector
    {
        public Collector()
        {
            invoice = new Invoice();
        }
        public string type { get; set; }
        public Invoice invoice { get; set; }
    }

    public class Configuration
    {
        public Configuration()
        {
            payex = new Payex();
            vipps = new Vipps();
            collector = new Collector();
        }
        public string default_payment_type { get; set; }
        public bool auto_capture { get; set; }
        public Payex payex { get; set; }
        public Vipps vipps { get; set; }
        public Collector collector { get; set; }
    }

    public class Item
    {
        public Item()
        {
            discount_lines = new List<Discountlines>();
        }
        public string id { get; set; }
        public string line_id { get; set; }
        public string description { get; set; }
        public int quantity { get; set; }
        public decimal amount { get; set; }
        public decimal vat_amount { get; set; }
        public decimal vat { get; set; }
        public IList<Discountlines> discount_lines { get; set; }
    }

    public class Order
    {
        public Order()
        {
            items = new List<Item>();
            shipping_option = new ShippingOption();
            discount_lines = new List<Discountlines>();
            discount_codes = new List<string>();
            shipping_address = new OrderAddress();
            billing_address = new OrderAddress();
        }
        public string merchant_reference { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public decimal vat_amount { get; set; }
        public IList<Item> items { get; set; }
        public ShippingOption shipping_option { get; set; }
        public OrderAddress shipping_address { get; set; }
        public OrderAddress billing_address { get; set; }
        public IList<Discountlines> discount_lines { get; set; }
        public IList<string> discount_codes { get; set; }
    }

    public class Discountlines
    {
        public decimal amount { get; set; }
        public decimal percentage { get; set; }
        public string discount_type { get; set; }
        public string discount_id { get; set; }
        public string description { get; set; }
        public int line_id { get; set; }
    }

    public class Url
    {
        public string return_url { get; set; }
        public string callback_url { get; set; }
       // public string merchant_terms_url { get; set; }

    }

    public class ShippingOption
    {
        public string id { get; set; }
        public string line_id { get; set; }
        public int amount { get; set; }
        public int vat_amount { get; set; }
        public int vat { get; set; }
        public string title { get; set; }
        public string @operator { get; set; }
    }

    public class OrderAddress
    {
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string address_line { get; set; }
        public string address_line_2 { get; set; }
        public string co_address { get; set; }
        public string business_name { get; set; }
        public string postal_code { get; set; }
        public string postal_place { get; set; }
        public string country { get; set; }
        public string phone_number { get; set; }
        public string email { get; set; }
        public int latitude { get; set; }
        public int longitude { get; set; }
        public string comment { get; set; }
        public string organization_number { get; set; }
        public string customer_reference { get; set; }
        public string cost_center { get; set; }
    }

    public class PayexCreditcard
    {
        public string payment_token { get; set; }
        public string recurrence_token { get; set; }
    }

    public class Tokens
    {
        public Tokens()
        {
            PayexCreditcard = new PayexCreditcard();
        }

        [JsonProperty("payex.creditcard")]
        public PayexCreditcard PayexCreditcard { get; set; }
    }

    public class Customer
    {
        public Customer()
        {
            tokens = new Tokens();
        }
        public string customer_id { get; set; }
        public string email { get; set; }
        public string phone_number { get; set; }
        public Tokens tokens { get; set; }
    }
}
