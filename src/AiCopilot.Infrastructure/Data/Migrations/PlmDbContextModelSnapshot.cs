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

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.Embedding", b =>
                {
                    b.HasOne("AiCopilot.Infrastructure.Data.Entities.Document", "Document")
                        .WithMany("Embeddings")
                        .HasForeignKey("DocumentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Document");
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.Document", b =>
                {
                    b.Navigation("Embeddings");
                });

            modelBuilder.Entity("AiCopilot.Infrastructure.Data.Entities.Part", b =>
                {
                    b.Navigation("ChildBomItems");

                    b.Navigation("Documents");

                    b.Navigation("ParentBomItems");
                });
#pragma warning restore 612, 618
        }
    }
}
