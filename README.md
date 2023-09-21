
# AzJob - Sample job for Azure container Apps #

This is a dotnet console application which is packed in docker container and is designed to be executed inside **ACA(Azure Containar Apps)** as a **Job**.  

The console application read messagge from Service Bus Queue and save message body as blob in Azure Storage account.

## Prerequisites ##
* Dotnet 6
* Docker Desktop or other compatible software
* Azure subscription

## How to use ##

* Build and upload docker image by executing the below commands:

```
docker build . -t <docker_registry>/<imagename>:latest --platform linux/amd64

docker push <docker_registry>/<imagename>:latest
```
* Create resource group in Azure subscription
* Create Storage Account with blob container in the resource group
* Create Service Bus with Queue in the resource group

* Create ACA environment by executing the following command  in cloud shell

```
Az containerapp env create \
    --name "env-bgsvrbusjob" \
    --resource-group "<<resouce_group_name>>" \
    --location westeurope

```
* Create Job in ACA environment
```
az containerapp job create \
    --name "bgsbusjob" --resource-group "<<resouce_group_name>>" --environment "env-bgjob" \
    --trigger-type "Event" \
    --replica-timeout "600" --replica-retry-limit "1" \
    --replica-completion-count "1" -"parallelism "1" \
    --min-executions "0" --max-executions "10" \
    --image "<<registry>>/<<consoleappimage>>:latest" \
    --registry-server "<<registry-server>>" --registry-username "<<registry-username>>" \
    --registry-password "<<registry-password>>" \
    --secrets "storage-account-constr=<<storage-account-connectionString>>" "service-bus-constr=<<ServiceBusConnectionString>>"  \  
    --scale-rule-name "serviceJob" --scale-rule-type "azure-servicebus" \
    --scale-rule-metadata "messageCount=1" "queueName=<<Servicebus-QueueName>>" \
    --scale-rule-auth "connection=service-bus-connection-string" \
    --env-vars "QueueConfig__QueueName=<<Servicebus-QueueName>>" "BlobConfig__ContainerName=<<Storage-blobName>>" "QueueConfig__ConnectionString=secretref:service-bus-constr" "BlobConfig__ConnectionString=secretref:storage-account-constr"
```

***Please replace values in markup <<>>***


