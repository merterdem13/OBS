namespace OBS.Models
{
    public class UpdateConfig
    {
        public bool ForceUpdate { get; set; }
        public string MinRequiredVersion { get; set; } = string.Empty;
        public string ForceUpdateMessage { get; set; } = string.Empty;
    }
}
