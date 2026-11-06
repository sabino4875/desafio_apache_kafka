namespace Kafka.Producer.Simulator.Infrastructure.Factories
{
    using Kafka.Common.Application.Dto;
    using Kafka.Producer.Simulator.Application.UseCases;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Security.Cryptography;

    internal static class PedidoFactory
    {
        public static DispatchPedidoRequest CreateRandom()
        {
            var orderId = Guid.NewGuid().ToString("N").Replace("-", "", StringComparison.InvariantCulture);
            var clientId = GerarChaveAleatoria();
            var items = new Collection<OrderItemDto>();
            var itemRange = RandomNumberGenerator.GetInt32(1, 5);
            var total = decimal.Zero;
            while(items.Count < itemRange)
            {
                var description = GerarEquipamentoAleatorio();
                
                if(!items.Any(e => string.Equals(e.ProductName.ToUpperInvariant(), description.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase)))
                {
                    var id = Guid.NewGuid().ToString("N").Replace("-", "", StringComparison.InvariantCulture);
                    var qty = RandomNumberGenerator.GetInt32(1, 10);
                    var price = GerarValorMonetarioSeguro();
                    var itemTotal = price * (decimal)qty;
                    var item = new OrderItemDto(ProductId: id,
                                                ProductName: description,
                                                Quantity: qty,
                                                UnitPrice: price,
                                                ItemTotal: itemTotal);
                    items.Add(item);
                    total += itemTotal;
                }
            }
            return new DispatchPedidoRequest(
                OrderId: orderId,
                ClientId: clientId,
                Total: total,
                CreatedAt: DateTime.UtcNow,
                Items: items
            );
        }

        private static decimal GerarValorMonetarioSeguro()
        {
            decimal valorMinimo = 10.00m;
            decimal valorMaximo = 500.00m;

            // 1. Convertemos os valores para a escala de centavos inteiros
            int minCentavos = (int)(valorMinimo * 100);
            int maxCentavos = (int)(valorMaximo * 100);

            // 2. RandomNumberGenerator.GetInt32 possui limite superior EXCLUSIVO. 
            // Adicionamos +1 para permitir que o valor máximo exato possa ser sorteado.
            int centavosAleatorios = RandomNumberGenerator.GetInt32(minCentavos, maxCentavos + 1);

            // 3. Dividimos de volta por 100 usando o sufixo 'm' para garantir o tipo decimal
            return Math.Round(((decimal)centavosAleatorios / 100m) * (4_900 + 100), 2);
        }

        private static string GerarEquipamentoAleatorio()
        {
            string[] Equipamentos = ["Notebook", "Mouse", "Monitor", "Teclado", "Headset", "Impressora", "SSD Externo", "Gabinete Gamer"];
            string[] Marcas = ["Dell", "HP", "Logitech", "Asus", "Samsung", "Corsair", "Razer", "Lenovo"];
            string[] LetrasCodigo = ["X", "Pro", "Ultra", "Max", "Prime", "Plus"];

            // Sorteia os índices de forma criptograficamente segura
            int idxEquipamento = RandomNumberGenerator.GetInt32(0, Equipamentos.Length);
            int idxMarca = RandomNumberGenerator.GetInt32(0, Marcas.Length);
            int idxLetra = RandomNumberGenerator.GetInt32(0, LetrasCodigo.Length);
            int numeroModelo = RandomNumberGenerator.GetInt32(100, 999); // Gera um número de modelo de 100 a 998

            // Junta as partes: Ex "Notebook Dell Ultra-550" ou "Mouse Logitech Pro-812"
            return $"{Equipamentos[idxEquipamento]} {Marcas[idxMarca]} {LetrasCodigo[idxLetra]}-{numeroModelo}";
        }

        private static string GerarChaveAleatoria()
        {
            var sequence = new List<char>();
            var alfabeto = "ABCDEF0123456789";

            while (sequence.Count < 32)
            {
                int indiceChar = RandomNumberGenerator.GetInt32(0, alfabeto.Length);
                sequence.Add(alfabeto[indiceChar]);
            }

            var resultado = string.Join("", sequence);

            return resultado;
        }
    }
}
