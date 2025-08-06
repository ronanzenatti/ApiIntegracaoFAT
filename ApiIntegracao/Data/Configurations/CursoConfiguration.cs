// Data/Configurations/CursoConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApiIntegracao.Models;

namespace ApiIntegracao.Data.Configurations
{
    public class CursoConfiguration : IEntityTypeConfiguration<Curso>
    {
        public void Configure(EntityTypeBuilder<Curso> builder)
        {
            builder.ToTable("Cursos");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.NomeCurso)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(e => e.CargaHoraria)
                .HasMaxLength(50);

            builder.Property(e => e.Descricao)
                .HasMaxLength(500);

            builder.Property(e => e.IdPortalFat)
                .HasMaxLength(50);

            builder.HasIndex(e => e.IdCettpro)
                .IsUnique()
                .HasDatabaseName("IX_Cursos_IdCettpro");

            builder.HasIndex(e => e.IdPortalFat)
                .HasDatabaseName("IX_Cursos_CodigoPortalFat");
        }
    }
}
