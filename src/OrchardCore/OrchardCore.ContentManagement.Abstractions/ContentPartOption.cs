using System;

namespace OrchardCore.ContentManagement
{
    public class ContentPartOption
    {
        public ContentPartOption(Type contentPartType)
        {
            if (contentPartType == null)
            {
                throw new ArgumentNullException(nameof(contentPartType));
            }

            Type = contentPartType;
        }

        public Type Type { get; }

        public Func<IServiceProvider, object> DisplayDriver { get; private set; }

        public void WithDisplayDriver(Func<IServiceProvider, object> func) => DisplayDriver = func;
    }
}
