using Serilog;
using SOPSearch.Web.Services;

string EnvironmentVariable = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "";

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{EnvironmentVariable}.json", optional: true)
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Configuration.AddConfiguration(configuration);

    // Add services to the container.
    builder.Services.AddControllersWithViews();

    builder.Services.AddHttpClient<IApiClient, ApiClient>(client =>
    {
        var baseUrl = builder.Configuration["Api:BaseUrl"];
        client.BaseAddress = new Uri(baseUrl!);
        client.Timeout = TimeSpan.FromSeconds(100);
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseAuthorization();

    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Chat}/{id?}")
        .WithStaticAssets();


    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}