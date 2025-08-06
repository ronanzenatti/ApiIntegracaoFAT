using Microsoft.EntityFrameworkCore;
using ApiIntegracao.Models;
using System.Linq.Expressions;

namespace ApiIntegracao.Data
{
    public class ApiIntegracaoDbContext(DbContextOptions<ApiIntegracaoDbContext> options) : DbContext(options)
    {
        public DbSet<Aluno> Alunos { get; set; }
        public DbSet<Matricula> Matriculas { get; set; }
        public DbSet<Curso> Cursos { get; set; }
        public DbSet<Turma> Turmas { get; set; }
        public DbSet<SyncLog> SyncLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuração da entidade Aluno
            modelBuilder.Entity<Aluno>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Cpf).IsRequired().HasMaxLength(11);
                entity.Property(e => e.NomeSocial).HasMaxLength(200);
                entity.Property(e => e.Rg).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.EmailInstitucional).HasMaxLength(100);
                entity.HasIndex(e => e.IdCettpro).IsUnique();
                entity.HasIndex(e => e.Cpf).IsUnique();
            });

            // Configuração da entidade Curso
            modelBuilder.Entity<Curso>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.NomeCurso).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CargaHoraria).HasMaxLength(50);
                entity.Property(e => e.Descricao).HasMaxLength(500);
                entity.HasIndex(e => e.IdCettpro).IsUnique();
            });

            // Configuração da entidade Turma
            modelBuilder.Entity<Turma>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nome).IsRequired().HasMaxLength(200);
                entity.Property(e => e.DisciplinaNomePortalFat).HasMaxLength(200);
                entity.HasIndex(e => e.IdCettpro).IsUnique();

                // Relacionamento Turma -> Curso
                entity.HasOne(t => t.Curso)
                      .WithMany()
                      .HasForeignKey(t => t.CursoId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuração da entidade Matricula
            modelBuilder.Entity<Matricula>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.IdCettpro).IsUnique();

                // Relacionamento Matricula -> Aluno
                entity.HasOne(m => m.Aluno)
                      .WithMany()
                      .HasForeignKey(m => m.AlunoId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Relacionamento Matricula -> Turma
                entity.HasOne(m => m.Turma)
                      .WithMany()
                      .HasForeignKey(m => m.TurmaId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Índice único para evitar matrículas duplicadas
                entity.HasIndex(m => new { m.AlunoId, m.TurmaId }).IsUnique();
            });

            // Configuração da entidade SyncLog
            modelBuilder.Entity<SyncLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TipoEntidade).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Operacao).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ErroDetalhes).HasMaxLength(1000);
            });

            // Configuração global para todas as entidades auditáveis
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(Models.Base.AuditableEntity).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property("CreatedAt")
                        .HasDefaultValueSql("GETUTCDATE()");

                    modelBuilder.Entity(entityType.ClrType)
                        .Property("UpdatedAt")
                        .HasDefaultValueSql("GETUTCDATE()");

                    // Filtro global para soft delete
                    var parameter = Expression.Parameter(entityType.ClrType, "e");
                    var property = Expression.Property(parameter, "DeletedAt");
                    var constant = Expression.Constant(null, typeof(DateTime?));
                    var body = Expression.Equal(property, constant);
                    var lambda = Expression.Lambda(body, parameter);

                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }
        }

        public override int SaveChanges()
        {
            UpdateAuditableEntities();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditableEntities();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateAuditableEntities()
        {
            var entries = ChangeTracker.Entries<Models.Base.AuditableEntity>();

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.DeletedAt = DateTime.UtcNow;
                        break;
                }
            }
        }
    }
}