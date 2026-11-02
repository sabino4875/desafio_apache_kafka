using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafka("kafka")
                   .WithKafkaUI();

// Injeto o Kafka no projeto Web API (Producer)
var producerApi  = builder.AddProject<Projects.KafkaProducerApi>("producer-api")
    .WithEnvironment("Kafka__ProducerSettings__BootstrapServers", kafka.Resource.ConnectionStringExpression)
        .WithEnvironment(context =>
        {
            context.EnvironmentVariables["Kestrel__HttpPort"] = "7500";
        })
        .WithHttpEndpoint(port: 6500, targetPort: 7500, name: "http")
        .WithReference(kafka)
        .WaitFor(kafka);

var consumerWorker = builder.AddProject<Projects.KafkaConsumerWorker>("consumer-worker")
        .WithEnvironment("Kafka__ConsumerSettings__BootstrapServers", kafka.Resource.ConnectionStringExpression)
       .WithReference(kafka)
       .WaitFor(kafka)
       .WaitFor(producerApi);

builder.AddProject<Projects.KafkaSimulator>("simulator")
    .WithEnvironment("Api__BaseUrl", "http://localhost:7500")
    .WaitFor(producerApi)
    .WaitFor(consumerWorker);

builder.Build().Run();
