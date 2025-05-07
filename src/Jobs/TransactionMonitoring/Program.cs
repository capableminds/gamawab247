using TransactionMonitoring;
using TransactionMonitoring.ServiceExtension;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.RegisterServices();
builder.Services.AddHttpClientInfrastructure();
builder.Services.AddHostedService<Worker>();
var host = builder.Build();
host.Run();
