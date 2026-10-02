using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiplomBackend.Data;
using DiplomBackend.Models;

namespace DiplomBackend.Services
{
    public class ProfessionService
    {
        // Добавим метод GetAllAsync, если его не хватало для других сервисов
        public async Task<List<Profession>> GetAllAsync()
        {
            return DataStorage.Professions.ToList();
        }

        public async Task<List<Profession>> SearchProfessionsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await GetAllAsync();
            }

            var trimmedQuery = query.Trim().ToLower();

            // Если в вашей модели Profession свойство называется Name, а не Title:
            return DataStorage.Professions
                .Where(p => (p.Name != null && p.Name.ToLower().Contains(trimmedQuery)) ||
                            (p.Description != null && p.Description.ToLower().Contains(trimmedQuery)))
                .ToList();
        }
        public List<Profession> GetAll()
        {
            return DataStorage.Professions.ToList();
        }
    }
}