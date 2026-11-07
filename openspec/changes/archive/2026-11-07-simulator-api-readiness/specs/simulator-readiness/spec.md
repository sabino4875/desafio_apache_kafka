# Spec Delta

## Purpose

Permite que o serviço simulador de carga sonde a API de pedidos antes de disparar requisições, evitando erros de recusa de conexão durante a inicialização e migração do sistema.

## ADDED Requirements

### Requirement: Sondagem de Prontidão da API
O simulador de carga MUST verificar se a API de pedidos está acessível e respondendo com sucesso antes de iniciar o envio contínuo de pedidos de venda.

#### Scenario: API indisponível no startup inicial
- **GIVEN** a API ainda está executando migrações ou inicializando o Kestrel
- **WHEN** o worker do simulador inicia sua execução
- **THEN** o simulador realiza sondagem com retentativas e registra mensagens informativas no log sem falhar a aplicação

#### Scenario: API pronta para receber tráfego
- **GIVEN** a API concluiu a inicialização e responde com sucesso na checagem de saúde
- **WHEN** o simulador recebe resposta positiva
- **THEN** o simulador conclui a sondagem e prossegue para o ciclo de despacho de pedidos
