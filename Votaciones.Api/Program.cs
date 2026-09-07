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

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    // Registrar esquema Bearer
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();

        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] =
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                In = ParameterLocation.Header,
                BearerFormat = "JWT",
                Description = "Ingrese el token JWT."
            };
        return Task.CompletedTask;
    });
    // Aplicar Bearer a endpoints con [Authorize]
    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        var tieneAuthorize = metadata.OfType<AuthorizeAttribute>().Any();
        
        if (tieneAuthorize)
        {
            operation.Security ??= [];

            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
            });
        }
        return Task.CompletedTask;
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
                    ? "El token de autenticación no es válido o ha expirado." 
                    : "Se requiere un token de autenticación válido."
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Mi API v1");
    });
}

app.UseMiddleware<ExceptionsMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
