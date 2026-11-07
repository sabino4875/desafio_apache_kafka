namespace Kafka.Database.Migrations.Migrations
{
    using FluentMigrator;

    [Migration(202611060001, "Criacao das tabelas de orders e order_items")]
    public sealed class CreateOrdersAndItemsTablesMigration : Migration
    {
        public override void Up()
        {
            this.Create.Table("orders")
                .WithColumn("id").AsString(100).NotNullable().PrimaryKey("pk_orders")
                .WithColumn("client_id").AsString(100).NotNullable()
                .WithColumn("total").AsDecimal(18, 2).NotNullable()
                .WithColumn("created_at").AsCustom("timestamptz").NotNullable();

            this.Create.Table("order_items")
                .WithColumn("id").AsString(100).NotNullable().PrimaryKey("pk_order_items")
                .WithColumn("order_id").AsString(100).NotNullable()
                    .ForeignKey("fk_order_items_orders", "orders", "id")
                    .OnDelete(System.Data.Rule.Cascade)
                .WithColumn("product_id").AsString(100).NotNullable()
                .WithColumn("product_name").AsString(200).NotNullable()
                .WithColumn("quantity").AsInt32().NotNullable()
                .WithColumn("unit_price").AsDecimal(18, 2).NotNullable()
                .WithColumn("item_total").AsDecimal(18, 2).NotNullable();

            this.Create.Index("ix_order_items_order_id")
                .OnTable("order_items")
                .OnColumn("order_id")
                .Ascending();
        }

        public override void Down()
        {
            this.Delete.Table("order_items");
            this.Delete.Table("orders");
        }
    }
}
