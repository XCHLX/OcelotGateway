using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using Ocelot.ServiceDiscovery;
using OcelotGateway.BackgroundServices;
using OcelotGateway.Controllers;
using OcelotGateway.Dto;
using OcelotGateway.Providers;
using OcelotGateway.Utils.Channels;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

// 1. 添加 ocelot.json 配置文件
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddHostedService<ServiceHealthCheckBackgroundService>();

builder.Services.AddSingleton<AlertChannel>();
//builder.Services.AddSingleton<DingTalkNotifier>();
builder.Services.AddHostedService<AlertBackgroundService>();

builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();
// 2. 注入 Ocelot 服务
builder.Services.AddOcelot(builder.Configuration).AddPolly();
// 3. 添加自定义服务发现
builder.Services.AddSingleton<IServiceDiscoveryProviderFactory, MyCustomServiceDiscoveryProviderFactory>();
// 4. 关键：除了 Delegate，建议同时注册 Provider 自身（解决某些版本下的反射校验问题）
builder.Services.AddSingleton<ServiceDiscoveryFinderDelegate>((provider, config, route) =>
{
    var factory = provider.GetRequiredService<IServiceDiscoveryProviderFactory>();

    var response = factory.Get(config, route);

    if (response.IsError)
    {
        throw new Exception(
            $"ServiceDiscovery error: {string.Join(",", response.Errors.Select(e => e.Message))}");
    }

    return response.Data;
});
var app = builder.Build();
app.UseRouting();

app.MapControllers();   // 先 MVC
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
    //Console.WriteLine(context.Request.Headers["X-Guest-Id"]);
    await next();
});
// 只让 非 /internal/* 进入 Ocelot
app.MapWhen(ctx => !ctx.Request.Path.StartsWithSegments("/homes"), subApp =>
{
    subApp.UseOcelot().Wait();
});

//// 3. 使用 Ocelot 中间件
//await app.UseOcelot();

app.Run();