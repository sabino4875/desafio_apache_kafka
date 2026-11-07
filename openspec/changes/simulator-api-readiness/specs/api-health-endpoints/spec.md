# Spec Delta

## Purpose

Expõe os endpoints padronizados de liveness e readiness do .NET Aspire na API de Pedidos para permitir monitoramento contínuo de integridade e orquestração distribuída.

## ADDED Requirements

### Requirement: Exposição de Endpoints de Saúde Padrão
A API de pedidos MUST mapear os endpoints `/health` e `/alive` conforme as diretrizes do .NET Aspire Service Defaults.

#### Scenario: Consulta de saúde geral
- **WHEN** uma requisição HTTP GET é enviada para `/health`
- **THEN** a API responde com código HTTP 200 OK e o relatório de integridade da aplicação

#### Scenario: Consulta de liveness
- **WHEN** uma requisição HTTP GET é enviada para `/alive`
- **THEN** a API responde com código HTTP 200 OK indicando que o processo está vivo
