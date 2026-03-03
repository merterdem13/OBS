using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using OBS.Models;

namespace OBS.ViewModels
{
    /// <summary>
    /// Student model için ViewModel wrapper.
    /// StudentCardComponent'in DataContext'i olarak kullanılır.
    /// INotifyPropertyChanged ile UI güncelleme desteği sağlar.
    /// </summary>
    public class StudentViewModel : INotifyPropertyChanged
    {
        private readonly Student _student;
        private bool _isFavorite;

        public StudentViewModel(Student student, bool isFavorite = false)
        {
            _student = student;
            _isFavorite = isFavorite;
        }

        // Model properties (read-only wrapping)
        public string StudentNumber => _student.StudentNumber;
        public string FirstName => _student.FirstName;
        public string LastName => _student.LastName;
        public string FullName => $"{FirstName} {LastName}";
        public string Class => _student.Class;
        public int ClassNo => _student.ClassNo;
        public string TcNo => _student.TcNo;
        public DateTime? BirthDate => _student.BirthDate;
        public string PhotoPath => _student.PhotoPath;
        public string KunyePdfPath => _student.KunyePdfPath;
        public int? GuardianId => _student.GuardianId;

        /// <summary>
        /// Doğum tarihini string formatında döndürür.
        /// İSTEM METNİ GEREĞİ: StudentCardComponent'te gösterilmek için.
        /// </summary>
        public string BirthDateString => BirthDate?.ToString("dd.MM.yyyy") ?? "-";

        /// <summary>
        /// Veli telefon numarası (Guardian tablosundan çekilecek).
        /// Şimdilik placeholder, MainViewModel tarafından doldurulacak.
        /// </summary>
        public string GuardianPhone { get; set; } = "-";

        /// <summary>
        /// Favori durumu (değiştirilebilir).
        /// İSTEM METNİ GEREĞİ: Favori butonu bu property'ye bağlı.
        /// </summary>
        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite != value)
                {
                    _isFavorite = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FavoriteTooltip));
                }
            }
        }

        /// <summary>
        /// Favori butonu tooltip metni.
        /// </summary>
        public string FavoriteTooltip => IsFavorite ? "Favorilerden çıkar" : "Favorilere ekle";

        private bool _isRemoving;
        public bool IsRemoving
        {
            get => _isRemoving;
            set
            {
                if (_isRemoving != value)
                {
                    _isRemoving = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Orijinal Student modelini döndürür (Repository işlemleri için).
        /// </summary>
        public Student GetModel() => _student;

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
