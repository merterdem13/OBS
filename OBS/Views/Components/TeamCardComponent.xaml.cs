namespace OBS.Views.Components
{
    public partial class TeamCardComponent
    {
        public TeamCardComponent()
        {
            InitializeComponent();
        }

        private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (DataContext is ViewModels.TeamCardViewModel vm && vm.IsDeleteConfirming)
            {
                vm.IsDeleteConfirming = false;
            }
        }
    }
}
