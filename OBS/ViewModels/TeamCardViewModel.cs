using CommunityToolkit.Mvvm.ComponentModel;
using OBS.Models;

namespace OBS.ViewModels
{
    public partial class TeamCardViewModel : ObservableObject
    {
        private readonly Team _team;

        public TeamCardViewModel(Team team)
        {
            _team = team;
        }

        public int Id => _team.Id;
        public string TeamName => _team.TeamName;
        public string Category => _team.Category;
        public string? MatchDate => _team.MatchDate;
        public string? Description => _team.Description;
        public int MemberCount => _team.MemberCount;

        public string MemberCountText => $"{MemberCount} Oyuncu";

        public string CategoryIcon => Category switch
        {
            "Futbol" or "Futbol (Kadınlar)" or "Futbol (Karma)" => "⚽",
            "Futsal" or "Futsal (Kadınlar)" or "Futsal (Karma)" => "⚽",
            "Basketbol" or "Basketbol (Kadınlar)" or "Basketbol (Karma)" => "🏀",
            "Hentbol" or "Hentbol (Kadınlar)" or "Hentbol (Karma)" => "🤾",
            "Voleybol" or "Voleybol (Kadınlar)" or "Voleybol (Karma)" => "🏐",
            _ => "🏅"
        };

        [ObservableProperty]
        private bool _isRemoving;

        public Team GetModel() => _team;
    }
}
