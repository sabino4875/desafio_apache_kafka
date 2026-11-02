# 04. Orquestração de Schema Registry no .NET Aspire

## Por que orquestrar com .NET Aspire?
O **.NET Aspire** simplified consideravelmente a inclusão do Confluent Schema Registry no ambiente de desenvolvimento local:

1. **Injeção Transparente de Variáveis de Ambiente:** O Aspire cuida da resolução de endereços e portas entre os containers do Kafka e do Schema Registry.
2. **Resolução Dinâmica de Connections:** A API Produtora e o Worker Consumidor recebem a URL do Schema Registry automaticamente via `builder.Configuration.GetConnectionString("schema-registry")`.
3. **Gerenciamento Unificado de Saúde (Health Checks):** Garante que a aplicação .NET só tente registrar esquemas quando a API do Schema Registry estiver respondendo adequadamente.
