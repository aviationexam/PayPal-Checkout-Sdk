// This class was generated on Tue, 04 Sep 2018 12:18:45 PDT by version 0.1.0-dev+904328-dirty of Braintree SDK Generator
// OrdersCaptureRequest.cs
// @version 0.1.0-dev+904328-dirty
// @type request
// @data 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
// DO NOT EDIT

using PayPalCheckoutSdk.RequestInterfaces;
using System;
using System.IO;
using System.Net.Http;


namespace PayPalCheckoutSdk.Orders
{
    /// <summary>
    /// Captures a payment for an order.
    /// </summary>
    public class OrdersCaptureRequest : BaseHttpRequest<Order, OrderActionRequest>, IConfigurePrefer, IConfigurePayPalRequestId, IConfigurePayPalMetadataId
    {
        public OrdersCaptureRequest(string orderId) : base("/v2/checkout/orders/{order_id}/capture?", HttpMethod.Post)
        {
            try
            {
                Path = Path.Replace("{order_id}", Uri.EscapeDataString(Convert.ToString(orderId)));
            }
            catch (IOException)
            {
            }

            ContentType = ApplicationJson;
        }
    }
}
