using ApiIntegracao.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiIntegracao.Data.Configurations
{
    // Data/Configurations/MatriculaConfiguration.cs
    public class MatriculaConfiguration : IEntityTypeConfiguration<Matricula>
    {
        public void Configure(EntityTypeBuilder<Matricula> builder)
        {
            builder.ToTable("Matriculas");

            builder.HasKey(e => e.Id);

            builder.HasOne(m => m.Aluno)
                .WithMany()
                .HasForeignKey(m => m.AlunoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(m => m.Turma)
                .WithMany()
                .HasForeignKey(m => m.TurmaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Índice único para evitar matrículas duplicadas
            builder.HasIndex(m => new { m.AlunoId, m.TurmaId })
                .IsUnique()
                .HasDatabaseName("IX_Matriculas_AlunoTurma");

            builder.HasIndex(e => e.IdCettpro)
                .IsUnique()
                .HasDatabaseName("IX_Matriculas_IdCettpro");
        }
    }
}
