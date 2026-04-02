using AiCopilot.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace AiCopilot.Infrastructure.Data;

public class PlmDbContext(DbContextOptions<PlmDbContext> options) : DbContext(options)
{
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<BomItem> Bom => Set<BomItem>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Embedding> Embeddings => Set<Embedding>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Feedback> Feedback => Set<Feedback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Part>(entity =>
        {
            entity.ToTable("parts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PartNumber).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.CreatedUtc).HasDefaultValueSql("NOW()");
            entity.HasIndex(x => x.PartNumber).IsUnique();
        });

        modelBuilder.Entity<BomItem>(entity =>
        {
            entity.ToTable("bom");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Quantity).HasColumnType("numeric(18,6)");
            entity.Property(x => x.CreatedUtc).HasDefaultValueSql("NOW()");

            entity.HasOne(x => x.ParentPart)
                .WithMany(x => x.ParentBomItems)
                .HasForeignKey(x => x.ParentPartId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ChildPart)
                .WithMany(x => x.ChildBomItems)
                .HasForeignKey(x => x.ChildPartId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.ParentPartId, x.ChildPartId }).IsUnique();
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FileName).HasMaxLength(512).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.StoragePath).HasMaxLength(1024).IsRequired();
            entity.Property(x => x.CreatedUtc).HasDefaultValueSql("NOW()");

            entity.HasOne(x => x.Part)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.PartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Embedding>(entity =>
        {
            entity.ToTable("embeddings");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ChunkText).HasColumnType("text").IsRequired();
            entity.Property(x => x.Vector).HasColumnType("vector(1536)").IsRequired();
            entity.Property(x => x.FeedbackScore)
                .HasColumnName("feedback_score")
                .HasColumnType("double precision")
                .HasDefaultValue(0d);
            entity.Property(x => x.UsageCount)
                .HasColumnName("usage_count")
                .HasDefaultValue(0);
            entity.Property(x => x.LastUsed)
                .HasColumnName("last_used")
                .HasColumnType("timestamp with time zone");
            entity.Property(x => x.CreatedUtc).HasDefaultValueSql("NOW()");

            entity.HasOne(x => x.Document)
                .WithMany(x => x.Embeddings)
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.Vector)
                .HasMethod("ivfflat")
                .HasOperators("vector_cosine_ops");
        });

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.ToTable("chat_sessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CreatedUtc).HasDefaultValueSql("NOW()");
            entity.Property(x => x.UpdatedUtc).HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("chat_messages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Role).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Content).HasColumnType("text").IsRequired();
            entity.Property(x => x.CreatedUtc).HasDefaultValueSql("NOW()");

            entity.HasOne(x => x.ChatSession)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.ChatSessionId, x.CreatedUtc });
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("feedback");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Score)
                .HasColumnType("double precision");
            entity.Property(x => x.Comment)
                .HasColumnType("text");
            entity.Property(x => x.CreatedUtc).HasDefaultValueSql("NOW()");

            entity.HasOne(x => x.Embedding)
                .WithMany(x => x.FeedbackEntries)
                .HasForeignKey(x => x.EmbeddingId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ChatSession)
                .WithMany(x => x.FeedbackEntries)
                .HasForeignKey(x => x.ChatSessionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => x.EmbeddingId);
            entity.HasIndex(x => x.ChatSessionId);
        });
    }
}
