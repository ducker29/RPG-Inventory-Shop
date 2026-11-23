namespace RpgInventoryShop.Application
{
    public interface ISaveStore
    {
        SaveStatus Save();
        SaveStatus Load();
    }
}
