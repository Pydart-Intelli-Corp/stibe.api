// Models/Entities/PartnersEntity/ServicesEntity/ServiceOfferItem.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace stibe.api.Models.Entities.PartnersEntity.ServicesEntity
{
    public class ServiceOfferItem : BaseEntity
    {
        public int ServiceId { get; set; }

        public int OfferID { get; set; }

        [ForeignKey("ServiceId")]
        public virtual Service Service { get; set; } = null!;

        [ForeignKey("OfferID")]
        public virtual ServiceOffer Offer { get; set; } = null!;
    }
}
