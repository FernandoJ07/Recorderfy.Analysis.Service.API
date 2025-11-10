using System;

namespace Recorderfy.Analisys.Service.Model.DTOs
{
    public class ImagenMetadataDto
    {
        public Guid ImagenId { get; set; }
        public string DescripcionReal { get; set; }
        public string MetadataJson { get; set; }
        public string UrlImagen { get; set; }
        public DateTime FechaSubida { get; set; }
    }
}
