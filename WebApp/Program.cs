using CoreWms;
using CoreWms.Config;
using CoreWms.DataSource;
using Microsoft.AspNetCore.Mvc.Formatters;
using Npgsql;

NpgsqlConnection.GlobalTypeMapper.UseNetTopologySuite();

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddSimpleConsole();
builder.Services.AddControllers();

var config = new Config();
builder.Configuration.Bind("CoreWms", config);
builder.Services.AddSingleton<IConfig>(config);
builder.Services.AddSingleton<IContext, Context>();
builder.Services.AddTransient<FlatGeobufSource>();
builder.Services.AddTransient<PostgreSQLSource>();
builder.Services.AddScoped<GetCapabilities>();
builder.Services.AddScoped<GetMap>();
builder.Services
    .AddMvcCore(o => o.OutputFormatters.Add(new XmlSerializerOutputFormatter()));

var app = builder.Build();
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseExceptionHandler("/error");
app.UseRouting();
app.MapControllers();
app.Run();