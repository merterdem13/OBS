using OBS.Models;
using OBS.Services;
using Xunit;

namespace OBS.Tests.Services;

public class StudentListFlowServiceTests
{
    [Fact]
    public void BuildRefreshResult_returns_empty_state_when_no_filter_is_active()
    {
        var service = new StudentListFlowService();
        var rawStudents = new[]
        {
            new Student { StudentNumber = "1", FirstName = "Ali", LastName = "Veli" }
        };

        var result = service.BuildRefreshResult(
            searchText: string.Empty,
            selectedClass: null,
            isFavoriteMode: false,
            rawStudents: rawStudents,
            favoriteNumbers: [],
            pageSize: 6);

        Assert.True(result.ShouldClearCurrentStudents);
        Assert.Empty(result.AllStudents);
        Assert.Empty(result.VisibleStudents);
        Assert.Equal(0, result.LoadedCount);
        Assert.False(result.HasMoreStudents);
    }

    [Fact]
    public void BuildRefreshResult_projects_students_marks_favorites_and_loads_first_page()
    {
        var service = new StudentListFlowService();
        var rawStudents = Enumerable.Range(1, 7)
            .Select(number => new Student
            {
                StudentNumber = number.ToString(),
                FirstName = $"Ad{number}",
                LastName = $"Soyad{number}"
            })
            .ToArray();

        var result = service.BuildRefreshResult(
            searchText: "ad",
            selectedClass: null,
            isFavoriteMode: false,
            rawStudents: rawStudents,
            favoriteNumbers: ["2", "6"],
            pageSize: 3);

        Assert.False(result.ShouldClearCurrentStudents);
        Assert.Equal(7, result.AllStudents.Count);
        Assert.Equal(3, result.VisibleStudents.Count);
        Assert.Equal(3, result.LoadedCount);
        Assert.True(result.HasMoreStudents);
        Assert.False(result.VisibleStudents[0].IsFavorite);
        Assert.True(result.VisibleStudents[1].IsFavorite);
        Assert.Equal(0, result.VisibleStudents[0].StaggerDelay);
        Assert.Equal(50, result.VisibleStudents[1].StaggerDelay);
        Assert.Equal(100, result.VisibleStudents[2].StaggerDelay);
    }

    [Fact]
    public void LoadNextPage_returns_next_batch_with_updated_state()
    {
        var service = new StudentListFlowService();
        var refreshResult = service.BuildRefreshResult(
            searchText: "12",
            selectedClass: null,
            isFavoriteMode: false,
            rawStudents: Enumerable.Range(1, 7)
                .Select(number => new Student
                {
                    StudentNumber = number.ToString(),
                    FirstName = $"Ad{number}",
                    LastName = $"Soyad{number}"
                }),
            favoriteNumbers: [],
            pageSize: 3);

        var nextPage = service.LoadNextPage(refreshResult.AllStudents, refreshResult.LoadedCount, pageSize: 3);

        Assert.Equal(3, nextPage.Students.Count);
        Assert.Equal("4", nextPage.Students[0].StudentNumber);
        Assert.Equal("6", nextPage.Students[2].StudentNumber);
        Assert.Equal(6, nextPage.LoadedCount);
        Assert.True(nextPage.HasMoreStudents);
        Assert.Equal(0, nextPage.Students[0].StaggerDelay);
        Assert.Equal(50, nextPage.Students[1].StaggerDelay);
        Assert.Equal(100, nextPage.Students[2].StaggerDelay);
    }
}
