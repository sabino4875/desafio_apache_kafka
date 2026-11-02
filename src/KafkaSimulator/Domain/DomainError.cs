using System;
using System.Collections.Generic;
using System.Text;

namespace Kafka.Producer.Simulator.Domain
{
    internal sealed record DomainError(string Code, string Message)
    {
        public static readonly DomainError HttpFailure =
            new("HTTP_FAILURE", "Falha ao enviar pedido para a API.");

        public static readonly DomainError Timeout =
            new("TIMEOUT", "Requisição excedeu o tempo limite.");

        public static readonly DomainError Unexpected =
            new("UNEXPECTED", "Erro inesperado ao despachar pedido.");
    }
}
