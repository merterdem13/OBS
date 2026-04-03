using System.IO;
using Xunit;

namespace OBS.Tests.Views;

public sealed class TeamManagementViewWiringTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void CreateTeamComponent_does_not_set_local_user_control_datacontext()
    {
        var xaml = File.ReadAllText(Path.Combine(RepoRoot, "OBS", "Views", "Components", "CreateTeamComponent.xaml"));

        Assert.DoesNotContain("<UserControl.DataContext>", xaml);
    }

    [Fact]
    public void EditTeamComponent_does_not_construct_view_model_through_datacontext()
    {
        var codeBehind = File.ReadAllText(Path.Combine(RepoRoot, "OBS", "Views", "Components", "EditTeamComponent.xaml.cs"));

        Assert.DoesNotContain("DataContext = new EditTeamViewModel()", codeBehind);
    }

    [Fact]
    public void TeamManagementView_binds_child_components_through_view_model_properties()
    {
        var xaml = File.ReadAllText(Path.Combine(RepoRoot, "OBS", "Views", "TeamManagementView.xaml"));

        Assert.Contains("ViewModel=\"{Binding CreateTeamViewModel}\"", xaml);
        Assert.Contains("ViewModel=\"{Binding EditTeamViewModel}\"", xaml);
        Assert.DoesNotContain("DataContext=\"{Binding}\"", xaml);
    }
}
