using System.Data.Common;
using Blog.Services.Identity.API.Infrastructure.TypeHandlers;
using Blog.Services.Identity.API.Application.Queries.AvatarQueries;
using MediatR;
using Npgsql;

namespace Blog.Services.Identity.API.Application;

public static class ApplicationInstaller
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        string connectionString = config.GetConnectionString("Postgres");
        NpgsqlConnection.GlobalTypeMapper.UseNodaTime();
        Dapper.SqlMapper.AddTypeHandler(new InstantTypeHandler());

        services
            .AddTransient<DbConnection, NpgsqlConnection>(sp => new NpgsqlConnection(connectionString))
            .AddScoped<IAvatarQueries, DapperAvatarQueries>()
            .AddScoped<IUserQueries, DapperUserQueries>()
            .AddMediatR(typeof(Program).Assembly);

        return services;
    }
}
