using System.Collections.Generic;
using System.Linq;
using OBS.Models;
using OBS.ViewModels;

namespace OBS.Services
{
    public sealed class StudentListFlowService
    {
        public StudentListRefreshResult BuildRefreshResult(
            string searchText,
            string? selectedClass,
            bool isFavoriteMode,
            IEnumerable<Student> rawStudents,
            HashSet<string> favoriteNumbers,
            int pageSize)
        {
            if (!isFavoriteMode && string.IsNullOrWhiteSpace(searchText) && string.IsNullOrWhiteSpace(selectedClass))
            {
                return new StudentListRefreshResult([], [], 0, false, true);
            }

            var allStudents = rawStudents
                .Select(student => new StudentViewModel(student, favoriteNumbers.Contains(student.StudentNumber)))
                .ToList();

            var firstPage = LoadNextPage(allStudents, 0, pageSize);
            return new StudentListRefreshResult(
                allStudents,
                firstPage.Students,
                firstPage.LoadedCount,
                firstPage.HasMoreStudents,
                false);
        }

        public StudentListPage LoadNextPage(IReadOnlyList<StudentViewModel> allStudents, int loadedCount, int pageSize)
        {
            var nextBatch = allStudents
                .Skip(loadedCount)
                .Take(pageSize)
                .ToList();

            var delay = 0;
            foreach (var student in nextBatch)
            {
                student.StaggerDelay = delay;
                delay += 50;
            }

            var nextLoadedCount = loadedCount + nextBatch.Count;
            return new StudentListPage(nextBatch, nextLoadedCount, nextLoadedCount < allStudents.Count);
        }
    }

    public sealed record StudentListRefreshResult(
        List<StudentViewModel> AllStudents,
        List<StudentViewModel> VisibleStudents,
        int LoadedCount,
        bool HasMoreStudents,
        bool ShouldClearCurrentStudents);

    public sealed record StudentListPage(
        List<StudentViewModel> Students,
        int LoadedCount,
        bool HasMoreStudents);
}
