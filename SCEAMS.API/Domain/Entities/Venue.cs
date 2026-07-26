namespace SCEAMS.Domain.Entities;

public class Venue
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool IsUnderMaintenance { get; set; }

    public ICollection<Event> Events { get; set; } = [];
}
