# Diretrizes de Diagramas C4 com Mermaid

Esta referência padroniza a criação de diagramas arquiteturais no modelo C4 (Contexto e Contêiner) utilizando sintaxe Mermaid nativa suportada pelo GitHub e documentação técnica em Markdown.

---

## 1. Regras de Sintaxe e Renderização Segura
- **Evite tags HTML dentro de rótulos**: Tags como `<br>` ou `<b>` podem quebrar renderizadores estritos. Prefira quebras de linha normais com aspas duplas: `["Texto da Linha 1\nTexto da Linha 2"]`.
- **Sempre utilize aspas duplas** em rótulos contendo parênteses, colchetes, dois-pontos ou caracteres especiais. Exemplo: `api["API de Pagamentos (.NET 10)"]`.
- **Utilize subgrafos (`subgraph`)** para delimitar limites de sistema e limites corporativos.

---

## 2. Nível 1: Diagrama de Contexto de Sistema (System Context)

Mostra os usuários, sistemas externos e o sistema em questão:

```mermaid
flowchart TD
    user(["Usuário / Cliente\n[Pessoa]"])
    admin(["Administrador\n[Pessoa]"])

    subgraph Enterprise ["Ambiente Corporativo"]
        system["Sistema WorkSpec\n[Software System]\nPlataforma de gestão de especificações técnicas"]
    end

    gatewayExt["Gateway de Pagamento Externo\n[Software System]"]
    authProvider["Provedor de Identidade OAuth/OIDC\n[Software System]"]

    user -->|"Consulta e edita especificações via HTTPS"| system
    admin -->|"Gerencia permissões e configurações"| system
    system -->|"Processa cobranças"| gatewayExt
    system -->|"Autentica usuários via JWT"| authProvider
```

---

## 3. Nível 2: Diagrama de Contêineres (Container Diagram)

Exibe as aplicações executáveis, bancos de dados, filas e serviços que compõem o sistema com base na topologia .NET Aspire:

```mermaid
flowchart TD
    user(["Usuário Final\n[Browser / Mobile]"])

    subgraph AspireApp ["WorkSpec System (.NET Aspire Orchestration)"]
        frontend["Web Frontend\n[SPA / Blazor]\nInterface gráfica do usuário"]
        api["Web API\n[.NET 10 Minimal API]\nOrquestração de casos de uso e regras de negócio"]
        worker["Background Worker\n[.NET 10 BackgroundService]\nProcessamento assíncrono de eventos"]

        db[("PostgreSQL\n[Database]\nArmazenamento relacional e transações")]
        cache[("Redis\n[Cache / In-Memory]\nSessões e cache distribuído")]
        queue[["RabbitMQ\n[Message Broker]\nFilas de eventos de domínio"]]
    end

    user -->|"Consome via HTTPS/JSON"| frontend
    frontend -->|"Chama endpoints REST"| api
    api -->|"Persiste e consulta via Dapper"| db
    api -->|"Cache de respostas e controle de concorrência"| cache
    api -->|"Publica eventos de domínio"| queue
    queue -->|"Consome mensagens de tarefas longas"| worker
    worker -->|"Atualiza status de processamento"| db
```
