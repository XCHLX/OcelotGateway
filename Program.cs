using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;

var builder = WebApplication.CreateBuilder(args);

// 1. 添加 ocelot.json 配置文件
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// 2. 注入 Ocelot 服务
builder.Services.AddOcelot().AddPolly();

var app = builder.Build();
app.Use(async (context, next) =>
{
    // 如果没有 Authorization 头
    if (!context.Request.Headers.ContainsKey("Authorization"))
    {
        // 获取 IP 并塞入一个伪造的 Header
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        context.Request.Headers["X-Guest-Id"] = remoteIp;
    }
    else
    {
        // 有 Token 的话，就把 Token 复制到这个伪造 Header
        context.Request.Headers["X-Guest-Id"] = context.Request.Headers["Authorization"];
    }
    Console.WriteLine(context.Request.Headers["X-Guest-Id"]);
    await next();
});
// 3. 使用 Ocelot 中间件
await app.UseOcelot();

app.Run();