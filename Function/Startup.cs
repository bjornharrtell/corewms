using System.IO;
using CoreWms;
using CoreWms.Config;
using CoreWms.DataSource;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

[assembly: FunctionsStartup(typeof(Startup))]

namespace CoreWms;

public class Startup : FunctionsStartup
{
    public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
    {
        FunctionsHostBuilderContext context = builder.GetContext();

        builder.ConfigurationBuilder
            .AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), optional: true, reloadOnChange: false)
            .AddJsonFile(Path.Combine(context.ApplicationRootPath, $"appsettings.{context.EnvironmentName}.json"), optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();
    }

    public override void Configure(IFunctionsHostBuilder builder)
    {
        NpgsqlConnection.GlobalTypeMapper.UseNetTopologySuite();

        FunctionsHostBuilderContext context = builder.GetContext();
        var config = new Config.Config();
        context.Configuration.Bind("CoreWms", config);
        config.DataPath = context.ApplicationRootPath;
        builder.Services.AddSingleton<IConfig>(config);
        builder.Services.AddSingleton<IContext, Context>();
        builder.Services.AddTransient<FlatGeobufSource>();
        builder.Services.AddTransient<PostgreSQLSource>();
        builder.Services.AddScoped<GetCapabilities>();
        builder.Services.AddScoped<GetMap>();
    }
}