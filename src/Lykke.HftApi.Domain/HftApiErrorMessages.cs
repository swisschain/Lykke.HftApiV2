namespace Lykke.HftApi.Domain
{
    public static class HftApiErrorMessages
    {
        public const string AssetNotFound = "Asset not found";
        public const string AssetPairNotFound = "Asset pair not found";
        public const string OrderNotFound = "Order not found";
        public static string LessThanZero(string name) => $"{name} cannot be less than zero.";
        public static string MustBeGreaterThan(string name, string minValue) => $"{name} must be greater than {minValue}";
        public static string MustBeOtherThan(string name, string currentValue) => $"{name} must be other than {currentValue}";
        public static string TooBig(string name, string value, string maxValue) =>
            $"{name} '{value}' is too big, maximum is '{maxValue}'.";
    }
}
