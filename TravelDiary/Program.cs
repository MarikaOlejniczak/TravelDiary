
using Microsoft.EntityFrameworkCore;
using TravelDiary.Data;
using TravelDiary.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Kontrolery API
builder.Services.AddControllers();

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
                "TravelDiarySuperSecretKey123456789")),

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

// Swagger w œrodowisku deweloperskim
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Pliki HTML, CSS, JS oraz zdjêcia
app.UseStaticFiles();

app.UseHttpsRedirection();

// Sprawdzanie to¿samoœci u¿ytkownika
app.UseAuthentication();

// Sprawdzanie uprawnieñ
app.UseAuthorization();

// Endpointy API
app.MapControllers();

app.Run();
