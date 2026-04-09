using AiCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AiCopilot.Infrastructure.Data.Migrations;

[DbContext(typeof(PlmDbContext))]
[Migration("20260409000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE EXTENSION IF NOT EXISTS vector;

            CREATE TABLE IF NOT EXISTS parts (
                "Id" uuid PRIMARY KEY,
                tenant_id character varying(128) NOT NULL,
                "PartNumber" character varying(128) NOT NULL,
                "Name" character varying(256) NOT NULL,
                "CreatedUtc" timestamp with time zone NOT NULL DEFAULT NOW()
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_parts_tenant_id_PartNumber"
                ON parts (tenant_id, "PartNumber");

            CREATE TABLE IF NOT EXISTS chat_sessions (
                "Id" uuid PRIMARY KEY,
                tenant_id character varying(128) NOT NULL,
                "CreatedUtc" timestamp with time zone NOT NULL DEFAULT NOW(),
                "UpdatedUtc" timestamp with time zone NOT NULL DEFAULT NOW()
            );

            CREATE TABLE IF NOT EXISTS documents (
                "Id" uuid PRIMARY KEY,
                tenant_id character varying(128) NOT NULL,
                "PartId" uuid NOT NULL REFERENCES parts("Id") ON DELETE CASCADE,
                "FileName" character varying(512) NOT NULL,
                "ContentType" character varying(128) NOT NULL,
                "StoragePath" character varying(1024) NOT NULL,
                "CreatedUtc" timestamp with time zone NOT NULL DEFAULT NOW()
            );

            CREATE INDEX IF NOT EXISTS "IX_documents_PartId"
                ON documents ("PartId");

            CREATE TABLE IF NOT EXISTS embeddings (
                "Id" uuid PRIMARY KEY,
                tenant_id character varying(128) NOT NULL,
                "DocumentId" uuid NOT NULL REFERENCES documents("Id") ON DELETE CASCADE,
                "ChunkText" text NOT NULL,
                "Vector" vector(1536) NOT NULL,
                feedback_score double precision NOT NULL DEFAULT 0,
                usage_count integer NOT NULL DEFAULT 0,
                last_used timestamp with time zone NULL,
                "CreatedUtc" timestamp with time zone NOT NULL DEFAULT NOW()
            );

            CREATE INDEX IF NOT EXISTS "IX_embeddings_DocumentId"
                ON embeddings ("DocumentId");

            CREATE INDEX IF NOT EXISTS "IX_embeddings_Vector"
                ON embeddings USING ivfflat ("Vector" vector_cosine_ops);

            CREATE TABLE IF NOT EXISTS bom (
                "Id" uuid PRIMARY KEY,
                tenant_id character varying(128) NOT NULL,
                "ParentPartId" uuid NOT NULL REFERENCES parts("Id") ON DELETE RESTRICT,
                "ChildPartId" uuid NOT NULL REFERENCES parts("Id") ON DELETE RESTRICT,
                "Quantity" numeric(18,6) NOT NULL,
                "CreatedUtc" timestamp with time zone NOT NULL DEFAULT NOW()
            );

            CREATE INDEX IF NOT EXISTS "IX_bom_ChildPartId"
                ON bom ("ChildPartId");

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_bom_tenant_id_ParentPartId_ChildPartId"
                ON bom (tenant_id, "ParentPartId", "ChildPartId");

            CREATE TABLE IF NOT EXISTS chat_messages (
                "Id" uuid PRIMARY KEY,
                tenant_id character varying(128) NOT NULL,
                "ChatSessionId" uuid NOT NULL REFERENCES chat_sessions("Id") ON DELETE CASCADE,
                "Role" character varying(32) NOT NULL,
                "Content" text NOT NULL,
                "CreatedUtc" timestamp with time zone NOT NULL DEFAULT NOW()
            );

            CREATE INDEX IF NOT EXISTS "IX_chat_messages_ChatSessionId_CreatedUtc"
                ON chat_messages ("ChatSessionId", "CreatedUtc");

            CREATE TABLE IF NOT EXISTS feedback (
                "Id" uuid PRIMARY KEY,
                tenant_id character varying(128) NOT NULL,
                "EmbeddingId" uuid NOT NULL REFERENCES embeddings("Id") ON DELETE CASCADE,
                "ChatSessionId" uuid NULL REFERENCES chat_sessions("Id") ON DELETE SET NULL,
                "Score" double precision NOT NULL,
                "Comment" text NULL,
                "CreatedUtc" timestamp with time zone NOT NULL DEFAULT NOW()
            );

            CREATE INDEX IF NOT EXISTS "IX_feedback_EmbeddingId"
                ON feedback ("EmbeddingId");

            CREATE INDEX IF NOT EXISTS "IX_feedback_ChatSessionId"
                ON feedback ("ChatSessionId");

            CREATE TABLE IF NOT EXISTS part_features (
                "Id" uuid PRIMARY KEY,
                tenant_id character varying(128) NOT NULL,
                "PartId" uuid NOT NULL REFERENCES parts("Id") ON DELETE CASCADE,
                usage_count integer NOT NULL DEFAULT 0,
                failure_rate double precision NOT NULL DEFAULT 0,
                lifecycle character varying(64) NOT NULL,
                cost numeric(18,2) NOT NULL,
                updated_utc timestamp with time zone NOT NULL DEFAULT NOW()
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_part_features_PartId"
                ON part_features ("PartId");

            CREATE TABLE IF NOT EXISTS digital_twin_state (
                "Id" uuid PRIMARY KEY,
                tenant_id character varying(128) NOT NULL,
                "PartId" uuid NOT NULL REFERENCES parts("Id") ON DELETE CASCADE,
                part_health character varying(32) NOT NULL,
                risk_score double precision NOT NULL,
                trends jsonb NOT NULL,
                updated_utc timestamp with time zone NOT NULL DEFAULT NOW()
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_digital_twin_state_PartId"
                ON digital_twin_state ("PartId");

            CREATE TABLE IF NOT EXISTS learning_memory (
                "Id" uuid PRIMARY KEY,
                tenant_id character varying(128) NOT NULL,
                scenario text NOT NULL,
                plan jsonb NOT NULL,
                success_rate double precision NOT NULL DEFAULT 0,
                execution_count integer NOT NULL DEFAULT 0,
                last_outcome text NOT NULL,
                updated_utc timestamp with time zone NOT NULL DEFAULT NOW()
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_learning_memory_tenant_id_scenario"
                ON learning_memory (tenant_id, scenario);

            CREATE TABLE IF NOT EXISTS audit_logs (
                "Id" uuid PRIMARY KEY,
                tenant_id character varying(128) NOT NULL,
                action character varying(256) NOT NULL,
                agent_decision text NULL,
                user_approval boolean NULL,
                performed_by character varying(256) NOT NULL,
                metadata jsonb NOT NULL,
                created_utc timestamp with time zone NOT NULL DEFAULT NOW()
            );

            CREATE INDEX IF NOT EXISTS "IX_audit_logs_tenant_id_created_utc"
                ON audit_logs (tenant_id, created_utc);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS audit_logs;
            DROP TABLE IF EXISTS learning_memory;
            DROP TABLE IF EXISTS digital_twin_state;
            DROP TABLE IF EXISTS part_features;
            DROP TABLE IF EXISTS feedback;
            DROP TABLE IF EXISTS chat_messages;
            DROP TABLE IF EXISTS bom;
            DROP TABLE IF EXISTS embeddings;
            DROP TABLE IF EXISTS documents;
            DROP TABLE IF EXISTS chat_sessions;
            DROP TABLE IF EXISTS parts;
            """);
    }
}
