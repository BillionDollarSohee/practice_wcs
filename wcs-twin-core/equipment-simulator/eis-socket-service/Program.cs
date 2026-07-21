using System.Collections.Generic;
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

// DB 연결 등록
string connectionString = builder.Configuration.GetConnectionString("WcsTwin");
builder.Services.AddDbContext<WcsTwinContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 라인(TR/FL/DR) - equipment-simulator가 라인 전용 워커로 대차를 순서대로 보내주므로
// WCS 쪽에는 동시성 제어 게이트가 없다. Modbus/RFID 연결을 라인별로 나누는 데만 이 목록을 쓴다.
string[] lineTypes = { "TR", "FL", "DR" };

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

// Modbus/RFID 계층 등록 - 라인(TR/FL/DR)마다 물리적으로 다른 RFID 리더를 쓰므로 연결도 라인별로 분리한다.
var modbusEndpoints = new Dictionary<string, (string Host, int Port)>();
foreach (var lineType in lineTypes)
{
    string modbusHost = builder.Configuration[$"ModbusReader:Lines:{lineType}:Host"] ?? "127.0.0.1";
    int modbusPort = int.Parse(builder.Configuration[$"ModbusReader:Lines:{lineType}:Port"] ?? "5020");
    modbusEndpoints[lineType] = (modbusHost, modbusPort);
}

builder.Services.AddSingleton<ModbusServiceRegistry>(sp =>
    new ModbusServiceRegistry(modbusEndpoints, sp.GetRequiredService<ILogger<ModbusPlcInterfaceService>>()));
builder.Services.AddSingleton<ModbusServiceManager>();

builder.Services.AddScoped<RFIDControllService>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();