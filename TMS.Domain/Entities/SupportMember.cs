namespace TMS.Domain.Entities;

public sealed class SupportMember
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }

    private SupportMember()
    {
    }

    public SupportMember(Guid id, string name, bool isActive = true)
    {
        Id = id;
        Name = name;
        IsActive = isActive;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
