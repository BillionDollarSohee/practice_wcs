using EisSocketService.Data;
using EisSocketService.Handlers;
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

// 소켓 서버를 백그라운드 서비스로 등록 - 호스트 시작 시 자동 실행
builder.Services.AddHostedService<SocketServer>();

var host = builder.Build();
host.Run();