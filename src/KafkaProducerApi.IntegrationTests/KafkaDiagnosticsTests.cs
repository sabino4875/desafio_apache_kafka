namespace Kafka.Producer.Api.IntegrationTests
{
    using Kafka.Common.Diagnostics;
    using System;
    using System.Diagnostics;
    using Xunit;

    public sealed class KafkaDiagnosticsTests
    {
        [Fact]
        public void KafkaDiagnostics_ShouldHaveConfiguredMeterAndActivitySource()
        {
            // Assert
            Assert.Equal("Kafka.Observability", KafkaDiagnostics.MeterName);
            Assert.Equal("Kafka.Traces", KafkaDiagnostics.ActivitySourceName);
            Assert.NotNull(KafkaDiagnostics.Meter);
            Assert.NotNull(KafkaDiagnostics.ActivitySource);
        }

        [Fact]
        public void RecordMessageProduced_ShouldExecuteWithoutExceptions()
        {
            // Act & Assert
            var exception = Record.Exception(() =>
            {
                KafkaDiagnostics.RecordMessageProduced("test-topic", 0, 512);
            });

            Assert.Null(exception);
        }

        [Fact]
        public void RecordMessageConsumed_ShouldExecuteWithoutExceptions()
        {
            // Act & Assert
            var exception = Record.Exception(() =>
            {
                KafkaDiagnostics.RecordMessageConsumed("test-topic", 0, 512);
            });

            Assert.Null(exception);
        }

        [Fact]
        public void UpdatePartitionLag_ShouldUpdateCorrectlyWithoutExceptions()
        {
            // Act & Assert
            var exception = Record.Exception(() =>
            {
                KafkaDiagnostics.UpdatePartitionLag("test-topic", 0, 10);
                KafkaDiagnostics.UpdatePartitionLag("test-topic", 1, 25);
            });

            Assert.Null(exception);
        }

        [Fact]
        public void ActivitySource_ShouldAllowStartingActivity()
        {
            // Act
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == KafkaDiagnostics.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(listener);

            using var activity = KafkaDiagnostics.ActivitySource.StartActivity("test.produce", ActivityKind.Producer);

            // Assert
            Assert.NotNull(activity);
            Assert.Equal("test.produce", activity.OperationName);
        }
    }
}
