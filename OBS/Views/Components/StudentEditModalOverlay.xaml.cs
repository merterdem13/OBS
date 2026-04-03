using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Win32;

namespace OBS.Views.Components
{
    public partial class StudentEditModalOverlay : UserControl
    {
        public StudentEditModalOverlay()
        {
            InitializeComponent();
        }

        private void OnChangePhotoClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Fotoğraf Seçin",
                Filter = "Image Files|*.png;*.jpg;*.jpeg",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                var gs = OBS.ViewModels.GlobalState.Instance;
                gs.EditPhotoPath = dialog.FileName;
            }
        }

        private void OnBirthDateClick(object sender, MouseButtonEventArgs e)
        {
            BirthDatePickerPopup.IsOpen = !BirthDatePickerPopup.IsOpen;
        }

        private void OnCalendarButtonClick(object sender, RoutedEventArgs e)
        {
            BirthDatePickerPopup.IsOpen = !BirthDatePickerPopup.IsOpen;
        }

        private void OnCalendarSelected(object sender, SelectionChangedEventArgs e)
        {
            if (EditCalendar.SelectedDate.HasValue)
            {
                BirthDatePickerPopup.IsOpen = false;
            }
        }
    }
}
