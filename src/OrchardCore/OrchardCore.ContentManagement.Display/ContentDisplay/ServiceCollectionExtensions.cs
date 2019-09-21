using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace OrchardCore.ContentManagement.Display.ContentDisplay
{
    public static class ServiceCollectionExtensions
    {
        public static ContentPartOption WithDisplayDriver<TContentPartDisplayDriver>(this ContentPartOption contentPartOption)
            where TContentPartDisplayDriver : class, IContentPartDisplayDriver
        {
            contentPartOption.WithDisplayDriver(sp => sp.GetRequiredService<TContentPartDisplayDriver>());
            return contentPartOption;
        }
    }
}
