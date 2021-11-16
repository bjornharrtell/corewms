using CoreWms;
using CoreWms.Config;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Npgsql;

NpgsqlConnection.GlobalTypeMapper.UseNetTopologySuite();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.Configure<IISServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
});
var config = new Config();
builder.Configuration.Bind("CoreWms", config);
builder.Services.AddSingleton<IConfig>(config);
builder.Services.AddSingleton<IContext, Context>();
builder.Services.AddScoped<GetCapabilities>();
builder.Services.AddScoped<GetMap>();
builder.Services
    .AddMvcCore(options => options.OutputFormatters.Add(new XmlSerializerOutputFormatter()));

var app = builder.Build();
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseExceptionHandler("/error");
app.UseRouting();
//app.UseAuthorization();
app.MapControllers();
app.Run();