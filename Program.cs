using FriendlyMeme.Handlers.Logger;
using FriendlyMeme.League;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = Host
    .CreateDefaultBuilder(Environment.GetCommandLineArgs())
    .ConfigureLogging(builder =>
        builder.ClearProviders()
            .AddColorConsoleLogger(_ => { }))
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Client>>();

Client client = new Client(logger);
await client.RunAsync();