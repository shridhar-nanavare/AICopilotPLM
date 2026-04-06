using System;
using AiCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;
using Pgvector.EntityFrameworkCore;

#nullable disable

namespace AiCopilot.Infrastructure.Data.Migrations
{
    [DbContext(typeof(PlmDbContext))]
    partial class PlmDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "vector");

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.BomItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("ChildPartId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("NOW()");

                    b.Property<Guid>("ParentPartId")
                        .HasColumnType("uuid");

                    b.Property<decimal>("Quantity")
                        .HasColumnType("numeric(18,6)");

                    b.HasKey("Id");

                    b.HasIndex("ChildPartId");

                    b.HasIndex("ParentPartId", "ChildPartId")
                        .IsUnique();

                    b.ToTable("bom", (string)null);
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.ChatMessage", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("ChatSessionId")
                        .HasColumnType("uuid");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("NOW()");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)");

                    b.HasKey("Id");

                    b.HasIndex("ChatSessionId", "CreatedUtc");

                    b.ToTable("chat_messages", (string)null);
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.ChatSession", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("NOW()");

                    b.Property<DateTime>("UpdatedUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("NOW()");

                    b.HasKey("Id");

                    b.ToTable("chat_sessions", (string)null);
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.Document", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("ContentType")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.Property<DateTime>("CreatedUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("NOW()");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)");

                    b.Property<Guid>("PartId")
                        .HasColumnType("uuid");

                    b.Property<string>("StoragePath")
                        .IsRequired()
                        .HasMaxLength(1024)
                        .HasColumnType("character varying(1024)");

                    b.HasKey("Id");

                    b.HasIndex("PartId");

                    b.ToTable("documents", (string)null);
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.DigitalTwinState", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("PartId")
                        .HasColumnType("uuid");

                    b.Property<string>("PartHealth")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)")
                        .HasColumnName("part_health");

                    b.Property<double>("RiskScore")
                        .HasColumnType("double precision")
                        .HasColumnName("risk_score");

                    b.Property<string>("Trends")
                        .IsRequired()
                        .HasColumnType("jsonb")
                        .HasColumnName("trends");

                    b.Property<DateTime>("UpdatedUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_utc")
                        .HasDefaultValueSql("NOW()");

                    b.HasKey("Id");

                    b.HasIndex("PartId")
                        .IsUnique();

                    b.ToTable("digital_twin_state", (string)null);
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.Embedding", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("ChunkText")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("NOW()");

                    b.Property<Guid>("DocumentId")
                        .HasColumnType("uuid");

                    b.Property<double>("FeedbackScore")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("double precision")
                        .HasColumnName("feedback_score")
                        .HasDefaultValue(0.0);

                    b.Property<DateTime?>("LastUsed")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_used");

                    b.Property<int>("UsageCount")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("usage_count")
                        .HasDefaultValue(0);

                    b.Property<Vector>("Vector")
                        .IsRequired()
                        .HasColumnType("vector(1536)");

                    b.HasKey("Id");

                    b.HasIndex("DocumentId");

                    b.HasIndex("Vector")
                        .HasMethod("ivfflat")
                        .HasOperators(new[] { "vector_cosine_ops" });

                    b.ToTable("embeddings", (string)null);
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.Feedback", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid?>("ChatSessionId")
                        .HasColumnType("uuid");

                    b.Property<string>("Comment")
                        .HasColumnType("text");

                    b.Property<DateTime>("CreatedUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("NOW()");

                    b.Property<Guid>("EmbeddingId")
                        .HasColumnType("uuid");

                    b.Property<double>("Score")
                        .HasColumnType("double precision");

                    b.HasKey("Id");

                    b.HasIndex("ChatSessionId");

                    b.HasIndex("EmbeddingId");

                    b.ToTable("feedback", (string)null);
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.Part", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("NOW()");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("PartNumber")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)");

                    b.HasKey("Id");

                    b.HasIndex("PartNumber")
                        .IsUnique();

                    b.ToTable("parts", (string)null);
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.PartFeature", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<decimal>("Cost")
                        .HasColumnType("numeric(18,2)")
                        .HasColumnName("cost");

                    b.Property<double>("FailureRate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("double precision")
                        .HasColumnName("failure_rate")
                        .HasDefaultValue(0.0);

                    b.Property<string>("Lifecycle")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)")
                        .HasColumnName("lifecycle");

                    b.Property<Guid>("PartId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("UpdatedUtc")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_utc")
                        .HasDefaultValueSql("NOW()");

                    b.Property<int>("UsageCount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasColumnName("usage_count")
                        .HasDefaultValue(0);

                    b.HasKey("Id");

                    b.HasIndex("PartId")
                        .IsUnique();

                    b.ToTable("part_features", (string)null);
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.BomItem", b =>
                {
                    b.HasOne("AiCopilot.Infrastructure.Data.Entities.Part", "ChildPart")
                        .WithMany("ChildBomItems")
                        .HasForeignKey("ChildPartId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("AiCopilot.Infrastructure.Data.Entities.Part", "ParentPart")
                        .WithMany("ParentBomItems")
                        .HasForeignKey("ParentPartId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("ChildPart");

                    b.Navigation("ParentPart");
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.Document", b =>
                {
                    b.HasOne("AiCopilot.Infrastructure.Data.Entities.Part", "Part")
                        .WithMany("Documents")
                        .HasForeignKey("PartId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Part");
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.ChatMessage", b =>
                {
                    b.HasOne("AiCopilot.Infrastructure.Data.Entities.ChatSession", "ChatSession")
                        .WithMany("Messages")
                        .HasForeignKey("ChatSessionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ChatSession");
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.Embedding", b =>
                {
                    b.HasOne("AiCopilot.Infrastructure.Data.Entities.Document", "Document")
                        .WithMany("Embeddings")
                        .HasForeignKey("DocumentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Document");
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.Feedback", b =>
                {
                    b.HasOne("AiCopilot.Infrastructure.Data.Entities.ChatSession", "ChatSession")
                        .WithMany("FeedbackEntries")
                        .HasForeignKey("ChatSessionId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("AiCopilot.Infrastructure.Data.Entities.Embedding", "Embedding")
                        .WithMany("FeedbackEntries")
                        .HasForeignKey("EmbeddingId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ChatSession");

                    b.Navigation("Embedding");
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.DigitalTwinState", b =>
                {
                    b.HasOne("AiCopilot.Infrastructure.Data.Entities.Part", "Part")
                        .WithOne("DigitalTwinState")
                        .HasForeignKey("AiCopilot.Infrastructure.Data.Entities.DigitalTwinState", "PartId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Part");
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.PartFeature", b =>
                {
                    b.HasOne("AiCopilot.Infrastructure.Data.Entities.Part", "Part")
                        .WithOne("Features")
                        .HasForeignKey("AiCopilot.Infrastructure.Data.Entities.PartFeature", "PartId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Part");
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.Document", b =>
                {
                    b.Navigation("Embeddings");
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.ChatSession", b =>
                {
                    b.Navigation("FeedbackEntries");

                    b.Navigation("Messages");
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.Embedding", b =>
                {
                    b.Navigation("FeedbackEntries");
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.Part", b =>
                {
                    b.Navigation("ChildBomItems");

                    b.Navigation("DigitalTwinState");

                    b.Navigation("Documents");

                    b.Navigation("Features");

                    b.Navigation("ParentBomItems");
                });
#pragma warning restore 612, 618
        }
    }
}
