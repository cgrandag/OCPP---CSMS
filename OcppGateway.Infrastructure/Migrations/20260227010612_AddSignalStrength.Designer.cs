using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using OcppGateway.Infrastructure.Persistence;

#nullable disable

namespace OcppGateway.Infrastructure.Migrations
{
    [DbContext(typeof(PostgresDbContext))]
    [Migration("20260227010612_AddSignalStrength")]
    partial class AddSignalStrength
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("OcppGateway.Domain.Charger", b =>
                {
                    b.Property<string>("ChargePointId")
                        .HasColumnType("text");

                    b.Property<string>("ConnectorType")
                        .HasColumnType("text");

                    b.Property<decimal?>("CurrentPowerKw")
                        .HasColumnType("numeric");

                    b.Property<string>("FirmwareVersion")
                        .HasColumnType("text");

                    b.Property<bool>("IsOnline")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("LastHeartbeat")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("LatestErrorCode")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("LatestInfo")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Location")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Model")
                        .HasColumnType("text");

                    b.Property<string>("NetworkType")
                        .HasColumnType("text");

                    b.Property<int?>("Rssi")
                        .HasColumnType("integer");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("ChargePointId");

                    b.ToTable("Chargers");
                });

            modelBuilder.Entity("OcppGateway.Domain.MeterValue", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("ChargePointId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("ConnectorId")
                        .HasColumnType("integer");

                    b.Property<string>("Measurand")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("TransactionId")
                        .HasColumnType("integer");

                    b.Property<string>("Unit")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("MeterValues");
                });

            modelBuilder.Entity("OcppGateway.Domain.Transaction", b =>
                {
                    b.Property<int>("TransactionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("TransactionId"));

                    b.Property<string>("ChargePointId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("ConnectorId")
                        .HasColumnType("integer");

                    b.Property<string>("IdTag")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("PricePerKwh")
                        .HasColumnType("numeric");

                    b.Property<int>("StartMeterValue")
                        .HasColumnType("integer");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("StopMeterValue")
                        .HasColumnType("integer");

                    b.Property<string>("StopReason")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime?>("StopTime")
                        .HasColumnType("timestamp with time zone");

                    b.Property<decimal>("TotalCost")
                        .HasColumnType("numeric");

                    b.HasKey("TransactionId");

                    b.HasIndex("ChargePointId");

                    b.ToTable("Transactions");
                });

            modelBuilder.Entity("OcppGateway.Domain.Transaction", b =>
                {
                    b.HasOne("OcppGateway.Domain.Charger", "Charger")
                        .WithMany("Transactions")
                        .HasForeignKey("ChargePointId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Charger");
                });

            modelBuilder.Entity("OcppGateway.Domain.Charger", b =>
                {
                    b.Navigation("Transactions");
                });
#pragma warning restore 612, 618
        }
    }
}
