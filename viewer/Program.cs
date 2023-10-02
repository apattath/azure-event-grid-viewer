using Microsoft.AspNetCore.Builder;
using viewer.Hubs;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using viewer.Auth;
using viewer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<EnvironmentManagerService>();
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddHttpClient("MessagingAIClient")
    .ConfigureHttpMessageHandlerBuilder(builder => 
    {
        builder.PrimaryHandler = new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };
    });

builder.Services.AddSingleton<IHttpAuthenticator, HmacHttpAuthenticator>();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCookiePolicy();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<GridEventsHub>("/hubs/gridevents");
    endpoints.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
});

app.Run();

