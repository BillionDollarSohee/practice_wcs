using EisSocketService.Data;
using EisSocketService.Handlers;
using EisSocketService.Host;
using EisSocketService.Socket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// DB 연결 등록 (appsettings.json의 ConnectionStrings:WcsTwin 사용)
string connectionString = builder.Configuration.GetConnectionString("WcsTwin");
builder.Services.AddDbContext<WcsTwinContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Command 핸들러 등록 - 스코프 단위로 생성됨
builder.Services.AddScoped<IMessageHandler, VisionCartInfoRequestHandler>();
builder.Services.AddScoped<IMessageHandler, VisionProductCompleteHandler>();
builder.Services.AddScoped<IMessageHandler, HeartbeatHandler>();
builder.Services.AddScoped<IMessageHandler, BcrReadHandler>();
builder.Services.AddScoped<IMessageHandler, DestinationSendHandler>();
builder.Services.AddScoped<IMessageHandler, DestinationAckHandler>();
builder.Services.AddScoped<IMessageHandler, DischargedHandler>();

// IEnumerable<IMessageHandler> 전체를 모아 Factory를 구성
builder.Services.AddScoped<MessageHandlerFactory>(sp =>
    new MessageHandlerFactory(sp.GetServices<IMessageHandler>()));

// 서비스 매니저는 Singleton으로 등록 - Worker가 Run()/Stop()을 직접 호출한다
builder.Services.AddSingleton<SocketServiceManager>();
// 다음 단계: builder.Services.AddSingleton<ModbusServiceManager>();

// Worker 하나만 HostedService로 등록 - 나머지 매니저는 Worker가 시작/종료를 위임받아 제어
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();