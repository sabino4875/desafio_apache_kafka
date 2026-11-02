# 03. Kafka Streams e Processamento em Tempo Real

## O que é a Kafka Streams API?
Uma biblioteca client em Java/JVM (e abstraída em ecossistemas .NET via bibliotecas de processamento como Streamiz) para construir aplicações e microsserviços onde os dados de entrada e saída estão armazenados em tópicos do Kafka.

---

## Conceitos Fundamentais

### 1. Stream vs. Table (KStream vs. KTable)
- **KStream:** Representa um fluxo contínuo de eventos individuais (Log de Eventos - ex: cada compra efetuada).
- **KTable:** Representa o estado atual dos dados (Chave-Valor - ex: saldo atual do cliente).

### 2. Janelas de Tempo (Windowing)
Permitem realizar agregações e cálculos sobre fluxos contínuos dentro de intervalos temporais definidos:
- **Tumbling Windows:** Janelas fixas e não sobrepostas (ex: total de vendas de 5 em 5 minutos).
- **Hopping Windows:** Janelas com tamanho fixo que avançam em um intervalo menor (ex: média móvel dos últimos 10 minutos, atualizada a cada 1 minuto).
- **Session Windows:** Janelas baseadas em períodos de atividade seguidos por um tempo de inatividade (ex: sessão do usuário no app).
