namespace OBS.Models
{
    /// <summary>
    /// Favori öğrencileri saklamak için model.
    /// Favorites tablosuna karşılık gelir.
    /// </summary>
    public class Favorite
    {
        public int Id { get; set; }

        /// <summary>
        /// Favoriye eklenen öğrencinin numarası
        /// </summary>
        public string StudentNumber { get; set; } = string.Empty;

        /// <summary>
        /// Favoriye eklenme tarihi
        /// </summary>
        public DateTime AddedDate { get; set; } = DateTime.Now;
    }
}
