namespace Kafka.Database.Migrations.Migrations
{
    using FluentMigrator;

    [Migration(202611060002, "Criacao da tabela outbox_messages para Outbox Pattern e CDC")]
    public sealed class CreateOutboxMessagesTableMigration : Migration
    {
        public override void Up()
        {
            this.Create.Table("outbox_messages")
                .WithColumn("id").AsGuid().NotNullable().PrimaryKey("pk_outbox_messages")
                .WithColumn("aggregate_type").AsString(100).NotNullable()
                .WithColumn("aggregate_id").AsString(100).NotNullable()
                .WithColumn("type").AsString(100).NotNullable()
                .WithColumn("payload").AsCustom("text").NotNullable()
                .WithColumn("created_at").AsCustom("timestamptz").NotNullable()
                .WithColumn("processed_at").AsCustom("timestamptz").Nullable();

            this.Create.Index("ix_outbox_messages_processed_at")
                .OnTable("outbox_messages")
                .OnColumn("processed_at")
                .Ascending();
        }

        public override void Down()
        {
            this.Delete.Table("outbox_messages");
        }
    }
}
