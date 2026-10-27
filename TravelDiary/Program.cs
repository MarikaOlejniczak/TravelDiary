using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TravelDiary.Data;
using TravelDiary.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TravelDiary.Filters;

var builder = WebApplication.CreateBuilder(args);

// Kontrolery API
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidateCsrfFilter>();
});


builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";

    options.Cookie.Name = "TravelDiary.CSRF";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Wpisz token JWT (bez s³owa Bearer)"
        });

    options.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] { }
            }
        });
});

// Baza danych SQL Server
builder.Services.AddDbContext<TravelDiaryDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ASP.NET Core Identity
builder.Services.AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<TravelDiaryDbContext>();

// Uwierzytelnianie JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = "TravelDiary",
        ValidAudience = "TravelDiary",

        IssuerSigningKey = new SymmetricSecurityKey(
    Encoding.UTF8.GetBytes(
        builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException(
            "Brakuje Jwt:Key w konfiguracji."))),

        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        // NOWOŒÆ: odczyt tokenu z ciasteczka
        OnMessageReceived = context =>
        {
            // Jeœli token jest ju¿ w nag³ówku Authorization,
            // pozostawiamy standardowe dzia³anie JWT.
            if (context.Request.Headers.ContainsKey("Authorization"))
            {
                return Task.CompletedTask;
            }

            // W przeciwnym razie szukamy tokenu w ciasteczku.
            if (context.Request.Cookies.TryGetValue(
                "TravelDiaryAuth",
                out var token))
            {
                context.Token = token;
            }

            return Task.CompletedTask;
        },

        OnAuthenticationFailed = context =>
        {
            Console.WriteLine(
                "JWT ERROR: " + context.Exception.Message);

            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

// Testowe konto administratora — tylko œrodowisko Development.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    var userManager = scope.ServiceProvider
        .GetRequiredService<UserManager<ApplicationUser>>();

    const string adminRole = "Admin";
    const string adminUserName = "traveldiary_admin";
    const string adminEmail = "admin@traveldiary.test";
    var adminPassword = builder.Configuration["Admin:Password"]
    ?? throw new InvalidOperationException(
        "Brakuje Admin:Password w User Secrets.");

    void CheckResult(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ",
                    result.Errors.Select(e => e.Description)));
        }
    }

    // Tworzymy rolê, jeœli jeszcze nie istnieje.
    if (!await roleManager.RoleExistsAsync(adminRole))
    {
        CheckResult(
            await roleManager.CreateAsync(
                new IdentityRole(adminRole)));
    }

    var adminUser = await userManager.FindByNameAsync(adminUserName);

    // Tworzymy konto tylko przy pierwszym uruchomieniu.
    if (adminUser == null)
    {
        var existingEmailUser =
            await userManager.FindByEmailAsync(adminEmail);

        if (existingEmailUser != null)
        {
            throw new InvalidOperationException(
                "Adres testowego administratora jest ju¿ zajêty. " +
                "Zmieñ adminEmail w Program.cs.");
        }

        adminUser = new ApplicationUser
        {
            UserName = adminUserName,
            Email = adminEmail,
            DisplayName = "Administrator",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        CheckResult(
            await userManager.CreateAsync(
                adminUser, adminPassword));
    }

    // Nadajemy administratora wskazanemu kontu.
    if (!await userManager.IsInRoleAsync(adminUser, adminRole))
    {
        CheckResult(
            await userManager.AddToRoleAsync(
                adminUser, adminRole));
    }
}

// Swagger w œrodowisku deweloperskim
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Pliki HTML, CSS, JS oraz zdjêcia
app.UseDefaultFiles();
app.UseStaticFiles();

// Sprawdzanie to¿samoœci u¿ytkownika
app.UseAuthentication();

// Sprawdzanie uprawnieñ
app.UseAuthorization();

// Endpointy API
app.MapControllers();

app.Run();
