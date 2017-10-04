﻿using Autofac;
using Autofac.Integration.WebApi;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs.Internals;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Sample.AzureTable.Controllers;
using Microsoft.Bot.Sample.AzureTable.Dialogs;
using Microsoft.WindowsAzure.Storage;


namespace Microsoft.Bot.Sample.AzureTable
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            Conversation.UpdateContainer(
                builder =>
                {
                    // register the Bot Builder module
                    builder.RegisterModule(new DialogModule());
                    // register the alarm dependencies
                    builder.RegisterModule(new AzureModule(Assembly.GetExecutingAssembly()));

                    builder
                        .RegisterInstance(new EchoDialog())
                        .As<IDialog<object>>();

                    var store = new TableBotDataStore(CloudStorageAccount.DevelopmentStorageAccount);
                    builder.Register(c => store)
                        .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                        .AsSelf()
                        .SingleInstance();


                    builder.Register(c => new CachingBotDataStore(store,
                                                                    CachingBotDataStoreConsistencyPolicy.ETagBasedConsistency))
                        .As<IBotDataStore<BotData>>()
                        .AsSelf()
                        .InstancePerLifetimeScope();

                    // Get your HttpConfiguration.
                    var config = GlobalConfiguration.Configuration;

                    // Register your Web API controllers.
                    builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

                    // OPTIONAL: Register the Autofac filter provider.
                    builder.RegisterWebApiFilterProvider(config);

                    config.DependencyResolver = new AutofacWebApiDependencyResolver(Conversation.Container);
                });   

            // WebApiConfig stuff
            GlobalConfiguration.Configure(config =>
            {
                config.MapHttpAttributeRoutes();

                config.Routes.MapHttpRoute(
                    name: "DefaultApi",
                    routeTemplate: "api/{controller}/{id}",
                    defaults: new { id = RouteParameter.Optional }
                );
            });
        }

        public static ILifetimeScope FindContainer()
        {
            var config = GlobalConfiguration.Configuration;
            var resolver = (AutofacWebApiDependencyResolver)config.DependencyResolver;
            return resolver.Container;
        }
    }
}