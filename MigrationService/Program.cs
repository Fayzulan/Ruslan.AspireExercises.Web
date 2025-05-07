using Microsoft.EntityFrameworkCore.Diagnostics;
using MigrationService;
using Ruslan.AspireExercises.Web.Data;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHostedService<DatabaseMigration>();

builder.Services.AddOpenTelemetry().WithTracing(tracing => tracing.AddSource(DatabaseMigration.ActivitySourceName));

builder.AddNpgsqlDbContext<ApplicationDbContext>("database", configureDbContextOptions: x =>
{
    x.EnableDetailedErrors();
    x.ConfigureWarnings(c => c.Ignore(RelationalEventId.PendingModelChangesWarning));//блокирует ошибку о выполнении sql команды на __EFMigrationsHistory 
});

var host = builder.Build();
host.Run();
