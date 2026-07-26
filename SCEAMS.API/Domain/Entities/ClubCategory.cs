namespace SCEAMS.Domain.Entities;

public class ClubCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Club> Clubs { get; set; } = [];
}
