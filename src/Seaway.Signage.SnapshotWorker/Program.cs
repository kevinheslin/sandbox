using Seaway.Signage.SnapshotWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<VendorCredentialStore>();
builder.Services.AddSingleton<ChromiumRenderer>();
builder.Services.AddHostedService<SnapshotScheduler>();

var host = builder.Build();
host.Run();
