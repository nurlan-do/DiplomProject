using DiplomBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using DiplomBackend.Data;

[ApiController]
[Route("api/[controller]")]
public class ProfessionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProfessionsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Profession>>> GetProfessions()
    {
        // Данные теперь берутся напрямую из PostgreSQL
        return await _context.Professions.ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Profession>> CreateProfession([FromBody] Profession profession)
    {
        _context.Professions.Add(profession);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProfessions), new { id = profession.Id }, profession);
    }
}