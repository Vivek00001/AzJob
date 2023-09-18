using Microsoft.Extensions.Hosting;
using AzJob.Configuration;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal sealed class JobHostService: IHostedService
{
    private readonly ILogger _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly BlobConfig _blobConfig;
    private readonly QueueConfig _queueConfig;

    public JobHostService(  
        ILogger<JobHostService> logger,
        IHostApplicationLifetime appLifetime,
        IOptions<QueueConfig> queueConfig,
        IOptions<BlobConfig> blobConfig
    )
    {
        _logger=logger;
        _appLifetime = appLifetime;
        _queueConfig = queueConfig.Value;
        _blobConfig = blobConfig.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Start processing request");
        Console.WriteLine("Start processing request");
        
        _appLifetime.ApplicationStarted.Register(() =>
        {
            Task.Run(async()=>
            {
                try
                {
                    var processor = GetServiceBusProcessor();
                    processor.ProcessMessageAsync += ProcessorOnProcessMsgAsync;
                    processor.ProcessErrorAsync += ProcessorOnProcessErrorAsync;
                    _logger.LogInformation("Start queue processing for Azure Service bus queue {QueueName}",_queueConfig.QueueName);
                    await processor.StartProcessingAsync(cancellationToken);

                    await Task.Delay(TimeSpan.FromMinutes(1));
                    await processor.StopProcessingAsync(cancellationToken);
                    _logger.LogInformation("Stopping queue processing for queue {QueueName}", _queueConfig.QueueName);
                    await processor.CloseAsync(cancellationToken);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception!");
                }
                finally
                {
                    Console.WriteLine("Stopping application");
                    _appLifetime.StopApplication();
                }
            });
        });
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private Task ProcessorOnProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError("Error while processing message {EventSource}: {Error}", args.ErrorSource, args.Exception!.ToString());
        return Task.CompletedTask;
    }

    private async Task ProcessorOnProcessMsgAsync(ProcessMessageEventArgs args)
    {
        var fileName = $"{args.Message!.MessageId}.json";
        var blobClient = new BlobContainerClient(_blobConfig.ConnectionString,_blobConfig.ContainerName);
        _logger.LogInformation("Uploading message to blob:{fileName} in container: {containerName}",fileName,_blobConfig.ContainerName);
        await blobClient.UploadBlobAsync(fileName,args.Message.Body!.ToStream())!;
        await args.CompleteMessageAsync(args.Message);
    }

    private ServiceBusProcessor GetServiceBusProcessor()
    {
        var client = new ServiceBusClient(_queueConfig.ConnectionString);
        return client.CreateProcessor(_queueConfig.QueueName,
            new ServiceBusProcessorOptions
            {
                MaxConcurrentCalls =1,
                AutoCompleteMessages = false,
                MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5)
            }
        );
    }

}