using StackExchange.Redis;

namespace OTRPO_Lab9;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. Конфигурация сервисов

        builder.Services.AddRazorPages();
        
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowAnyOrigin();
            });
        });

        builder.Services.AddSingleton<IConnectionMultiplexer>(
            ConnectionMultiplexer.Connect("redis:6379,abortConnect=false"));

        builder.Services.AddTransient<ISubscriber>(sp =>
        {
            var multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
            return multiplexer.GetSubscriber();
        });
        
        // Регистрация ChatService
        builder.Services.AddSingleton<ChatService>();

        // Регистрация ChatHandler
        builder.Services.AddSingleton<ChatHandler>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        // 2. Конфигурация Middleware
        app.UseCors("AllowAll");
        app.UseWebSockets();

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.Map("/ws", async (HttpContext context) =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var chatHandler = context.RequestServices.GetRequiredService<ChatHandler>();
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await chatHandler.HandleAsync(context, webSocket);
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        });

        app.Map("/ws/private/{roomId}", async context =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var chatHandler = context.RequestServices.GetRequiredService<ChatHandler>();
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var roomId = context.Request.RouteValues["roomId"]?.ToString();
                var username = context.Request.Query["username"].ToString();
                await chatHandler.HandlePrivateRoomAsync(context, webSocket, roomId, username);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        });

        app.MapControllers();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }
}
