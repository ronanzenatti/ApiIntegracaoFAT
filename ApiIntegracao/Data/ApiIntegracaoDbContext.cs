// Data/ApiIntegracaoDbContext.cs
using ApiIntegracao.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ApiIntegracao.Data
{
    public class ApiIntegracaoDbContext : DbContext
    {
        public ApiIntegracaoDbContext(DbContextOptions<ApiIntegracaoDbContext> options)
            : base(options) { }

        // DbSets - Tabelas do banco
        public DbSet<Curso> Cursos { get; set; }
        public DbSet<Turma> Turmas { get; set; }
        public DbSet<Aluno> Alunos { get; set; }
        public DbSet<Matricula> Matriculas { get; set; }
        public DbSet<AulaGerada> AulasGeradas { get; set; }
        public DbSet<FrequenciaProcessada> FrequenciasProcessadas { get; set; }
        public DbSet<SyncLog> SyncLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Aplicar todas as configurações de uma vez
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApiIntegracaoDbContext).Assembly);

            // Configuração global para soft delete
            ConfigurarSoftDelete(modelBuilder);
        }

        private void ConfigurarSoftDelete(ModelBuilder modelBuilder)
        {
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(Models.Base.AuditableEntity).IsAssignableFrom(entityType.ClrType))
                {
                    // Criar filtro global para soft delete
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
                        entry.Entity.Id = Guid.NewGuid();
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        break;

                    case EntityState.Modified:
                        entry.Entity.UpdatedAt = DateTime.UtcNow;
                        entry.Property(x => x.CreatedAt).IsModified = false;
                        break;

                    case EntityState.Deleted:
                        // Implementar soft delete
                        entry.State = EntityState.Modified;
                        entry.Entity.DeletedAt = DateTime.UtcNow;
                        break;
                }
            }
        }
    }
}