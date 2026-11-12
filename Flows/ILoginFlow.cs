using System;
using System.Collections.Generic;
using System.Text;

namespace PlaywrightXUnitv3Learning.Flows
{
    public interface ILoginFlow
    {
        Task LoginWithCredentials(string username, string password);
    }
}
