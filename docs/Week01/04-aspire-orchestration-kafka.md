# 04. Orquestração com .NET Aspire

## Por que usar .NET Aspire com Kafka?
1. **Zero Configuração de Portas/IPs Manual:** O AppHost gerencia os endpoints e distribui as configurações por variáveis de ambiente.
2. **OpenTelemetry Nativo:** Rastreabilidade distribuída (Traces), Logs e Métricas do Producer até o Consumer em um único Dashboard.
3. **Gerenciamento Transparente de Containers:** O container Kafka é instanciado via integração `Aspire.Hosting.Kafka`.
