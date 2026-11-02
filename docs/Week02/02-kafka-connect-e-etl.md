# 02. Kafka Connect e Integrações ETL/CDC

## O que é o Kafka Connect?
O **Kafka Connect** é uma ferramenta escalável para integrar o Kafka com sistemas externos de armazenamento (bancos de dados, sistemas de arquivos, índices de busca, etc.), sem a necessidade de escrever código customizado de integração.

---

## Tipos de Connectors

### 1. Source Connectors (Entrada)
Capturam dados de sistemas externos e publicam tópicos no Kafka.
- **Exemplo de uso:** **Debezium** para Change Data Capture (CDC). Captura inserções, atualizações e deleções no PostgreSQL ou MySQL em tempo real e envia eventos para o Kafka.

### 2. Sink Connectors (Saída)
Lêem eventos de tópicos do Kafka e persistem em sistemas de destino.
- **Exemplos de uso:**
  - Persistir tópicos de auditoria no **Amazon S3** ou **Google Cloud Storage**.
  - Indexar logs de transações no **Elasticsearch** / **OpenSearch**.

---

## Vantagens do Kafka Connect
- **Escalabilidade Horizontal:** Pode rodar em modo distribuído (`Distributed Mode`) com gerenciamento automático de offset e tarefas.
- **Reaproveitamento de Conectores:** Milhares de conectores prontos mantidos pela comunidade e fornecedores.
