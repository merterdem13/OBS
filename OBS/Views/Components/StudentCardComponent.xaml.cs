namespace OBS.Views.Components
{
    public partial class StudentCardComponent
    {
        public StudentCardComponent()
        {
            InitializeComponent();
        }

        private void BtnSaveNote_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.StudentViewModel vm)
            {
                var repo = new DataAccess.StudentRepository();
                repo.Upsert(vm.GetModel());
                btnNote.IsChecked = false;
            }
        }
    }
}
