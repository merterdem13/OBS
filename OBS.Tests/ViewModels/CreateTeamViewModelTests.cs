using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OBS.Models;
using OBS.ViewModels;
using Xunit;

namespace OBS.Tests.ViewModels;

public sealed class CreateTeamViewModelTests
{
    [Fact]
    public async Task CreateAsync_sets_error_when_team_name_is_blank()
    {
        var vm = new CreateTeamViewModelFactory().Create();
        vm.TeamName = "   ";

        await vm.CreateAsync();

        Assert.Equal("Takım adı boş bırakılamaz.", vm.ErrorMessage);
    }

    [Fact]
    public async Task CreateAsync_sets_error_when_team_name_exists()
    {
        var vm = new CreateTeamViewModelFactory { TeamExists = true }.Create();
        vm.TeamName = "A Takımı";

        await vm.CreateAsync();

        Assert.Equal("Bu isimde bir takım zaten mevcut.", vm.ErrorMessage);
    }

    [Fact]
    public void UpdateRequiredGender_sets_null_for_karma()
    {
        var vm = new CreateTeamViewModelFactory().Create();
        vm.SelectedCategory = "Belirtilmemiş";
        vm.SelectedGenderMode = "Karma";

        vm.UpdateRequiredGender();

        Assert.Null(vm.RequiredGender);
    }

    [Fact]
    public async Task ToggleFavoritesAsync_loads_only_eligible_favorite_students()
    {
        var factory = new CreateTeamViewModelFactory
        {
            Students =
            [
                CreateStudent("1", "Ali", "Erkek"),
                CreateStudent("2", "Ayse", "Kız"),
                CreateStudent("3", "Can", "Erkek")
            ],
            FavoriteStudentNumbers = ["1", "2", "3"],
            ExistingTeamsByStudentNumber = new Dictionary<string, string?>
            {
                ["3"] = "Mevcut Takım"
            }
        };

        var vm = factory.Create();

        await vm.ToggleFavoritesAsync();

        Assert.True(vm.IsFavoritesMode);
        Assert.Equal(["1"], vm.SearchResults.Select(student => student.StudentNumber).ToArray());
    }

    [Fact]
    public void UpdateRequiredGender_removes_pending_members_that_no_longer_match_the_rule()
    {
        var factory = new CreateTeamViewModelFactory();
        var vm = factory.Create();
        vm.PendingMembers.Add(CreateStudent("1", "Ali", "Erkek"));
        vm.PendingMembers.Add(CreateStudent("2", "Ayse", "Kız"));
        vm.SelectedGenderMode = "Kız";

        vm.UpdateRequiredGender();

        Assert.Equal(["2"], vm.PendingMembers.Select(student => student.StudentNumber).ToArray());
        Assert.Contains(factory.ToastMessages, message => message.Contains("listeden çıkarıldı."));
    }

    [Fact]
    public async Task CreateAsync_creates_team_adds_pending_members_clears_form_and_requests_close()
    {
        var factory = new CreateTeamViewModelFactory();
        var vm = factory.Create();
        vm.TeamName = "Yeni Takım";
        vm.SelectedCategory = "Futbol";
        vm.PendingMembers.Add(CreateStudent("1", "Ali", "Erkek"));
        vm.PendingMembers.Add(CreateStudent("2", "Veli", "Erkek"));

        await vm.CreateAsync();

        Assert.NotNull(factory.InsertedTeam);
        Assert.Equal("Yeni Takım", factory.InsertedTeam!.TeamName);
        Assert.Equal("Futbol", factory.InsertedTeam.Category);
        Assert.Equal(["1", "2"], factory.AddedMembers.Select(entry => entry.StudentNumber).ToArray());
        Assert.Equal(string.Empty, vm.TeamName);
        Assert.Empty(vm.PendingMembers);
        Assert.True(factory.CloseRequested);
        Assert.Contains(factory.ToastMessages, message => message.Contains("başarıyla oluşturuldu."));
    }

    [Fact]
    public async Task CreateAsync_does_not_emit_gender_removal_toasts_while_clearing_the_form()
    {
        var factory = new CreateTeamViewModelFactory();
        var vm = factory.Create();
        vm.TeamName = "Yeni Takım";
        vm.SelectedGenderMode = "Kız";
        vm.PendingMembers.Add(CreateStudent("2", "Ayse", "Kız"));

        await vm.CreateAsync();

        Assert.DoesNotContain(factory.ToastMessages, message => message.Contains("listeden çıkarıldı."));
    }

    [Fact]
    public async Task ToggleFavoritesAsync_cancels_in_flight_search_so_stale_results_do_not_overwrite_favorites()
    {
        var factory = new CreateTeamViewModelFactory
        {
            Students =
            [
                CreateStudent("1", "Ali", "Erkek"),
                CreateStudent("11", "Veli", "Erkek")
            ],
            FavoriteStudentNumbers = ["1"]
        };
        var delay = new ControlledSearchDelay();
        factory.SearchDelayAsync = delay.WaitAsync;
        var vm = factory.Create();

        vm.SearchText = "11";
        await delay.Started.Task;
        await vm.ToggleFavoritesAsync();
        delay.Release();
        await delay.Completed.Task;
        await Task.Yield();

        Assert.True(vm.IsFavoritesMode);
        Assert.Equal(["1"], vm.SearchResults.Select(student => student.StudentNumber).ToArray());
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

    private sealed class CreateTeamViewModelFactory
    {
        public bool TeamExists { get; set; }

        public List<Student> Students { get; set; } = [];

        public List<string> FavoriteStudentNumbers { get; set; } = [];

        public Dictionary<string, string?> ExistingTeamsByStudentNumber { get; set; } = [];

        public Team? InsertedTeam { get; private set; }

        public List<(int TeamId, string StudentNumber)> AddedMembers { get; } = [];

        public List<string> ToastMessages { get; } = [];

        public bool CloseRequested { get; private set; }

        public Func<CancellationToken, Task> SearchDelayAsync { get; set; } = _ => Task.CompletedTask;

        public CreateTeamViewModel Create()
        {
            var vm = new CreateTeamViewModel(
                teamExists: _ => TeamExists,
                insertTeam: team =>
                {
                    InsertedTeam = team;
                    team.Id = 42;
                },
                addMember: (teamId, studentNumber) => AddedMembers.Add((teamId, studentNumber)),
                getTeamNameForStudent: studentNumber => ExistingTeamsByStudentNumber.GetValueOrDefault(studentNumber),
                searchStudents: keyword => Students.Where(student => student.StudentNumber.Contains(keyword)),
                getAllStudents: () => Students,
                getStudentByNumber: studentNumber => Students.FirstOrDefault(student => student.StudentNumber == studentNumber),
                getFavoriteStudentNumbers: () => FavoriteStudentNumbers,
                searchDelayAsync: SearchDelayAsync);

            vm.ToastRequested += (_, message) => ToastMessages.Add(message.Message);
            vm.CloseRequested += (_, hasChanges) => CloseRequested = hasChanges;
            return vm;
        }
    }

    private sealed class ControlledSearchDelay
    {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Started => _started;

        public TaskCompletionSource Completed => _completed;

        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            _started.TrySetResult();

            using var registration = cancellationToken.Register(() =>
            {
                _completed.TrySetResult();
                _release.TrySetCanceled(cancellationToken);
            });

            try
            {
                await _release.Task;
            }
            catch (TaskCanceledException)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            _completed.TrySetResult();
        }

        public void Release()
        {
            _release.TrySetResult();
        }
    }
}
