using RpgInventoryShop.Domain;

namespace RpgInventoryShop.Application
{
    public interface IItemCatalog
    {
        bool TryGet(string itemId, out ItemDefinition item);
    }
}
