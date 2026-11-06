using Aspire.Hosting;
using System;

var builder = DistributedApplication.CreateBuilder(args);

// 1. Adiciona o Kafka como um container customizado
var kafka = builder.AddContainer("kafka", "confluentinc/cp-kafka", "8.2.0")
    .WithEndpoint(port: 9092, targetPort: 9092, name: "kafka-external")
    .WithEndpoint(port: 29093, targetPort: 29093, name: "kafka-controller")
    .WithEnvironment("KAFKA_NODE_ID", "1")
    .WithEnvironment("KAFKA_PROCESS_ROLES", "broker,controller")
    .WithEnvironment("KAFKA_LISTENERS", "PLAINTEXT://0.0.0.0:29092,CONTROLLER://0.0.0.0:29093,PLAINTEXT_HOST://0.0.0.0:9092")
    .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://kafka:29092,PLAINTEXT_HOST://localhost:9092")
    .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT")
    .WithEnvironment("KAFKA_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", "1@kafka:29093")
    .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "PLAINTEXT")
    .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_MIN_ISR", "1")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "1")
    .WithEnvironment("KAFKA_LOG_DIRS", "/tmp/kraft-combined-logs")
    .WithEnvironment("CLUSTER_ID", "MkU3OEVBNTcwNTJENDM2Qk");

// 2. Adiciona o Confluent Schema Registry como um container customizado
var schemaRegistry = builder.AddContainer("schema-registry", "confluentinc/cp-schema-registry", "8.2.0")
    .WithEnvironment("SCHEMA_REGISTRY_HOST_NAME", "schema-registry")
    .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS", "PLAINTEXT://kafka:29092")
    .WithEnvironment("SCHEMA_REGISTRY_LISTENERS", "http://0.0.0.0:8081")
    .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_TOPIC_REPLICATION_FACTOR", "1")
    .WithEndpoint(port: 8081, targetPort: 8081, name: "http")
    .WaitFor(kafka); // Garante que o Kafka sobe antes do Schema Registry

// 3. Adiciona o Kafka UI (Provectus) para gerenciar tÃ³picos, mensagens e esquemas visualmente
builder.AddContainer("kafka-ui", "provectuslabs/kafka-ui", "latest")
    .WithEnvironment("KAFKA_CLUSTERS_0_NAME", "local-kraft-cluster")
    .WithEnvironment("KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS", "kafka:29092")
    .WithEnvironment("KAFKA_CLUSTERS_0_SCHEMAREGISTRY", "http://schema-registry:8081")
    .WithEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WaitFor(kafka)
    .WaitFor(schemaRegistry);

var producerApi = builder.AddProject<Projects.KafkaProducerApi>("producer-api")
    .WithEnvironment("Kafka__ProducerSettings__BootstrapServers", "localhost:9092")
        .WithEnvironment(context =>
        {
            context.EnvironmentVariables["Kestrel__HttpPort"] = "7500";
            context.EnvironmentVariables["ConnectionStrings__SchemaRegistry"] = "http://localhost:8081";
        })
        .WithHttpEndpoint(port: 6500, targetPort: 7500, name: "http")
        //.WithReference(kafka)
        //.WithReference(name: schemaRegistry.Resource.Name)//, uri: new Uri(schemaRegistry.GetEndpoint("schema-registry").Url)
        .WaitFor(kafka)
        .WaitFor(schemaRegistry);

var consumerWorker = builder.AddProject<Projects.KafkaConsumerWorker>("consumer-worker")
        .WithEnvironment("Kafka__ConsumerSettings__BootstrapServers", "localhost:9092")
        .WithEnvironment(context =>
        {
            context.EnvironmentVariables["ConnectionStrings__SchemaRegistry"] = "http://localhost:8081";
        })
        .WaitFor(kafka)
        .WaitFor(producerApi)
        .WaitFor(schemaRegistry);

builder.AddProject<Projects.KafkaSimulator>("simulator")
    .WithEnvironment("Api__BaseUrl", "http://localhost:7500")
    .WaitFor(producerApi)
    .WaitFor(consumerWorker);

builder.Build().Run();


