using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Text.Json.Serialization;
using Votaciones.Api.Authentication;
using Votaciones.Api.AuthorizationPermisos;
using Votaciones.Api.Middleware;
using Votaciones.Application.DTOs.Mapping;
using Votaciones.Application.Interfaces.ISecurity;
using Votaciones.Application.ServiceRegistration;
using Votaciones.Infrastructure.Data;
using Votaciones.Infrastructure.ServiceRegistration;

var builder = WebApplication.CreateBuilder(args);

// Habilitar CORS (Para que Angular pueda acceder a la API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});
// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Votaciones API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] =
                 new List<string>()
        });
});

// Agregar conexión a la base de datos
builder.Services.AddDbContext<AppDbContext>(options =>
   options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// builder.Services.AddDbContext<AppDbContext>(options =>
// {
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

//     options.EnableDetailedErrors();
//     options.EnableSensitiveDataLogging();

//     options.LogTo(Console.WriteLine, LogLevel.Information);
// });

// AutoMapper
builder.Services.AddAutoMapper(cfg => { cfg.AddProfile<AutoMapperProfiles>(); });
// Registro por Capas (Ordenado y Purista)
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();

//Token
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("La clave JWT no está configurada.");
}
// Configurar autenticación JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            // Cuando NO hay token o no se pudo autenticar
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                var response = new
                {
                    message = context.AuthenticateFailure != null 
                    ? "Su sesión ha expirado, por favor inicie sesión nuevamente."
                    : "Se requiere autenticación, por favor inicie sesión."
                };
                await context.Response.WriteAsJsonAsync(response);
            },
            // Cuando el token existe pero es inválido
            OnAuthenticationFailed = context =>
            {
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Provider de políticas dinámicas
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
// Handler de permisos
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
// 
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationMiddlewareResult>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentService, CurrentUserService>();

var app = builder.Build();

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Mi API v1");
    });
}

app.UseMiddleware<ExceptionsMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
