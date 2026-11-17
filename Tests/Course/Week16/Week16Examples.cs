using Microsoft.Playwright;

namespace PlaywrightXUnitv3Learning.Tests.Course.Week16
{
    public class Week16Examples : IAsyncLifetime
    {
        private const string BaseUrl = "https://www.saucedemo.com";
        private IPlaywright _playwright = null!;
        private IBrowser _browser = null!;
        private IBrowserContext _context = null!;
        private IPage _page = null!;

        public async ValueTask InitializeAsync()
        {
            _playwright = await Playwright.CreateAsync();
            _playwright.Selectors.SetTestIdAttribute("data-test");
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            _context = await _browser.NewContextAsync();
            _page = await _context.NewPageAsync();
        }

        public async ValueTask DisposeAsync()
        {
            _playwright.Selectors.SetTestIdAttribute("data-testid");
            await _context.CloseAsync();
            await _browser.CloseAsync();
            _playwright.Dispose();
        }

        private async Task LoginAsync(string user, string password = "secret_sauce")
        {
            await _page.GotoAsync(BaseUrl);
            await _page.GetByTestId("username").FillAsync(user);
            await _page.GetByTestId("password").FillAsync(password);
            await _page.GetByTestId("login-button").ClickAsync();
        }

        private async Task LoginAndWaitAsync()
        {
            await LoginAsync("standard_user");
            await Assertions.Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("inventory\\.html$"));
        }

        // ---------- Day 91: login ----------

        [Fact]
        public async Task Day91_ValidLogin()
        {
            await LoginAndWaitAsync();
            await Assertions.Expect(_page.GetByTestId("title")).ToHaveTextAsync("Products");
            await Assertions.Expect(_page.GetByTestId("inventory-item")).ToHaveCountAsync(6);
        }

        [Fact]
        public async Task Day91_WrongPassword()
        {
            await LoginAsync("standard_user", "wrong");
            await Assertions.Expect(_page.GetByTestId("error")).ToContainTextAsync("Username and password do not match");
            await Assertions.Expect(_page).Not.ToHaveURLAsync(new System.Text.RegularExpressions.Regex("inventory"));
        }

        [Fact]
        public async Task Day91_LockedOutUser()
        {
            await LoginAsync("locked_out_user");
            await Assertions.Expect(_page.GetByTestId("error")).ToContainTextAsync("locked out");
        }

        [Fact]
        public async Task Day91_EmptyFields()
        {
            await _page.GotoAsync(BaseUrl);
            await _page.GetByTestId("login-button").ClickAsync();
            await Assertions.Expect(_page.GetByTestId("error")).ToContainTextAsync("Username is required");

            await _page.GetByTestId("username").FillAsync("standard_user");
            await _page.GetByTestId("login-button").ClickAsync();
            await Assertions.Expect(_page.GetByTestId("error")).ToContainTextAsync("Password is required");
        }

        [Fact]
        public async Task Day91_Logout()
        {
            await LoginAndWaitAsync();
            await _page.GetByRole(AriaRole.Button, new() { Name = "Open Menu" }).ClickAsync();
            await _page.GetByTestId("logout-sidebar-link").ClickAsync();
            await Assertions.Expect(_page.GetByTestId("login-button")).ToBeVisibleAsync();
        }

        // ---------- Day 92: inventory ----------

        [Fact]
        public async Task Day92_SortByPriceLowToHigh()
        {
            await LoginAndWaitAsync();
            await _page.GetByTestId("product-sort-container").SelectOptionAsync("lohi");

            IReadOnlyList<string> texts = await _page.GetByTestId("inventory-item-price").AllTextContentsAsync();
            List<decimal> prices = texts.Select(t => decimal.Parse(t.TrimStart('$'), System.Globalization.CultureInfo.InvariantCulture)).ToList();

            Assert.Equal(6, prices.Count);
            Assert.Equal(prices.OrderBy(p => p).ToList(), prices);
        }

        [Fact]
        public async Task Day92_SortByNameZToA()
        {
            await LoginAndWaitAsync();
            await _page.GetByTestId("product-sort-container").SelectOptionAsync("za");

            IReadOnlyList<string> names = await _page.GetByTestId("inventory-item-name").AllTextContentsAsync();
            Assert.Equal(names.OrderByDescending(n => n, StringComparer.Ordinal).ToList(), names);
        }

        [Fact]
        public async Task Day92_AddAndRemoveFromCart()
        {
            await LoginAndWaitAsync();
            ILocator badge = _page.GetByTestId("shopping-cart-badge");

            await _page.GetByTestId("add-to-cart-sauce-labs-backpack").ClickAsync();
            await Assertions.Expect(badge).ToHaveTextAsync("1");

            await _page.GetByTestId("add-to-cart-sauce-labs-bike-light").ClickAsync();
            await Assertions.Expect(badge).ToHaveTextAsync("2");

            await _page.GetByTestId("remove-sauce-labs-backpack").ClickAsync();
            await Assertions.Expect(badge).ToHaveTextAsync("1");

            await _page.GetByTestId("remove-sauce-labs-bike-light").ClickAsync();
            await Assertions.Expect(badge).Not.ToBeVisibleAsync();
        }

        [Fact]
        public async Task Day92_AddByFilteringTheItemCard()
        {
            await LoginAndWaitAsync();
            await _page.GetByTestId("inventory-item")
                .Filter(new() { HasText = "Sauce Labs Backpack" })
                .GetByRole(AriaRole.Button, new() { Name = "Add to cart" })
                .ClickAsync();
            await _page.GetByTestId("shopping-cart-link").ClickAsync();

            await Assertions.Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("cart\\.html$"));
            await Assertions.Expect(_page.GetByTestId("cart-list")).ToBeVisibleAsync();
            await Assertions.Expect(_page.GetByTestId("inventory-item-name")).ToHaveTextAsync("Sauce Labs Backpack");
        }

        // ---------- Day 93: checkout ----------

        private async Task GoToCheckoutStepOneAsync()
        {
            await LoginAndWaitAsync();
            await _page.GetByTestId("add-to-cart-sauce-labs-backpack").ClickAsync();
            await _page.GetByTestId("shopping-cart-link").ClickAsync();
            await _page.GetByTestId("checkout").ClickAsync();
            await Assertions.Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("checkout-step-one\\.html$"));
        }

        [Fact]
        public async Task Day93_CompleteCheckout()
        {
            await GoToCheckoutStepOneAsync();
            await _page.GetByTestId("firstName").FillAsync("Ada");
            await _page.GetByTestId("lastName").FillAsync("Lovelace");
            await _page.GetByTestId("postalCode").FillAsync("12345");
            await _page.GetByTestId("continue").ClickAsync();

            await Assertions.Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("checkout-step-two\\.html$"));
            await Assertions.Expect(_page.GetByTestId("inventory-item-name")).ToHaveTextAsync("Sauce Labs Backpack");
            await Assertions.Expect(_page.GetByTestId("subtotal-label")).ToContainTextAsync("29.99");

            await _page.GetByTestId("finish").ClickAsync();
            await Assertions.Expect(_page.GetByTestId("complete-header")).ToHaveTextAsync("Thank you for your order!");
            await _page.GetByTestId("back-to-products").ClickAsync();
            await Assertions.Expect(_page.GetByTestId("title")).ToHaveTextAsync("Products");
        }

        [Fact]
        public async Task Day93_TotalsAddUp()
        {
            await LoginAndWaitAsync();
            await _page.GetByTestId("add-to-cart-sauce-labs-backpack").ClickAsync();
            await _page.GetByTestId("add-to-cart-sauce-labs-bike-light").ClickAsync();
            await _page.GetByTestId("shopping-cart-link").ClickAsync();
            await _page.GetByTestId("checkout").ClickAsync();
            await _page.GetByTestId("firstName").FillAsync("Ada");
            await _page.GetByTestId("lastName").FillAsync("Lovelace");
            await _page.GetByTestId("postalCode").FillAsync("12345");
            await _page.GetByTestId("continue").ClickAsync();

            static decimal Money(string text) =>
                decimal.Parse(System.Text.RegularExpressions.Regex.Match(text, @"\d+\.\d+").Value, System.Globalization.CultureInfo.InvariantCulture);

            decimal subtotal = Money((await _page.GetByTestId("subtotal-label").TextContentAsync())!);
            decimal tax = Money((await _page.GetByTestId("tax-label").TextContentAsync())!);
            decimal total = Money((await _page.GetByTestId("total-label").TextContentAsync())!);

            Assert.Equal(39.98m, subtotal);
            Assert.Equal(subtotal + tax, total);
        }

        // ---------- Day 94: negative scenarios ----------

        [Fact]
        public async Task Day94_CheckoutRequiresFirstName()
        {
            await GoToCheckoutStepOneAsync();
            await _page.GetByTestId("continue").ClickAsync();
            await Assertions.Expect(_page.GetByTestId("error")).ToContainTextAsync("First Name is required");

            await _page.GetByTestId("firstName").FillAsync("Ada");
            await _page.GetByTestId("continue").ClickAsync();
            await Assertions.Expect(_page.GetByTestId("error")).ToContainTextAsync("Last Name is required");

            await _page.GetByTestId("lastName").FillAsync("Lovelace");
            await _page.GetByTestId("continue").ClickAsync();
            await Assertions.Expect(_page.GetByTestId("error")).ToContainTextAsync("Postal Code is required");
        }

        [Fact]
        public async Task Day94_CannotOpenInventoryWithoutLogin()
        {
            await _page.GotoAsync(BaseUrl + "/inventory.html");
            await Assertions.Expect(_page.GetByTestId("error")).ToContainTextAsync("only access '/inventory.html' when you are logged in");
        }

        [Fact]
        public async Task Day94_ErrorMessageCanBeDismissed()
        {
            await LoginAsync("standard_user", "wrong");
            await Assertions.Expect(_page.GetByTestId("error")).ToBeVisibleAsync();
            await _page.GetByTestId("error-button").ClickAsync();
            await Assertions.Expect(_page.GetByTestId("error")).Not.ToBeVisibleAsync();
        }
    }
}
