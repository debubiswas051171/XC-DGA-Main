using Sitecore.Abstractions;
using Sitecore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGAIdentityLogin
{
    public static class SettingsExtensions
    {
        public static Is4Settings GetIs4Settings(this BaseSettings settings)
        {
            Assert.ArgumentNotNull(settings, "settings");
            return new Is4Settings(settings);
        }
    }

}
