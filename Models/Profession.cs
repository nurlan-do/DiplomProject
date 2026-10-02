namespace DiplomBackend.Models // Или ваше пространство имен
{
    public class Profession
    {
        public int Id { get; set; }

        // Основное имя (если в сервисах ищется Name, сделаем оба или приведем к единому)
        public string Title { get; set; } = string.Empty;
        public string Name
        {
            get => Title;
            set => Title = value;
        }

        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Свойства, которые требуются в RecommendationService
        public string Interests { get; set; } = string.Empty;
        public string RequiredSkills { get; set; } = string.Empty;

        // Конструктор по умолчанию (обязателен для Entity Framework Core и инициализаторов {})
        public Profession() { }

        // Конструктор с параметрами (если вы хотите создавать через new Profession("...", "..."))
        public Profession(string title, string category, string description)
        {
            Title = title;
            Category = category;
            Description = description;
        }
    }
}