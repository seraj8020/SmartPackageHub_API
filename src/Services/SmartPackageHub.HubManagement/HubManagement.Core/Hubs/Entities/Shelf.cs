using HubManagement.Core.Hubs.ValueObjects;
using SmartPackageHub.Domains.Abstractions.Entities;
using System.ComponentModel.DataAnnotations;

namespace HubManagement.Core.Hubs.Entities
{
    public sealed class Shelf : EntityBase
    {
        public Guid SocietyId { get; private set; }

        [Required]
        public string Code { get; private set; } = null!; //shelf code unique within a society
        public string TowerOrWing { get; private set; } = null!;
        public bool IsColdStorage { get; private set; }
        public int Capacity { get; private set; }
        public ShelfLocation? Location { get; private set; } // Holds the Value Object

        private Shelf() { }
        private Shelf(Guid societyId, string code, string towerWing, bool isColdStorage, int capacity, ShelfLocation location)
        {
            this.SocietyId = societyId;
            this.Code = code;
            this.TowerOrWing = towerWing;
            this.IsColdStorage = isColdStorage;
            this.Capacity = capacity;
            this.Location = location;

        }
        public static Shelf Create(Guid societyId, string code, string towerWing, bool isColdStorage, int capacity, ShelfLocation location)
        {
            return new Shelf(societyId, code, towerWing, isColdStorage, capacity, location);
        }
    }
}
