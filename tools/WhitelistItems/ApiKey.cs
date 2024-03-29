using System;

namespace WhitelistItems
{
    public class ApiKey
    {
        public Guid Id { get; set; }
        public string ClientId { get; set; }
        public string WalletId { get; set; }
        public string Token { get; set; }
        public DateTime? Created { get; set; }
        public DateTime? ValidTill { get; set; }
        public bool Apiv2Only { get; set; }
    }
}
