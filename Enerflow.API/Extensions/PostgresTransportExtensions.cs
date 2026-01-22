using MassTransit;
using Npgsql;

namespace Enerflow.API.Extensions;

/// <summary>
/// Extension methods to configure PostgreSQL as the MassTransit message transport.
/// </summary>
public static class PostgresTransportExtensions
{
    /// <summary>
    /// Configures the PostgreSQL transport options for MassTransit.
    /// Creates a dedicated schema and role for the message transport.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">The PostgreSQL connection string</param>
    /// <param name="create">Whether to create the transport schema on startup</param>
    /// <param name="delete">Whether to delete the transport schema on shutdown</param>
    public static IServiceCollection ConfigurePostgresTransport(
        this IServiceCollection services,
        string? connectionString,
        bool create = true,
        bool delete = false)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        services.AddOptions<SqlTransportOptions>().Configure(options =>
        {
            options.Host = builder.Host ?? "localhost";
            options.Port = builder.Port;
            options.Database = builder.Database ?? "enerflow_db";
            options.Schema = "transport";
            options.Role = "transport";
            options.Username = builder.Username ?? "enerflow";
            options.Password = builder.Password; // No default password for security
            options.AdminUsername = builder.Username;
            options.AdminPassword = builder.Password;
        });

        services.AddPostgresMigrationHostedService(create, delete);

        return services;
    }
}
