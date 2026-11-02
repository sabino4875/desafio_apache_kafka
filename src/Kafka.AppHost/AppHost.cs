using Aspire.Hosting;
using System;

var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafka("kafka")
                   .WithKafkaUI();

var schemaRegistry = builder.AddContainer("schema-registry", "confluentinc/cp-schema-registry", "8.4.0")
    .WithEnvironment("SCHEMA_REGISTRY_HOST_NAME", "schema-registry")
    .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS", "PLAINTEXT://kafka:29092")
    .WithEnvironment("SCHEMA_REGISTRY_LISTENERS", "http://0.0.0.0:8081")
    .WithEndpoint(port: 8081, targetPort: 8081, name: "http-schema-registry")
    .WaitFor(kafka);

// Injeto o Kafka no projeto Web API (Producer)
var producerApi  = builder.AddProject<Projects.KafkaProducerApi>("producer-api")
    .WithEnvironment("Kafka__ProducerSettings__BootstrapServers", kafka.Resource.ConnectionStringExpression)
        .WithEnvironment(context =>
        {
            context.EnvironmentVariables["Kestrel__HttpPort"] = "7500";
        })
        .WithHttpEndpoint(port: 6500, targetPort: 7500, name: "http")
        .WithReference(kafka)
        .WithReference(name: schemaRegistry.Resource.Name, uri: new Uri(schemaRegistry.GetEndpoint("http-schema-registry").Url))
        .WaitFor(kafka)
        .WaitFor(schemaRegistry);

var consumerWorker = builder.AddProject<Projects.KafkaConsumerWorker>("consumer-worker")
        .WithEnvironment("Kafka__ConsumerSettings__BootstrapServers", kafka.Resource.ConnectionStringExpression)
        .WithReference(kafka)
        .WithReference(name: schemaRegistry.Resource.Name, uri: new Uri(schemaRegistry.GetEndpoint("http-schema-registry").Url))
        .WaitFor(kafka)
        .WaitFor(producerApi)
        .WaitFor(schemaRegistry);

builder.AddProject<Projects.KafkaSimulator>("simulator")
    .WithEnvironment("Api__BaseUrl", "http://localhost:7500")
    .WaitFor(producerApi)
    .WaitFor(consumerWorker);

builder.Build().Run();
