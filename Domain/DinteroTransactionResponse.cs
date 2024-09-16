using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Nop.Plugin.Payments.Dintero.Domain;


// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class Card
{
    public string brand { get; set; }
    public string masked_pan { get; set; }
    public string expiry_date { get; set; }
    public string type { get; set; }
    public string issuing_bank { get; set; }
    public string issuer_authorization_code { get; set; }
    public string acquirer_transaction_type { get; set; }
    public string acquirer_stan { get; set; }
    public string acquirer_terminal_id { get; set; }
    public DateTime acquirer_transaction_time { get; set; }
    public string authentication_status { get; set; }
}

public class Item
{
    public string id { get; set; }
    public decimal vat { get; set; }
    public decimal amount { get; set; }
    public string line_id { get; set; }
    public int quantity { get; set; }
    public decimal vat_amount { get; set; }
    public string description { get; set; }
}

public class Metadata
{
    [JsonProperty("payex:transaction:id")]
    public string PayexTransactionId { get; set; }

    [JsonProperty("payex:transaction:type")]
    public string PayexTransactionType { get; set; }

    [JsonProperty("payex:transaction:state")]
    public string PayexTransactionState { get; set; }

    [JsonProperty("payex:transaction:number")]
    public long? PayexTransactionNumber { get; set; }

    [JsonProperty("payex:transaction:created")]
    public DateTime? PayexTransactionCreated { get; set; }

    [JsonProperty("payex:transaction:payee_reference")]
    public string PayexTransactionPayeeReference { get; set; }
    public string merchant_name { get; set; }

    [JsonProperty("payex:payment:id")]
    public string PayexPaymentId { get; set; }

    [JsonProperty("payex:payment:number")]
    public long PayexPaymentNumber { get; set; }

    [JsonProperty("payex:payment:created")]
    public DateTime PayexPaymentCreated { get; set; }
    public string payout_correlation_id { get; set; }

    [JsonProperty("payex:payment:operation")]
    public string PayexPaymentOperation { get; set; }

    [JsonProperty("payex:payment:payee_info:payee_id")]
    public string PayexPaymentPayeeInfoPayeeId { get; set; }

    [JsonProperty("payex:payment:payee_info:payee_name")]
    public string PayexPaymentPayeeInfoPayeeName { get; set; }
}

public class Event
{
    public Event()
    {
        items = new List<Item>();
    }
    public string id { get; set; }
    public string @event { get; set; }
    public IList<Item> items { get; set; }
    public decimal amount { get; set; }
    public bool success { get; set; }
    public Metadata metadata { get; set; }
    public DateTime created_at { get; set; }
    public string request_id { get; set; }
    public string transaction_status { get; set; }
    public EventError error { get; set; }
}

public class EventError
{
    public string message { get; set; }
    public string code { get; set; }
}

public class Url
{
    public string return_url { get; set; }
}

public class DinteroTransactionResponse
{
    public DinteroTransactionResponse()
    {
        events = new List<Event>();
        items = new List<Item>();
    }
    public string payment_product { get; set; }
    public decimal amount { get; set; }
    public string currency { get; set; }
    public string merchant_reference { get; set; }
    public string customer_ip { get; set; }
    public string user_agent { get; set; }
    public string payment_product_type { get; set; }
    public string status { get; set; }
    public string account_id { get; set; }
    public DateTime created_at { get; set; }
    public DateTime updated_at { get; set; }
    public string session_id { get; set; }
    public int version { get; set; }
    public string id { get; set; }
    public Card card { get; set; }
    public string settlement_status { get; set; }
    public IList<Event> events { get; set; }
    public Metadata metadata { get; set; }
    public IList<Item> items { get; set; }
    public Url url { get; set; }
}
