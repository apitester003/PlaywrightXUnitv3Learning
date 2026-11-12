using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlaywrightXUnitv3Learning.Core
{
    public abstract class BasePageSimple : IBasePage
    {
        protected IPage Page { get; }
        protected string PageUrl { get; set; } = string.Empty;

        protected BasePageSimple(IPage page)
        {
            Page = page;
        }

        public virtual async Task GoTo(string? baseUrl = null)
        {
            var url = baseUrl != null ? $"{baseUrl}/{PageUrl}" : PageUrl;
            await Page.GotoAsync(url);
        }
    }
}
