using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TMS.Application.Abstractions;
using TMS.Domain.Abstractions;
using TMS.Infrastructure.Persistence;
using TMS.Infrastructure.Persistence.Encryption;
using TMS.Infrastructure.Repositories;
using TMS.Infrastructure.Time;

namespace TMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
            var connectionString = configuration.GetConnectionString("TmsDatabase") ?? "Data Source=Data/tms.db";

            var encryptionKey = configuration["Database:EncryptionKey"] ?? "ChangeMe-StrongKey";
            DatabaseEncryption.Configure(encryptionKey);

            var sqliteBuilder = new SqliteConnectionStringBuilder(connectionString);
            var dataSource = sqliteBuilder.DataSource;

            if (!Path.IsPathRooted(dataSource))
            {
                dataSource = Path.Combine(AppContext.BaseDirectory, dataSource);
            }

            var directory = Path.GetDirectoryName(dataSource);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            sqliteBuilder.DataSource = dataSource;
            connectionString = sqliteBuilder.ToString();

            services.AddDbContext<TmsDbContext>(options =>
            {
                // Default all queries to no-tracking for read performance.
                // Opt-in to tracking only when you need to update entities.
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

                options.UseSqlite(connectionString);
            });

        services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
        services.AddScoped<ISupportMemberRepository, SupportMemberRepository>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        return services;
    }
}
