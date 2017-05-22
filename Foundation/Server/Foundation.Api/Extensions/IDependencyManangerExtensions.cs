﻿using Autofac.Integration.SignalR;
using Autofac.Integration.WebApi;
using AutoMapper;
using Foundation.Api.Contracts;
using Foundation.Api.Contracts.Metadata;
using Foundation.Api.Contracts.Project;
using Foundation.Api.Implementations;
using Foundation.Api.Implementations.Project;
using Foundation.Api.Middlewares;
using Foundation.Api.Middlewares.JobScheduler;
using Foundation.Api.Middlewares.JobScheduler.Implementations;
using Foundation.Api.Middlewares.SignalR;
using Foundation.Api.Middlewares.SignalR.Contracts;
using Foundation.Api.Middlewares.SignalR.Implementations;
using Foundation.Api.Middlewares.WebApi;
using Foundation.Api.Middlewares.WebApi.ActionFilters;
using Foundation.Api.Middlewares.WebApi.Contracts;
using Foundation.Api.Middlewares.WebApi.Implementations;
using Foundation.Api.Middlewares.WebApi.OData;
using Foundation.Api.Middlewares.WebApi.OData.ActionFilters;
using Foundation.Api.Middlewares.WebApi.OData.Contracts;
using Foundation.Api.Middlewares.WebApi.OData.Implementations;
using Foundation.DataAccess.Implementations;
using Foundation.Model.Contracts;
using Hangfire;
using Hangfire.Dashboard;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.OData;

namespace Foundation.Core.Contracts
{
    public static class IDependencyManangerExtensions
    {
        public static IDependencyManager RegisterSignalRMiddlewareUsingDefaultConfiguration<TMessagesHubEvents>(this IDependencyManager dependencyManager, params Assembly[] hubsAssemblies)
            where TMessagesHubEvents : class, IMessagesHubEvents
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            hubsAssemblies = hubsAssemblies.Any() ? hubsAssemblies : new[] { Assembly.GetCallingAssembly(), typeof(MessagesHub).GetTypeInfo().Assembly };

            dependencyManager.RegisterHubs(hubsAssemblies);
            dependencyManager.Register<IMessagesHubEvents, TMessagesHubEvents>();
            dependencyManager.Register<IMessageSender, SignalRMessageSender>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IMessageContentFormatter, SignalRMessageContentFormatter>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<Microsoft.AspNet.SignalR.IDependencyResolver, AutofacDependencyResolver>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.RegisterInstance<Microsoft.AspNet.SignalR.Hubs.IAssemblyLocator>(new DefaultSignalRAssemblyLocator(hubsAssemblies));

            dependencyManager.RegisterOwinMiddleware<SignalRMiddlewareConfiguration>();

            return dependencyManager;
        }

        public static IDependencyManager RegisterSignalRMiddlewareUsingDefaultConfiguration(this IDependencyManager dependencyManager, params Assembly[] hubsAssemblies)
        {
            return RegisterSignalRMiddlewareUsingDefaultConfiguration<DefaultMessageHubEvents>(dependencyManager, hubsAssemblies);
        }

        public static IDependencyManager RegisterBackgroundJobWorkerUsingDefaultConfiguration<TJobSchedulerInMemoryBackendConfiguration>(this IDependencyManager dependencyManager)
            where TJobSchedulerInMemoryBackendConfiguration : class, IAppEvents
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IDashboardAuthorizationFilter, DefaultJobsDashboardAuthorizationFilter>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
            dependencyManager.RegisterOwinMiddleware<JobSchedulerMiddlewareConfiguration>();
            dependencyManager.RegisterAppEvents<TJobSchedulerInMemoryBackendConfiguration>();
            dependencyManager.Register<IBackgroundJobWorker, DefaultBackgroundJobWorker>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<JobActivator, AutofacJobActivator>(lifeCycle: DependencyLifeCycle.SingleInstance);


            return dependencyManager;
        }

        public static IDependencyManager RegisterDefaultWebApiConfiguration(this IDependencyManager dependencyManager, params Assembly[] controllersAssemblies)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            controllersAssemblies = (controllersAssemblies.Any() ? controllersAssemblies : new[] { Assembly.GetCallingAssembly(), typeof(FoundationEdmModelProvider).GetTypeInfo().Assembly }).Union(new[] { typeof(MetadataController).GetTypeInfo().Assembly }).ToArray();

            dependencyManager.RegisterInstance<IApiAssembliesProvider>(new DefaultApiAssembliesProvider(controllersAssemblies), overwriteExciting: false);

            dependencyManager.RegisterApiControllers(controllersAssemblies);

            dependencyManager.Register<System.Web.Http.Dispatcher.IAssembliesResolver, DefaultWebApiAssembliesResolver>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<System.Web.Http.Tracing.ITraceWriter, DefaultWebApiTraceWritter>(lifeCycle: DependencyLifeCycle.SingleInstance);

            dependencyManager.RegisterGlobalWebApiActionFiltersUsing(webApiConfig => webApiConfig.Filters.Add(new OwinActionFilterAttribute(typeof(OwinNoCacheResponseMiddleware))));
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalHostAuthenticationFilterProvider>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalDefaultExceptionHandlerActionFilterProvider>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalDefaultLogActionArgsActionFilterProvider>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<ClientCorrelationWebApiConfigurationCustomizer>();

            return dependencyManager;
        }

        public static IDependencyManager RegisterWebApiConfigurationCustomizer<TActionFilter>(this IDependencyManager dependencyManager)
    where TActionFilter : class, IWebApiConfigurationCustomizer
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IWebApiConfigurationCustomizer, TActionFilter>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterGlobalWebApiActionFiltersUsing(this IDependencyManager dependencyManager, Action<HttpConfiguration> addGlobalActionFilters)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            if (addGlobalActionFilters == null)
                throw new ArgumentNullException(nameof(addGlobalActionFilters));

            dependencyManager.RegisterInstance<IWebApiConfigurationCustomizer>(new DelegateGlobalActionFiltersProvider(addGlobalActionFilters), overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterEdmModelProvider<TEdmModelProvider>(this IDependencyManager dependencyManager)
            where TEdmModelProvider : class, IEdmModelProvider
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<IEdmModelProvider, TEdmModelProvider>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterWebApiMiddlewareUsingDefaultConfiguration(this IDependencyManager dependencyManager, string name = null)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<System.Web.Http.Dependencies.IDependencyResolver, AutofacWebApiDependencyResolver>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IWebApiOwinPipelineInjector, DefaultWebApiOwinPipelineInjector>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.RegisterOwinMiddleware<WebApiMiddlewareConfiguration>(name);

            return dependencyManager;
        }

        public static IDependencyManager RegisterSignalRConfiguration<TSignalRConfiguration>(this IDependencyManager dependencyManager)
            where TSignalRConfiguration : class, ISignalRConfiguration
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<ISignalRConfiguration, TSignalRConfiguration>(lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            return dependencyManager;
        }

        public static IDependencyManager RegisterWebApiODataMiddlewareUsingDefaultConfiguration(this IDependencyManager dependencyManager, string name = null)
        {
            if (dependencyManager == null)
                throw new ArgumentNullException(nameof(dependencyManager));

            dependencyManager.Register<System.Web.Http.Dependencies.IDependencyResolver, AutofacWebApiDependencyResolver>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IWebApiOwinPipelineInjector, DefaultWebApiODataOwinPipelineInjector>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalDefaultRequestQSStringCorrectorsApplierActionFilterProvider>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<GlobalDefaultRequestQSTimeZoneApplierActionFilterProvider>();
            dependencyManager.RegisterWebApiConfigurationCustomizer<DefaultGlobalEnableQueryActionFilterProvider>();
            dependencyManager.Register<IAutoEdmBuilder, DefaultAutoEdmBuilder>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IODataModelBuilderProvider, DefaultODataModelBuilderProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IODataContainerBuilderCustomizer, DefaultODataContainerBuilderCustomizer>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<System.Web.Http.Controllers.IHttpActionSelector, DefaultWebApiODataControllerActionSelector>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.RegisterOwinMiddleware<WebApiODataMiddlewareConfiguration>(name);

            return dependencyManager;
        }
    }
}
