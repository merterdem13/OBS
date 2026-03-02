namespace AlparslanOBS.Models
{
    /// <summary>
    /// Uygulama ayarlarını (PIN vb.) saklamak için model.
    /// Settings tablosuna karşılık gelir.
    /// </summary>
    public class Setting
    {
        public int Id { get; set; }

        /// <summary>
        /// Ayar anahtarı (örn: "PIN")
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Ayar değeri (hash'lenmiş PIN gibi)
        /// </summary>
        public string Value { get; set; } = string.Empty;
    }
}
