using System.Text.Json.Serialization;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RinhaDasLendas.Api.Filters;
using RinhaDasLendas.Api.Hubs;
using RinhaDasLendas.Api.Observability;
using RinhaDasLendas.Api.Services;
using RinhaDasLendas.Application;
using RinhaDasLendas.Application.Interfaces;
using RinhaDasLendas.Domain.Constants;
using RinhaDasLendas.Infrastructure;
using RinhaDasLendas.Infrastructure.Messages;
using RinhaDasLendas.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<ApiMetrics>();
builder.Services.AddScoped<IDraftMontagemRealtimeNotifier, DraftMontagemRealtimeNotifier>();
builder.Services.AddHostedService<DraftMontagemTurnTimerService>();
builder.Services.AddHostedService<DraftMontagemPresenceClosureService>();
builder.Services.AddSignalR();
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var jwtSection = builder.Configuration.GetSection("Authentication:Jwt");
var startupMessages = new ResourceMessageProvider();
var jwtKey = jwtSection.GetValue<string>("Key")
    ?? throw new InvalidOperationException(startupMessages.GetMessage(MessageCodes.JwtKeyNotConfigured));
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(startupMessages.GetMessage(MessageCodes.JwtKeyNotConfigured));
}

builder.Services.Configure<BotInternalAuthOptions>(BotInternalAuthOptions.SchemeName, options =>
{
    options.Token = builder.Configuration["RINHA_API_INTERNAL_TOKEN"] ?? builder.Configuration["DiscordBot:InternalToken"] ?? string.Empty;
    options.ValidTokens = new[]
        {
            builder.Configuration["RINHA_API_INTERNAL_TOKEN"],
            builder.Configuration["DiscordBot:InternalToken"]
        }
        .Where(token => !string.IsNullOrWhiteSpace(token))
        .Select(token => token!)
        .Distinct(StringComparer.Ordinal)
        .ToArray();
});
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
        .AddScheme<BotInternalAuthOptions, BotInternalAuthHandler>(BotInternalAuthOptions.SchemeName, _ => { })
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
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    context.HttpContext.RequestServices.GetRequiredService<ApiMetrics>().RecordAuthFailure(JwtBearerDefaults.AuthenticationScheme);
                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs/draft-montagens"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                },
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
        options.AddPolicy(AuthPermissions.CanUseDiscordBotApi, policy => policy.RequireAssertion(_ => true));
        options.AddPolicy(AuthPermissions.CanManageDraftsOrUseDiscordBotApi, policy => policy.RequireAssertion(_ => true));
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
        options.AddPolicy(AuthPermissions.CanUseDiscordBotApi, policy => policy
            .AddAuthenticationSchemes(BotInternalAuthOptions.SchemeName)
            .RequireClaim("scope", AuthPermissions.CanUseDiscordBotApi));
        options.AddPolicy(AuthPermissions.CanManageDraftsOrUseDiscordBotApi, policy => policy
            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, BotInternalAuthOptions.SchemeName)
            .RequireAssertion(context =>
                context.User.IsInRole(AuthRoles.SuperAdmin)
                || context.User.IsInRole(AuthRoles.Admin)
                || context.User.IsInRole(AuthRoles.Moderador)
                || context.User.HasClaim("scope", AuthPermissions.CanUseDiscordBotApi)));
    }
});
builder.Services.AddHealthChecks();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.RequestServices.GetRequiredService<ApiMetrics>().RecordRateLimitedRequest(context.HttpContext.Request.Path);
        return ValueTask.CompletedTask;
    };
    options.AddFixedWindowLimiter("api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 120;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});
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

ValidateProductionConfiguration(app.Environment, app.Configuration, jwtKey);

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
else if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.MapHealthChecks("/health");
app.UseCors("Frontend");
app.UseRateLimiter();
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
app.MapControllers().RequireRateLimiting("api");
app.MapHub<DraftMontagensHub>("/hubs/draft-montagens");

app.Run();

static void ValidateProductionConfiguration(IWebHostEnvironment environment, IConfiguration configuration, string jwtKey)
{
    if (environment.IsDevelopment() || environment.IsEnvironment("Testing") || environment.IsEnvironment("IntegrationTesting"))
    {
        return;
    }

    if (jwtKey.Contains("dev-only", StringComparison.OrdinalIgnoreCase)
        || jwtKey.Contains("change-me", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(new ResourceMessageProvider().GetMessage(MessageCodes.JwtKeyNotConfigured));
    }

    var connectionString = configuration.GetConnectionString("RinhaDasLendas") ?? configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
    if (connectionString.Contains("Password=postgres", StringComparison.OrdinalIgnoreCase)
        || connectionString.Contains("Username=postgres", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(new ResourceMessageProvider().GetMessage(MessageCodes.DatabaseConnectionStringNotConfigured));
    }
}

public partial class Program;
