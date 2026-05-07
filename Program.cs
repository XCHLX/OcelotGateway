using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using Ocelot.ServiceDiscovery;
using OcelotGateway.BackgroundServices;
using OcelotGateway.Controllers;
using OcelotGateway.Dto;
using OcelotGateway.Providers;
using OcelotGateway.Utils;
using OcelotGateway.Utils.Channels;
using System;
using System.Net;
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
    // 自定义一个简单的日志中间件，放在 Ocelot 之后
    subApp.Use(async (context, next) =>
    {
        await next(); // 先执行 Ocelot 逻辑

        // Ocelot 触发限流后会将状态码设为 429
        if (context.Response.StatusCode == 429)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var clientId = context.Request.Headers["X-Guest-Id"].ToString();
            var path = context.Request.Path;

            // 如果 Header 里没有，尝试获取 Connection 里的 RemoteIpAddress
            // 注意：这里用了 ?. 和 ToString() 防空指针
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // 记录日志
            TextLogger.Log($"Ocelot 限流触发: 客户端IP={ipAddress}, 路径={path} , token={clientId}", "Ocelot 限流触发");
            // 进阶：如果你想联动你项目里的 AlertChannel 发送告警
            // var alertChannel = context.RequestServices.GetRequiredService<AlertChannel>();
            // await alertChannel.SendAsync($"警告：检测到恶意刷接口，Client: {clientId}");
        }
    });

    subApp.UseOcelot().Wait();
});

//// 3. 使用 Ocelot 中间件
//await app.UseOcelot();

app.Run();