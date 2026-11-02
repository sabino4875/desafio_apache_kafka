namespace Kafka.Producer.Simulator.Infrastructure.Configuration
{
    using System;
    using System.ComponentModel.DataAnnotations;

    internal sealed class SimulatorOptions
    {
        public const string Section = "Simulator";

        [Range(100, 60000)] public int MinDelayMs { get; init; } = 500;
        [Range(100, 120000)] public int MaxDelayMs { get; init; } = 3000;
    }
}
