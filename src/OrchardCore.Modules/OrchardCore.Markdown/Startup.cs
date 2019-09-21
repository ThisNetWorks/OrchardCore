using Fluid;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Handlers;
using OrchardCore.ContentTypes.Editors;
using OrchardCore.Data.Migration;
using OrchardCore.Indexing;
using OrchardCore.Liquid;
using OrchardCore.Markdown.Drivers;
using OrchardCore.Markdown.Fields;
using OrchardCore.Markdown.Filters;
using OrchardCore.Markdown.Handlers;
using OrchardCore.Markdown.Indexing;
using OrchardCore.Markdown.Models;
using OrchardCore.Markdown.Settings;
using OrchardCore.Markdown.ViewModels;
using OrchardCore.Modules;

namespace OrchardCore.Markdown
{
    public class Startup : StartupBase
    {
        static Startup()
        {
            TemplateContext.GlobalMemberAccessStrategy.Register<MarkdownBodyPartViewModel>();
            TemplateContext.GlobalMemberAccessStrategy.Register<MarkdownFieldViewModel>();
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            // Markdown Part
            //services.AddContentPart<MarkdownBodyPart>();

            // This should register it as well, which is why we need the Builder class.
            services.AddContentPart<MarkdownBodyPart>(o =>
            {
                o.WithDisplayDriver<MarkdownBodyPartDisplay>();
            });

            // not required, but still need it for edit etc.
            services.AddScoped<MarkdownBodyPartDisplay>();
            services.AddScoped<IContentPartDisplayDriver>(sp => sp.GetRequiredService<MarkdownBodyPartDisplay>());
            //services.AddScoped<IContentPartDisplayDriver, MarkdownBodyPartDisplay>();

            services.AddScoped<IContentTypePartDefinitionDisplayDriver, MarkdownBodyPartSettingsDisplayDriver>();
            services.AddScoped<IDataMigration, Migrations>();
            services.AddScoped<IContentPartIndexHandler, MarkdownBodyPartIndexHandler>();
            services.AddScoped<IContentPartHandler, MarkdownBodyPartHandler>();

            // Markdown Field
            services.AddContentField<MarkdownField>();
            services.AddScoped<IContentFieldDisplayDriver, MarkdownFieldDisplayDriver>();
            services.AddScoped<IContentPartFieldDefinitionDisplayDriver, MarkdownFieldSettingsDriver>();
            services.AddScoped<IContentFieldIndexHandler, MarkdownFieldIndexHandler>();

            services.AddLiquidFilter<Markdownify>("markdownify");
        }
    }
}
