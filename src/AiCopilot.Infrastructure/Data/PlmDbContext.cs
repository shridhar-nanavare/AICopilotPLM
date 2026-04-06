using AiCopilot.Infrastructure.Data.Entities;
using AiCopilot.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace AiCopilot.Infrastructure.Data;

public class PlmDbContext : DbContext
{
    private readonly ICurrentTenantProvider _currentTenantProvider;

    public PlmDbContext(DbContextOptions<PlmDbContext> options, ICurrentTenantProvider currentTenantProvider)
        : base(options)
    {
        _currentTenantProvider = currentTenantProvider;
    }

    private string CurrentTenantId => _currentTenantProvider.TenantId;

    public DbSet<Part> Parts => Set<Part>();
    public DbSet<BomItem> Bom => Set<BomItem>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Embedding> Embeddings => Set<Embedding>();
    public DbSet<PartFeature> PartFeatures => Set<PartFeature>();
    public DbSet<DigitalTwinState> DigitalTwinStates => Set<DigitalTwinState>();
    public DbSet<LearningMemory> LearningMemories => Set<LearningMemory>();
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
            entity.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(128).IsRequired();
            entity.Property(x => x.PartNumber).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.CreatedUtc).HasDefaultValueSql("NOW()");
            entity.HasQueryFilter(x => x.TenantId == CurrentTenantId);
            entity.HasIndex(x => new { x.TenantId, x.PartNumber }).IsUnique();
        });

        modelBuilder.Entity<BomItem>(entity =>
        {
            entity.ToTable("bom");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(128).IsRequired();
            entity.Property(x => x.Quantity).HasColumnType("numeric(18,6)");
            entity.Property(x => x.CreatedUtc).HasDefaultValueSql("NOW()");
            entity.HasQueryFilter(x => x.TenantId == CurrentTenantId);

            entity.HasOne(x => x.ParentPart)
                .WithMany(x => x.ParentBomItems)
                .HasForeignKey(x => x.ParentPartId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ChildPart)
                .WithMany(x => x.ChildBomItems)
                .HasForeignKey(x => x.ChildPartId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.TenantId, x.ParentPartId, x.ChildPartId }).IsUnique();
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(128).IsRequired();
            entity.Property(x => x.FileName).HasMaxLength(512).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.StoragePath).HasMaxLength(1024).IsRequired();
            entity.Property(x => x.CreatedUtc).HasDefaultValueSql("NOW()");

            entity.HasOne(x => x.Part)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.PartId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(x => x.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<PartFeature>(entity =>
        {
            entity.ToTable("part_features");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(128).IsRequired();
            entity.Property(x => x.UsageCount)
                .HasColumnName("usage_count")
                .HasDefaultValue(0);
            entity.Property(x => x.FailureRate)
                .HasColumnName("failure_rate")
                .HasColumnType("double precision")
                .HasDefaultValue(0d);
            entity.Property(x => x.Lifecycle)
                .HasColumnName("lifecycle")
                .HasMaxLength(64)
                .IsRequired();
            entity.Property(x => x.Cost)
                .HasColumnName("cost")
                .HasColumnType("numeric(18,2)");
            entity.Property(x => x.UpdatedUtc)
                .HasColumnName("updated_utc")
                .HasDefaultValueSql("NOW()");

            entity.HasOne(x => x.Part)
                .WithOne(x => x.Features)
                .HasForeignKey<PartFeature>(x => x.PartId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(x => x.TenantId == CurrentTenantId);
            entity.HasIndex(x => x.PartId)
                .IsUnique();
        });

        modelBuilder.Entity<DigitalTwinState>(entity =>
        {
            entity.ToTable("digital_twin_state");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(128).IsRequired();
            entity.Property(x => x.PartHealth)
                .HasColumnName("part_health")
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.RiskScore)
                .HasColumnName("risk_score")
                .HasColumnType("double precision");
            entity.Property(x => x.Trends)
                .HasColumnName("trends")
                .HasColumnType("jsonb")
                .IsRequired();
            entity.Property(x => x.UpdatedUtc)
                .HasColumnName("updated_utc")
                .HasDefaultValueSql("NOW()");

            entity.HasOne(x => x.Part)
                .WithOne(x => x.DigitalTwinState)
                .HasForeignKey<DigitalTwinState>(x => x.PartId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(x => x.TenantId == CurrentTenantId);
            entity.HasIndex(x => x.PartId)
                .IsUnique();
        });

        modelBuilder.Entity<LearningMemory>(entity =>
        {
            entity.ToTable("learning_memory");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(128).IsRequired();
            entity.Property(x => x.Scenario)
                .HasColumnName("scenario")
                .HasColumnType("text")
                .IsRequired();
            entity.Property(x => x.Plan)
                .HasColumnName("plan")
                .HasColumnType("jsonb")
                .IsRequired();
            entity.Property(x => x.SuccessRate)
                .HasColumnName("success_rate")
                .HasColumnType("double precision")
                .HasDefaultValue(0d);
            entity.Property(x => x.ExecutionCount)
                .HasColumnName("execution_count")
                .HasDefaultValue(0);
            entity.Property(x => x.LastOutcome)
                .HasColumnName("last_outcome")
                .HasColumnType("text")
                .IsRequired();
            entity.Property(x => x.UpdatedUtc)
                .HasColumnName("updated_utc")
                .HasDefaultValueSql("NOW()");

            entity.HasQueryFilter(x => x.TenantId == CurrentTenantId);
            entity.HasIndex(x => new { x.TenantId, x.Scenario })
                .IsUnique();
        });

        modelBuilder.Entity<Embedding>(entity =>
        {
            entity.ToTable("embeddings");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(128).IsRequired();
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

            entity.HasQueryFilter(x => x.TenantId == CurrentTenantId);
            entity.HasIndex(x => x.Vector)
                .HasMethod("ivfflat")
                .HasOperators("vector_cosine_ops");
        });

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.ToTable("chat_sessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(128).IsRequired();
            entity.Property(x => x.CreatedUtc).HasDefaultValueSql("NOW()");
            entity.Property(x => x.UpdatedUtc).HasDefaultValueSql("NOW()");
            entity.HasQueryFilter(x => x.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("chat_messages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(128).IsRequired();
            entity.Property(x => x.Role).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Content).HasColumnType("text").IsRequired();
            entity.Property(x => x.CreatedUtc).HasDefaultValueSql("NOW()");

            entity.HasOne(x => x.ChatSession)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(x => x.TenantId == CurrentTenantId);
            entity.HasIndex(x => new { x.ChatSessionId, x.CreatedUtc });
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("feedback");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TenantId).HasColumnName("tenant_id").HasMaxLength(128).IsRequired();
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

            entity.HasQueryFilter(x => x.TenantId == CurrentTenantId);
            entity.HasIndex(x => x.EmbeddingId);
            entity.HasIndex(x => x.ChatSessionId);
        });
    }

    public override int SaveChanges()
    {
        ApplyTenantIds();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTenantIds();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTenantIds()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && string.IsNullOrWhiteSpace(entry.Entity.TenantId))
            {
                entry.Entity.TenantId = CurrentTenantId;
            }
        }
    }
}
