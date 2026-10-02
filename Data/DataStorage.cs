using System.Collections.Generic;
using DiplomBackend.Models;

namespace DiplomBackend.Data
{
    public static class DataStorage
    {
        // 1. Создаем объекты через явный конструктор Profession(id, name, description)
        public static List<Profession> Professions { get; set; } = new List<Profession>
        {
            new Profession { Title = "Frontend", Category = "IT", Description = "..." }
        };

        // 2. Добавляем коллекцию Users, которой не хватало для AuthService.cs
        public static List<User> Users { get; set; } = new List<User>();
    }
}
