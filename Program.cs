using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AzJob.Configuration;

internal sealed class Program
{
    private static async Task Main(string[] args)
    {
        await Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.Configure<BlobConfig>(options =>
                    hostContext.Configuration.GetRequiredSection(BlobConfig.SectionName).Bind(options));
                services.Configure<QueueConfig>(options =>
                    hostContext.Configuration.GetRequiredSection(QueueConfig.SectionName).Bind(options));
                services.AddHostedService<JobHostService>();
            }
        ).RunConsoleAsync();
    }
}