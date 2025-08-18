using ApiIntegracao.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiIntegracao.Data.Configurations
{
    // Data/Configurations/TurmaConfiguration.cs
    public class TurmaConfiguration : IEntityTypeConfiguration<Turma>
    {
        public void Configure(EntityTypeBuilder<Turma> builder)
        {
            builder.ToTable("Turmas");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Nome)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(e => e.IdPortalFat);

            builder.Property(e => e.DisciplinaIdPortalFat);

            builder.Property(e => e.DisciplinaNomePortalFat)
                .HasMaxLength(200);

            builder.HasOne(t => t.Curso)
               .WithMany(c => c.Turmas)
                .HasForeignKey(t => t.CursoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(e => e.IdCettpro)
                .IsUnique()
                .HasDatabaseName("IX_Turmas_IdCettpro");
        }
    }
}
