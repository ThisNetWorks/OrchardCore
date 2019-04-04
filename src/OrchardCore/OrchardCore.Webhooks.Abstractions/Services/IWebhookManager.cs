using System;
using System.Collections.Generic;
using System.Text;

namespace OrchardCore.Webhooks.Services
{
    public interface IWebhookManager
    {        //so what we want to achieve is to be able to call all providers
        //like this

        //describeFor("slack"
        //describeFor("sitemaps"

        //notifyFor("slack" with templateContext
        //notifyFor("sitemaps (put url in templateContext)

        //so what it also needs is to be able to call ListDescriptions
        //so that a slack configurer can have a list of things, like
        //sitemaps
        //workflowslackactivity
        //workflowemailactivity
        //adminnotifier
        //that the slack provider can use to configure settings for each one

        //then when you say notify slack for sitemaps
        //the slack provider can look at it's settings
        //and see if it has a configuration for sitemaps
        //and if it does proceed and notify

        //so we have a IWebhookProvider
        //it has a WebhookDescribeContext
        //it has a dictionary of WebhookContext s
        //which have a WebhookDescriptor (maybe not necesary in this case) as that provides the delegate/func and we probably don't need one of those

        //so what we really need are two things
        //an IWebhookProvider which slack,googleping, bingping will implement

        //and an IWebhookDesriber which sitemaps (and workflownotifyactivity or adminnotifyactivity) will implement to describe themselves
        //should really only need to provider technical name and localized displayname.

        //so that an implementation of SlackWebhookProvider can go and see if is configured to support sitemaps, or workflow etc
        //return if it isn't and do something useful if it is

        //so when you call notify you need to find the describer you want (is that via a static property name, or do you inject
        //your SitemapWebhookDescriber in say the pingsitemapcontroller?)
        //get it's describeContext and send that in to the notifier? or do you just call SitemapWebhookDescriber (maybe it's SitemapNotifier)
        //SitemapWebhookDescriber.NotifyAsync()
        //so the WebhookManager might only be used for descriptions? (a coordinator could manage the list of providers... to make life easier for SitemapNotifier)
        //maybe you register the descriptionContext so it can be localised?
        //and actually it's then SitemapNotifier which is allowed to take the list of IWebhookProviders and manage them because it knows
        //about it's own description context - it injected it?

        //so in this case we have
        //registered SitemapWebhookDescribeContext : WebhookDescribeContext
        //registered SitemapWebhook : IWebhook (inherits from WebhookBase)
        //registered GooglePingWebhookProvider (always limited to SitemapWebhookDescribeContext)
        //registed BingPingWebhookProvider (always limited to SitemapWebhookDescribeContext)
        //registered WebhookManager (provides descriptions to UI for configuring)
        //registered SlackWebhookProvider (configurable for sitemaps and other things by asking WebhookManager what it can Describe)
        //registered WebhookCoordinator (that has the list of Providers, and run NotifyAsync for SitemapWebhook)

        //registered SlackWorkflowWebhookDescribeContext (maybe it has a list of describecontext?)

        //so for workflows you could have a generic WebhookActivity - that would also need to list the IWebhookProviders and allow selecting / activating them
        //or you could just roll a SlackWebhookActivity - that would be simpler and provide better configurability for slack specifically (the template etc)
        //and SlackWebhookActivity would just be a wrapper for SlackWebhook
        //a warpper would probably still need to provide a list of channels or some possible descriptions to the Slack Settings. or maybe
        //it just overrides the slack channel setting that the slack settings has in it's backend
        //so you still get a list of, sitemaps, workflow, admin but it could be defaultchannel (and just enabled/disabled for workflows?)

    }
}
