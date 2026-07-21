using System.ComponentModel.DataAnnotations;

namespace AccessWatch.Models
{
    public class Facility
    {
        [Key]
        public int FacilityId { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; }

        [Required, StringLength(300)]
        public string Address { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }
    }
}