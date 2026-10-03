/// <summary>
/// Optional: implement on any component of a purchasable prefab (cash register, shelf, ...)
/// to be notified when the building is spawned from a BuildSlot.
/// </summary>
public interface IBuildable
{
    void OnBuilt(BuildSlot slot);
}
