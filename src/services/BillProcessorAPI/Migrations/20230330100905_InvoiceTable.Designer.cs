﻿// <auto-generated />
using System;
using BillProcessorAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BillProcessorAPI.Migrations
{
    [DbContext(typeof(BillProcessorDbContext))]
    [Migration("20230330100905_InvoiceTable")]
    partial class InvoiceTable
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("BillProcessorAPI.Entities.BillCharge", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid?>("BusinessId")
                        .HasColumnType("uuid");

                    b.Property<string>("ChannelModel")
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid?>("CreatedById")
                        .HasColumnType("uuid");

                    b.Property<decimal>("MaxChargeAmount")
                        .HasColumnType("numeric");

                    b.Property<decimal>("MinChargeAmount")
                        .HasColumnType("numeric");

                    b.Property<double>("PercentageCharge")
                        .HasColumnType("double precision");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.ToTable("Charges");
                });

            modelBuilder.Entity("BillProcessorAPI.Entities.BillPayerInfo", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("AccountInfoRequestData")
                        .HasColumnType("text");

                    b.Property<string>("AccountInfoResponseData")
                        .HasColumnType("text");

                    b.Property<string>("AcctCloseDate")
                        .HasColumnType("text");

                    b.Property<string>("AgencyCode")
                        .HasColumnType("text");

                    b.Property<string>("AgencyName")
                        .HasColumnType("text");

                    b.Property<decimal>("AmountDue")
                        .HasColumnType("numeric");

                    b.Property<string>("CbnCode")
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid?>("CreatedById")
                        .HasColumnType("uuid");

                    b.Property<string>("CreditAccount")
                        .HasColumnType("text");

                    b.Property<string>("Currency")
                        .HasColumnType("text");

                    b.Property<decimal>("MaxAmount")
                        .HasColumnType("numeric");

                    b.Property<decimal>("MinAmount")
                        .HasColumnType("numeric");

                    b.Property<string>("OraAgencyRev")
                        .HasColumnType("text");

                    b.Property<string>("PayerName")
                        .HasColumnType("text");

                    b.Property<string>("PaymentFlag")
                        .HasColumnType("text");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("text");

                    b.Property<string>("Pid")
                        .HasColumnType("text");

                    b.Property<string>("ReadOnly")
                        .HasColumnType("text");

                    b.Property<string>("RevName")
                        .HasColumnType("text");

                    b.Property<string>("RevenueCode")
                        .HasColumnType("text");

                    b.Property<string>("State")
                        .HasColumnType("text");

                    b.Property<string>("Status")
                        .HasColumnType("text");

                    b.Property<string>("StatusMessage")
                        .HasColumnType("text");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("billCode")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("BillPayers");
                });

            modelBuilder.Entity("BillProcessorAPI.Entities.BillTransaction", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal>("Amount")
                        .HasColumnType("numeric");

                    b.Property<decimal>("AmountDue")
                        .HasColumnType("numeric");

                    b.Property<decimal>("AmountPaid")
                        .HasColumnType("numeric");

                    b.Property<string>("BillNumber")
                        .HasColumnType("text");

                    b.Property<Guid?>("BillPayerInfoId")
                        .HasColumnType("uuid");

                    b.Property<string>("Channel")
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid?>("CreatedById")
                        .HasColumnType("uuid");

                    b.Property<string>("DateCompleted")
                        .HasColumnType("text");

                    b.Property<string>("DueDate")
                        .HasColumnType("text");

                    b.Property<string>("FiName")
                        .HasColumnType("text");

                    b.Property<decimal>("GatewayTransactionCharge")
                        .HasColumnType("numeric");

                    b.Property<string>("GatewayTransactionReference")
                        .HasColumnType("text");

                    b.Property<int>("GatewayType")
                        .HasColumnType("integer");

                    b.Property<string>("Hash")
                        .HasColumnType("text");

                    b.Property<string>("Narration")
                        .HasColumnType("text");

                    b.Property<string>("PayerName")
                        .HasColumnType("text");

                    b.Property<string>("PaymentInfoRequestData")
                        .HasColumnType("text");

                    b.Property<string>("PaymentInfoResponseData")
                        .HasColumnType("text");

                    b.Property<string>("PaymentReference")
                        .HasColumnType("text");

                    b.Property<string>("PaymentUrl")
                        .HasColumnType("text");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("text");

                    b.Property<string>("Pid")
                        .HasColumnType("text");

                    b.Property<decimal>("PrinciPalAmount")
                        .HasColumnType("numeric");

                    b.Property<string>("ReceiptUrl")
                        .HasColumnType("text");

                    b.Property<string>("ResourcePIN")
                        .HasColumnType("text");

                    b.Property<string>("RevName")
                        .HasColumnType("text");

                    b.Property<string>("Status")
                        .HasColumnType("text");

                    b.Property<string>("StatusMessage")
                        .HasColumnType("text");

                    b.Property<string>("SuccessIndicator")
                        .HasColumnType("text");

                    b.Property<decimal>("TransactionCharge")
                        .HasColumnType("numeric");

                    b.Property<string>("TransactionReference")
                        .HasColumnType("text");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("BillPayerInfoId");

                    b.ToTable("BillTransactions");
                });

            modelBuilder.Entity("BillProcessorAPI.Entities.Invoice", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal>("AmountDue")
                        .HasColumnType("numeric");

                    b.Property<decimal>("AmountPaid")
                        .HasColumnType("numeric");

                    b.Property<string>("BillNumber")
                        .HasColumnType("text");

                    b.Property<Guid>("BillTransactionId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<Guid?>("CreatedById")
                        .HasColumnType("uuid");

                    b.Property<string>("DueDate")
                        .HasColumnType("text");

                    b.Property<decimal>("GatewayTransactionCharge")
                        .HasColumnType("numeric");

                    b.Property<string>("GatewayTransactionReference")
                        .HasColumnType("text");

                    b.Property<int>("GatewayType")
                        .HasColumnType("integer");

                    b.Property<string>("PayerName")
                        .HasColumnType("text");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("text");

                    b.Property<string>("Pid")
                        .HasColumnType("text");

                    b.Property<Guid>("ReceiptId")
                        .HasColumnType("uuid");

                    b.Property<string>("ReceiptUrl")
                        .HasColumnType("text");

                    b.Property<string>("RevName")
                        .HasColumnType("text");

                    b.Property<decimal>("TransactionCharge")
                        .HasColumnType("numeric");

                    b.Property<string>("TransactionReference")
                        .HasColumnType("text");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("BillTransactionId");

                    b.HasIndex("ReceiptId");

                    b.ToTable("Invoices");
                });

            modelBuilder.Entity("BillProcessorAPI.Entities.Receipt", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal>("AmountDue")
                        .HasColumnType("numeric");

                    b.Property<decimal>("AmountPaid")
                        .HasColumnType("numeric");

                    b.Property<string>("GateWay")
                        .HasColumnType("text");

                    b.Property<string>("GatewayTransactionReference")
                        .HasColumnType("text");

                    b.Property<string>("PaymentRef")
                        .HasColumnType("text");

                    b.Property<string>("ReceiptUrl")
                        .HasColumnType("text");

                    b.Property<string>("TransactionDate")
                        .HasColumnType("text");

                    b.Property<Guid>("TransactionId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("TransactionId")
                        .IsUnique();

                    b.ToTable("Receipts");
                });

            modelBuilder.Entity("BillProcessorAPI.Entities.BillTransaction", b =>
                {
                    b.HasOne("BillProcessorAPI.Entities.BillPayerInfo", "BillPayerInfo")
                        .WithMany("BillTransactions")
                        .HasForeignKey("BillPayerInfoId");

                    b.Navigation("BillPayerInfo");
                });

            modelBuilder.Entity("BillProcessorAPI.Entities.Invoice", b =>
                {
                    b.HasOne("BillProcessorAPI.Entities.BillTransaction", "BillTransaction")
                        .WithMany()
                        .HasForeignKey("BillTransactionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BillProcessorAPI.Entities.Receipt", "Receipt")
                        .WithMany()
                        .HasForeignKey("ReceiptId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("BillTransaction");

                    b.Navigation("Receipt");
                });

            modelBuilder.Entity("BillProcessorAPI.Entities.Receipt", b =>
                {
                    b.HasOne("BillProcessorAPI.Entities.BillTransaction", "Transaction")
                        .WithOne("Receipts")
                        .HasForeignKey("BillProcessorAPI.Entities.Receipt", "TransactionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Transaction");
                });

            modelBuilder.Entity("BillProcessorAPI.Entities.BillPayerInfo", b =>
                {
                    b.Navigation("BillTransactions");
                });

            modelBuilder.Entity("BillProcessorAPI.Entities.BillTransaction", b =>
                {
                    b.Navigation("Receipts");
                });
#pragma warning restore 612, 618
        }
    }
}
