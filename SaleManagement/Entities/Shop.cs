using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaleManagement.Entities;

[Table("Shops")]
public class Shop
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public Guid UserId { get; set; }
    public virtual User? User { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int PreparationTime { get; set; }
    public virtual ICollection<Item> Items { get; set; }
    
    public Shop()
    {
        Items = new HashSet<Item>();
    }
}