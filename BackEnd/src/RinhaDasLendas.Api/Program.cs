using System.Text.Json.Serialization;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Application;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Infrastructure;
using RinhaDasLendas.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var jwtSection = builder.Configuration.GetSection("Authentication:Jwt");
var jwtKey = jwtSection.GetValue<string>("Key") ?? throw new InvalidOperationException("Authentication:Jwt:Key nao configurado.");
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddAuthentication(TestingAuthHandler.SchemeName)
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestingAuthHandler>(TestingAuthHandler.SchemeName, _ => { });
}
else
{
    builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidIssuer = jwtSection.GetValue<string>("Issuer"),
                ValidAudience = jwtSection.GetValue<string>("Audience"),
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.FromSeconds(30),
            };
        });
}
builder.Services.AddAuthorization(options =>
{
    if (builder.Environment.IsEnvironment("Testing"))
    {
        options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build();
        options.AddPolicy(AuthPermissions.CanViewUsers, policy => policy.RequireAssertion(_ => true));
        options.AddPolicy(AuthPermissions.CanManageUsers, policy => policy.RequireAssertion(_ => true));
        options.AddPolicy(AuthPermissions.CanManageRoles, policy => policy.RequireAssertion(_ => true));
        options.AddPolicy(AuthPermissions.CanResetUserPassword, policy => policy.RequireAssertion(_ => true));
        options.AddPolicy(AuthPermissions.CanActivateDeactivateUsers, policy => policy.RequireAssertion(_ => true));
        options.AddPolicy(AuthPermissions.CanManageDrafts, policy => policy.RequireAssertion(_ => true));
        options.AddPolicy(AuthPermissions.CanManageMatches, policy => policy.RequireAssertion(_ => true));
        options.AddPolicy(AuthPermissions.CanViewAdminLogs, policy => policy.RequireAssertion(_ => true));
    }
    else
    {
        options.AddPolicy(AuthPermissions.CanViewUsers, policy => policy.RequireRole(AuthRoles.SuperAdmin, AuthRoles.Admin, AuthRoles.Moderador));
        options.AddPolicy(AuthPermissions.CanManageUsers, policy => policy.RequireRole(AuthRoles.SuperAdmin, AuthRoles.Admin));
        options.AddPolicy(AuthPermissions.CanManageRoles, policy => policy.RequireRole(AuthRoles.SuperAdmin, AuthRoles.Admin));
        options.AddPolicy(AuthPermissions.CanResetUserPassword, policy => policy.RequireRole(AuthRoles.SuperAdmin, AuthRoles.Admin));
        options.AddPolicy(AuthPermissions.CanActivateDeactivateUsers, policy => policy.RequireRole(AuthRoles.SuperAdmin, AuthRoles.Admin));
        options.AddPolicy(AuthPermissions.CanManageDrafts, policy => policy.RequireRole(AuthRoles.SuperAdmin, AuthRoles.Admin, AuthRoles.Moderador));
        options.AddPolicy(AuthPermissions.CanManageMatches, policy => policy.RequireRole(AuthRoles.SuperAdmin, AuthRoles.Admin, AuthRoles.Moderador));
        options.AddPolicy(AuthPermissions.CanViewAdminLogs, policy => policy.RequireRole(AuthRoles.SuperAdmin));
    }
});
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "RinhaDasLendas API",
        Version = "v1",
        Description = "API para cadastro de jogadores e preferencias de rotas."
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<RinhaDasLendasDbContext>();
    dbContext.Database.Migrate();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    await RinhaDasLendas.Infrastructure.DependencyInjection.SeedIdentityAsync(app.Services);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ApiExceptionMiddleware>();
app.MapHealthChecks("/health");
app.UseCors("Frontend");
app.UseAuthentication();
if (app.Environment.IsEnvironment("Testing"))
{
    app.Use(async (context, next) =>
    {
        context.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "99999999-0000-0000-0000-000000000001"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "Teste"),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, AuthRoles.SuperAdmin),
        ], TestingAuthHandler.SchemeName));
        await next();
    });
}
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
