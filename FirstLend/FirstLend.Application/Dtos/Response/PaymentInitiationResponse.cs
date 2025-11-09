using System;

namespace FirstLend.Application.Dtos.Response
{
    public class PaymentInitiationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string AuthorizationUrl { get; set; } = "";
        public string AccessCode { get; set; } = "";
        public string Reference { get; set; } = "";
    }
}
