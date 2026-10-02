using DiplomBackend.Models;
using DiplomBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Google.Apis.Auth;

Console.WriteLine("=== СЕРВЕР ЗАПУЩЕН И ВИДИТ ЭТОТ ФАЙЛ ===");

var builder = WebApplication.CreateBuilder(args);

// 1. Подключение к PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Настройка аутентификации (куки + Google)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
        ?? throw new InvalidOperationException("Google ClientId is missing");
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
        ?? throw new InvalidOperationException("Google ClientSecret is missing");
});

// Добавление сервисов
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Настройка CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


var app = builder.Build();

// Смягчение политики COOP для бесшовного Google OAuth во всплывающем окне
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("Cross-Origin-Opener-Policy", "unsafe-none");
    context.Response.Headers.Append("Cross-Origin-Embedder-Policy", "unsafe-none");
    await next();
});


// 2. Автоматическое применение миграций и сидинг стартовых данных
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();

    if (!dbContext.Professions.Any())
    {
        dbContext.Professions.AddRange(
            new Profession { Title = "Frontend-разработчик", Category = "IT", Description = "Создание пользовательских интерфейсов с использованием HTML, CSS и JavaScript." },
            new Profession { Title = ".NET Разработчик", Category = "IT", Description = "Разработка надежных серверных приложений на C# и .NET 8." },
            new Profession { Title = "UI/UX Дизайнер", Category = "Дизайн", Description = "Проектирование пользовательских интерфейсов и улучшение пользовательского опыта." }
        );
        dbContext.SaveChanges();
    }
}

// Конфигурация конвейера HTTP-запросов (Middleware)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Маппинг контроллеров
app.MapControllers();
app.MapFallbackToFile("index.html");

// ==========================================
// ЭНДПОИНТЫ АВТОРИЗАЦИИ И ПОЛЬЗОВАТЕЛЕЙ
// ==========================================

// Бесшовный вход по токену от Google (без переходов со страницы)
app.MapPost("/api/auth/google-token", async (HttpContext context, HttpRequest request) =>
{
    var body = await request.ReadFromJsonAsync<GoogleTokenModel>();
    if (body?.Token == null) return Results.BadRequest(new { message = "Token is missing" });

    try
    {
        var payload = await GoogleJsonWebSignature.ValidateAsync(body.Token);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, payload.Name ?? "User"),
            new Claim(ClaimTypes.Email, payload.Email ?? string.Empty),
            new Claim("Picture", payload.Picture ?? string.Empty)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

        return Results.Ok(new { success = true, name = payload.Name, email = payload.Email });
    }
    catch (Exception)
    {
        return Results.BadRequest(new { message = "Invalid Google token" });
    }
});

app.MapPost("/api/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok(new { success = true });
});

app.MapGet("/api/auth/user", async (HttpContext context, AppDbContext db) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var email = context.User.FindFirst(ClaimTypes.Email)?.Value;
        var dbUser = await db.Users.FirstOrDefaultAsync(u => u.Email == email);

        return Results.Ok(new
        {
            isAuthenticated = true,
            name = dbUser?.FullName ?? context.User.FindFirst(ClaimTypes.Name)?.Value,
            email = email,
            city = dbUser?.City ?? "Не указан"
        });
    }
    return Results.Ok(new { isAuthenticated = false });
});

app.MapPost("/api/user/city", async (HttpContext context, AppDbContext db) =>
{
    if (context.User.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();

    // Универсально забираем email из любых клеймов
    var email = context.User.FindFirst(ClaimTypes.Email)?.Value
                ?? context.User.Identity?.Name;

    if (string.IsNullOrEmpty(email))
        return Results.Unauthorized();

    var body = await context.Request.ReadFromJsonAsync<UpdateCityModel>();

    // Ищем пользователя, а если его нет в базе — создаем на лету (защита от 404/NullReference)
    var dbUser = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
    if (dbUser == null)
    {
        dbUser = new User
        {
            Email = email,
            FullName = context.User.FindFirst(ClaimTypes.Name)?.Value ?? "Пользователь",
            City = body?.City
        };
        db.Users.Add(dbUser);
    }
    else
    {
        dbUser.City = body?.City;
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { success = true, city = dbUser.City });
});

//app.MapPost("/api/user/city", () =>
//{
//    Console.WriteLine("=== ЭНДПОИНТ /api/user/city СРАБОТАЛ ===");
//    return Results.Ok(new { success = true, message = "Маршрут работает!" });
//});

// Сохранить профессию в профиль пользователя
app.MapPost("/api/user/professions", async (SaveProfessionModel model, HttpContext context, AppDbContext db) =>
{
    if (context.User.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();

    var email = context.User.FindFirst(ClaimTypes.Email)?.Value
                ?? context.User.Identity?.Name;

    var user = await db.Users
        .Include(u => u.SavedProfessions)
        .FirstOrDefaultAsync(u => u.Email == email);

    if (user == null)
        return Results.Unauthorized();

    var profession = await db.Professions.FindAsync(model.ProfessionId);
    if (profession == null)
        return Results.NotFound("Профессия не найдена");

    if (!user.SavedProfessions.Any(p => p.Id == profession.Id))
    {
        user.SavedProfessions.Add(profession);
        await db.SaveChangesAsync();
    }

    return Results.Ok(user.SavedProfessions);
});

// Получить сохраненные профессии пользователя
app.MapGet("/api/user/professions", async (HttpContext context, AppDbContext db) =>
{
    if (context.User.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();

    var email = context.User.FindFirst(ClaimTypes.Email)?.Value
                ?? context.User.Identity?.Name;

    var user = await db.Users
        .Include(u => u.SavedProfessions)
        .FirstOrDefaultAsync(u => u.Email == email);

    if (user == null)
        return Results.Unauthorized();

    return Results.Ok(user.SavedProfessions);
});

app.MapDelete("/api/user/professions/{id}", async (int id, HttpContext context, AppDbContext db) =>
{
    if (context.User.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();

    var email = context.User.FindFirst(ClaimTypes.Email)?.Value
                ?? context.User.Identity?.Name;

    var user = await db.Users
        .Include(u => u.SavedProfessions)
        .FirstOrDefaultAsync(u => u.Email == email);

    if (user == null)
        return Results.Unauthorized();

    var profession = user.SavedProfessions.FirstOrDefault(p => p.Id == id);
    if (profession != null)
    {
        user.SavedProfessions.Remove(profession);
        await db.SaveChangesAsync();
    }

    return Results.Ok(user.SavedProfessions);
});

app.MapPost("/api/recommendations/calculate", async (QuizSubmissionModel model, AppDbContext db) =>
{
    if (model.SelectedCategories == null || !model.SelectedCategories.Any())
        return Results.BadRequest(new { message = "Не выбраны критерии для подбора." });

    // Ищем профессии, которые совпадают с выбранными пользователем категориями
    var matchedProfessions = await db.Professions
        .Where(p => model.SelectedCategories.Contains(p.Category))
        .Take(3) // Берем топ-3 подходящих варианта
        .ToListAsync();

    // Если по точным категориям ничего не нашлось, отдаем базовую подборку
    if (!matchedProfessions.Any())
    {
        matchedProfessions = await db.Professions.Take(3).ToListAsync();
    }

    return Results.Ok(matchedProfessions);
});

app.Run();

public record UpdateCityModel(string City);
public record GoogleTokenModel(string Token);
public record SaveProfessionModel(int ProfessionId);