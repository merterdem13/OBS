using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OBS.Models;
using OBS.ViewModels;
using Xunit;

namespace OBS.Tests.ViewModels;

public sealed class EditTeamViewModelTests
{
    [Fact]
    public async Task SaveNameAsync_sets_error_when_name_is_blank()
    {
        var factory = new EditTeamViewModelFactory();
        var vm = factory.Create();
        vm.TeamName = "   ";

        await vm.SaveNameAsync();

        Assert.Equal("Takım adı boş bırakılamaz.", vm.ErrorMessage);
        Assert.Contains(factory.ToastErrors, message => message == "Takım adı boş bırakılamaz.");
    }

    [Fact]
    public async Task SaveNameAsync_sets_error_when_name_exists_for_another_team()
    {
        var factory = new EditTeamViewModelFactory { TeamExists = true };
        var vm = factory.Create();
        vm.OriginalName = "Eski Ad";
        vm.TeamName = "Yeni Ad";

        await vm.SaveNameAsync();

        Assert.Equal("Bu isimde bir takım zaten mevcut.", vm.ErrorMessage);
        Assert.Contains(factory.ToastErrors, message => message == "Bu isimde bir takım zaten mevcut.");
    }

    [Fact]
    public void ResolveRequiredGender_returns_null_for_karma_category()
    {
        Assert.Null(EditTeamViewModel.ResolveRequiredGender("Futbol (Karma)"));
    }

    [Fact]
    public void LoadTeam_populates_team_state_and_members()
    {
        var factory = new EditTeamViewModelFactory
        {
            Members =
            [
                CreateStudent("1", "Ali", "Erkek"),
                CreateStudent("2", "Veli", "Erkek")
            ]
        };
        var vm = factory.Create();

        vm.LoadTeam(new TeamCardViewModel(new Team { Id = 42, TeamName = "Kartallar", Category = "Futbol" }));

        Assert.Equal(42, vm.TeamId);
        Assert.Equal("Kartallar", vm.TeamName);
        Assert.Equal("Kartallar", vm.OriginalName);
        Assert.Equal("Futbol", vm.Category);
        Assert.Equal("Erkek", vm.RequiredGender);
        Assert.Equal(["1", "2"], vm.Members.Select(member => member.StudentNumber).ToArray());
        Assert.Equal("Mevcut Üyeler (2)", vm.MembersHeaderText);
    }

    [Fact]
    public async Task SaveNameAsync_updates_name_marks_changes_and_requests_success_toast()
    {
        var factory = new EditTeamViewModelFactory();
        var vm = factory.Create();
        vm.LoadTeam(new TeamCardViewModel(new Team { Id = 9, TeamName = "Eski Ad", Category = "Futbol" }));
        vm.TeamName = "Yeni Ad";

        await vm.SaveNameAsync();

        Assert.Equal((9, "Yeni Ad"), factory.UpdatedName);
        Assert.True(vm.HasChanges);
        Assert.Equal("Yeni Ad", vm.OriginalName);
        Assert.Contains(factory.ToastMessages, message => message == "Takım adı güncellendi.");
    }

    [Fact]
    public async Task RefreshSearchResultsAsync_returns_only_eligible_students()
    {
        var factory = new EditTeamViewModelFactory
        {
            Members = [CreateStudent("1", "Ali", "Erkek")],
            Students =
            [
                CreateStudent("1", "Ali", "Erkek"),
                CreateStudent("2", "Ayse", "Kız"),
                CreateStudent("3", "Can", "Erkek"),
                CreateStudent("4", "Cem", "Erkek")
            ],
            ExistingTeamsByStudentNumber = new Dictionary<string, string?>
            {
                ["4"] = "Diger Takim"
            }
        };
        var vm = factory.Create();
        vm.LoadTeam(new TeamCardViewModel(new Team { Id = 1, TeamName = "Kartallar", Category = "Futbol" }));
        vm.SearchText = "Ca";

        await vm.RefreshSearchResultsAsync();

        Assert.Equal(["3"], vm.SearchResults.Select(student => student.StudentNumber).ToArray());
    }

    [Fact]
    public void AddStudent_adds_member_refreshes_search_results_and_marks_changes()
    {
        var factory = new EditTeamViewModelFactory
        {
            Members = [CreateStudent("1", "Ali", "Erkek")],
            Students =
            [
                CreateStudent("1", "Ali", "Erkek"),
                CreateStudent("3", "Can", "Erkek")
            ]
        };
        var vm = factory.Create();
        vm.LoadTeam(new TeamCardViewModel(new Team { Id = 7, TeamName = "Kartallar", Category = "Futbol" }));
        vm.SearchText = "Ca";
        vm.RefreshSearchResults();

        vm.AddStudent(factory.Students.Single(student => student.StudentNumber == "3"));

        Assert.Equal((7, "3"), factory.AddedMember);
        Assert.Equal(["1", "3"], vm.Members.Select(member => member.StudentNumber).ToArray());
        Assert.Empty(vm.SearchResults);
        Assert.True(vm.HasChanges);
        Assert.Contains(factory.ToastMessages, message => message == "Öğrenci takıma eklendi.");
    }

    [Fact]
    public void RemoveMember_removes_student_refreshes_search_results_and_marks_changes()
    {
        var factory = new EditTeamViewModelFactory
        {
            Members =
            [
                CreateStudent("1", "Ali", "Erkek"),
                CreateStudent("3", "Can", "Erkek")
            ],
            Students =
            [
                CreateStudent("1", "Ali", "Erkek"),
                CreateStudent("3", "Can", "Erkek")
            ]
        };
        var vm = factory.Create();
        vm.LoadTeam(new TeamCardViewModel(new Team { Id = 7, TeamName = "Kartallar", Category = "Futbol" }));
        vm.SearchText = "Ca";

        vm.RemoveMember(factory.Members.Single(student => student.StudentNumber == "3"));

        Assert.Equal((7, "3"), factory.RemovedMember);
        Assert.Equal(["1"], vm.Members.Select(member => member.StudentNumber).ToArray());
        Assert.Equal(["3"], vm.SearchResults.Select(student => student.StudentNumber).ToArray());
        Assert.True(vm.HasChanges);
        Assert.Contains(factory.ToastMessages, message => message == "Öğrenci takımdan çıkarıldı.");
    }

    [Fact]
    public async Task ToggleFavoritesAsync_loads_only_eligible_favorite_students()
    {
        var factory = new EditTeamViewModelFactory
        {
            Members = [CreateStudent("1", "Ali", "Erkek")],
            Students =
            [
                CreateStudent("1", "Ali", "Erkek"),
                CreateStudent("2", "Ayse", "Kız"),
                CreateStudent("3", "Can", "Erkek"),
                CreateStudent("4", "Cem", "Erkek")
            ],
            FavoriteStudentNumbers = ["1", "2", "3", "4"],
            ExistingTeamsByStudentNumber = new Dictionary<string, string?>
            {
                ["4"] = "Diger Takim"
            }
        };
        var vm = factory.Create();
        vm.LoadTeam(new TeamCardViewModel(new Team { Id = 3, TeamName = "Kartallar", Category = "Futbol" }));

        await vm.ToggleFavoritesAsync();

        Assert.True(vm.IsFavoritesMode);
        Assert.Equal(["3"], vm.SearchResults.Select(student => student.StudentNumber).ToArray());
    }

    [Fact]
    public async Task RefreshSearchResultsAsync_sets_error_and_requests_toast_when_search_throws()
    {
        var factory = new EditTeamViewModelFactory
        {
            SearchStudents = _ => throw new InvalidOperationException("Arama bozuldu")
        };
        var vm = factory.Create();
        vm.LoadTeam(new TeamCardViewModel(new Team { Id = 3, TeamName = "Kartallar", Category = "Futbol" }));
        vm.SearchText = "Ca";

        await vm.RefreshSearchResultsAsync();

        Assert.Equal("Arama bozuldu", vm.ErrorMessage);
        Assert.Contains(factory.ToastErrors, message => message == "Arama bozuldu");
    }

    [Fact]
    public void Replacing_members_collection_keeps_members_header_notifications_in_sync()
    {
        var vm = new EditTeamViewModelFactory().Create();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != null)
            {
                changedProperties.Add(args.PropertyName);
            }
        };

        vm.Members = [CreateStudent("1", "Ali", "Erkek")];
        changedProperties.Clear();

        vm.Members.Add(CreateStudent("2", "Veli", "Erkek"));

        Assert.Contains(nameof(EditTeamViewModel.MembersHeaderText), changedProperties);
    }

    [Fact]
    public void Replacing_search_results_collection_keeps_visibility_notifications_in_sync()
    {
        var vm = new EditTeamViewModelFactory().Create();
        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != null)
            {
                changedProperties.Add(args.PropertyName);
            }
        };

        vm.SearchResults = [];
        changedProperties.Clear();

        vm.SearchResults.Add(CreateStudent("1", "Ali", "Erkek"));

        Assert.Contains(nameof(EditTeamViewModel.HasSearchResults), changedProperties);
    }

    private static Student CreateStudent(string studentNumber, string firstName, string gender)
    {
        return new Student
        {
            StudentNumber = studentNumber,
            FirstName = firstName,
            LastName = "Test",
            Gender = gender,
            Class = "10-A"
        };
    }

    private sealed class EditTeamViewModelFactory
    {
        public bool TeamExists { get; set; }

        public List<Student> Members { get; set; } = [];

        public List<Student> Students { get; set; } = [];

        public List<string> FavoriteStudentNumbers { get; set; } = [];

        public Dictionary<string, string?> ExistingTeamsByStudentNumber { get; set; } = [];

        public (int TeamId, string TeamName)? UpdatedName { get; private set; }

        public (int TeamId, string StudentNumber)? AddedMember { get; private set; }

        public (int TeamId, string StudentNumber)? RemovedMember { get; private set; }

        public List<string> ToastMessages { get; } = [];

        public List<string> ToastErrors { get; } = [];

        public Func<CancellationToken, Task> SearchDelayAsync { get; set; } = _ => Task.CompletedTask;

        public Func<string, IEnumerable<Student>>? SearchStudents { get; set; }

        public EditTeamViewModel Create()
        {
            var vm = new EditTeamViewModel(
                teamExists: _ => TeamExists,
                updateName: (teamId, teamName) => UpdatedName = (teamId, teamName),
                getMembers: _ => Members,
                removeMember: (teamId, studentNumber) =>
                {
                    RemovedMember = (teamId, studentNumber);
                    Members = Members.Where(student => student.StudentNumber != studentNumber).ToList();
                },
                addMember: (teamId, studentNumber) =>
                {
                    AddedMember = (teamId, studentNumber);
                    var student = Students.Single(entry => entry.StudentNumber == studentNumber);
                    Members = [.. Members, student];
                },
                getTeamNameForStudent: studentNumber => ExistingTeamsByStudentNumber.GetValueOrDefault(studentNumber),
                searchStudents: SearchStudents ?? (keyword => Students.Where(student =>
                    student.StudentNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    student.FirstName.Contains(keyword, StringComparison.OrdinalIgnoreCase))),
                getAllStudents: () => Students,
                getFavoriteStudentNumbers: () => FavoriteStudentNumbers,
                searchDelayAsync: SearchDelayAsync);

            vm.ToastRequested += (_, message) =>
            {
                ToastMessages.Add(message.Message);
                if (message.IsError)
                {
                    ToastErrors.Add(message.Message);
                }
            };
            return vm;
        }
    }
}
