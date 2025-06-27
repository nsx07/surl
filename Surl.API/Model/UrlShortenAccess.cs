using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata;
using System.Runtime.Serialization;

namespace Surl.API.Model
{
    public class UrlShortenAccess
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        [ForeignKey(nameof(UrlShorten))]
        public Guid UrlShortenId { get; set; }
        public DateTime AccessedAt { get; set; }        
        public string? IpAddress { get; set; }
        public string HeadersRaw { get; set; } = string.Empty;

        [Required]
        [DeleteBehavior(DeleteBehavior.Cascade)]
        public UrlShorten UrlShorten { get; set; } = null!;
    }
}
