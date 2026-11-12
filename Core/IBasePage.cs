using System;
using System.Collections.Generic;
using System.Text;

namespace PlaywrightXUnitv3Learning.Core
{
    public interface IBasePage
    {
        Task GoTo(string? baseUrl = null);
    }
}
