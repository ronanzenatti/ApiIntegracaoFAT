namespace ApiIntegracao.DTOs
{
    public class CursoResponseDto
    {
        public Guid IdCurso { get; set; }
        public string NomeCurso { get; set; } = string.Empty;
        public int CargaHoraria { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public Guid? ModalidadeId { get; set; }
        public bool Ativo { get; set; }
        public string? CodigoPortalFat { get; set; }
    }
}
