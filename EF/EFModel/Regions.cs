using System.Collections.Generic;

namespace EF.EFModel
{
    public partial class Regions
    {
        public Regions()
        {
            Territories = new HashSet<Territories>();
        }

        public int RegionId { get; set; }
        public string RegionDescription { get; set; }

        public ICollection<Territories> Territories { get; set; }
    }
}
