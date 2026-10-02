namespace DiplomBackend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? City { get; set; } // Добавили поле для города
        public ICollection<Profession> SavedProfessions { get; set; } = new List<Profession>();
    }
}