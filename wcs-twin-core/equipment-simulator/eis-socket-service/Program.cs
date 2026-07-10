using Database;
using EisSocketService.Handlers;
using EisSocketService.Host;
using EisSocketService.Socket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModbusService;
using RfidControllService;

var builder = Host.CreateApplicationBuilder(args);

// DB 연결 등록 (appsettings.json의 ConnectionStrings:WcsTwin 사용) - database 프로젝트 사용
string connectionString = builder.Configuration.GetConnectionString("WcsTwin");
builder.Services.AddDbContext<WcsTwinContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Vision Command 핸들러 등록 - 스코프 단위로 생성됨
builder.Services.AddScoped<IMessageHandler, VisionCartInfoRequestHandler>();
builder.Services.AddScoped<IMessageHandler, VisionProductCompleteHandler>();
builder.Services.AddScoped<IMessageHandler, HeartbeatHandler>();
builder.Services.AddScoped<IMessageHandler, BcrReadHandler>();
builder.Services.AddScoped<IMessageHandler, DestinationSendHandler>();
builder.Services.AddScoped<IMessageHandler, DestinationAckHandler>();
builder.Services.AddScoped<IMessageHandler, DischargedHandler>();

builder.Services.AddScoped<MessageHandlerFactory>(sp =>
    new MessageHandlerFactory(sp.GetServices<IMessageHandler>()));

builder.Services.AddSingleton<SocketServiceManager>();

// Modbus/RFID 계층 등록
string modbusHost = builder.Configuration["ModbusReader:Host"] ?? "127.0.0.1";
int modbusPort = int.Parse(builder.Configuration["ModbusReader:Port"] ?? "5020");

builder.Services.AddSingleton<ModbusPlcInterfaceService>(sp =>
    new ModbusPlcInterfaceService(sp.GetRequiredService<ILogger<ModbusPlcInterfaceService>>(), modbusHost, modbusPort));
builder.Services.AddSingleton<ModbusServiceManager>();

builder.Services.AddScoped<RFIDControllService>();
builder.Services.AddSingleton<EcsRFIDControllManager>();

// Worker 하나만 HostedService로 등록 - 나머지 매니저는 Worker가 시작/종료를 위임받아 제어
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();