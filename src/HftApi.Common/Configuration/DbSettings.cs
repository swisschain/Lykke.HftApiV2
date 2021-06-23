using Lykke.SettingsReader.Attributes;

namespace HftApi.Common.Configuration
{
    public class DbSettings
    {
        [AzureTableCheck]
        public string DataConnString { get; set; }
    }
}
