using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlaywrightXUnitv3Learning.Core
{
    public abstract class BaseTest : IAsyncLifetime
    {
        protected IPage Page { get; set; } = null;
        protected IBrowser Browser { get; set; } = null;
        protected IPlaywright Playwright { get; set; } = null;

        public virtual async ValueTask InitializeAsync()
        {
            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false
            });
            IBrowserContext context = await Browser.NewContextAsync();
            Page = await context.NewPageAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await Page.CloseAsync();
            await Browser.CloseAsync();
            Playwright.Dispose();
        }
    }

        
}
