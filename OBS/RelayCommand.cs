using System;
using System.Windows.Input;

namespace OBS
{
    /// <summary>
    /// MVVM pattern'de ViewModel'ler ile View'ler arasındaki etkileşimleri sağlamak için
    /// kullanılan generic command sınıfı. ICommand interface'ini implements eder.
    /// Sade ve anlık (immediate) command yürütme için tasarlanmıştır.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?>? _execute;
        private readonly Predicate<object?>? _canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// RelayCommand oluşturur.
        /// </summary>
        /// <param name="execute">Komut çalıştırıldığında yapılacak işlem.</param>
        /// <param name="canExecute">Komutun yürütülebilir olup olmadığını kontrol eden yüklem (predicate). Null ise her zaman yürütülebilir.</param>
        public RelayCommand(Action<object?>? execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute?.Invoke(parameter);
        }
    }
}
