using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Ruslan.AspireExercises.Web.Data;
using System.Diagnostics;

namespace MigrationService;

public class DatabaseMigration : BackgroundService
{
    public const string ActivitySourceName = "Migrations";
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private static readonly ActivitySource MigrationActivitySource = new(ActivitySourceName);

    public DatabaseMigration(IServiceProvider serviceProvider, IHostApplicationLifetime hostApplicationLifetime)
    {
        _serviceProvider = serviceProvider;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var activity = MigrationActivitySource.StartActivity(ActivityKind.Client);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await EnsureDatabaseAsync(dbContext, cancellationToken);
            await RunMigrationAsync(dbContext, cancellationToken);
            await SeedDataAsync(dbContext, scope, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        _hostApplicationLifetime.StopApplication();//позволяет настроить стартануть основное приложение после того как эти миграции закончатся
    }    

    /// <summary>
    /// Проверяет есть ли база, и если нет, то создается
    /// </summary>
    private async Task EnsureDatabaseAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var dbCreator = dbContext.GetService<IRelationalDatabaseCreator>();

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            if (!await dbCreator.ExistsAsync(cancellationToken))
            {
                await dbCreator.CreateAsync(cancellationToken);
            }
        });
    }

    /// <summary>
    /// Запуск миграции
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task RunMigrationAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
        });
    }

    /// <summary>
    /// Добавление пользователя
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task SeedDataAsync(ApplicationDbContext dbContext, IServiceScope scope, CancellationToken cancellationToken)
    {
        if (dbContext.Users.Any())
        {
            return;
        }

        var user = new IdentityUser
        {
            Email = "ruslan.faz@mail.ru",
            NormalizedEmail = "RUSLAN.FAZ@MAIL.RU",
            UserName = "ruslan.faz@mail.ru",
            NormalizedUserName = "RUSLAN.FAZ@MAIL.RU",
            PhoneNumber = "+00000000000",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("D")
        };        

        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var password = new PasswordHasher<IdentityUser>();
            var hashed = password.HashPassword(user, "My_String!@#WQE_PASS");
            user.PasswordHash = hashed;
            await dbContext.Users.AddAsync(user, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }
}
