namespace PlaywrightXUnitv3Learning.Tests.Course.Samples
{
    public record CartItem(string Name, decimal Price, int Quantity = 1);

    public class ShoppingCart
    {
        private readonly List<CartItem> _items = new();

        public IReadOnlyList<CartItem> Items => _items;

        public int Count => _items.Sum(i => i.Quantity);

        public decimal Total => _items.Sum(i => i.Price * i.Quantity);

        public void Add(CartItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (item.Price < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(item), "Price cannot be negative.");
            }
            if (item.Quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(item), "Quantity must be positive.");
            }

            int index = _items.FindIndex(i => i.Name == item.Name);
            if (index >= 0)
            {
                CartItem existing = _items[index];
                _items[index] = existing with { Quantity = existing.Quantity + item.Quantity };
            }
            else
            {
                _items.Add(item);
            }
        }

        public bool Remove(string name) => _items.RemoveAll(i => i.Name == name) > 0;

        public decimal ApplyDiscount(int percent)
        {
            if (percent is < 0 or > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(percent));
            }
            return Total - Total * percent / 100m;
        }
    }
}
