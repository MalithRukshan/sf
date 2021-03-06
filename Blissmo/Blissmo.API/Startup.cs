﻿using System.Fabric;
using Blissmo.API.Handlers;
using Blissmo.API.Mapper;
using Blissmo.Helpers.MailProvider;
using Blissmo.Helpers.MessageBrokerProvider;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Blissmo.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            //var endpoint = Configuration.GetSection("MessageBroker:EndPoint");
            //services.Configure<MessageBroker>(Configuration.GetSection("MessageBroker"));
            services.AddSingleton<IEventHandler, Handlers.EventHandler>();
            services.AddSingleton<IEmailProvider, EmailProvider>();
            services.AddTransient<IMessageBroker, RabbitMQ>(); //ServiceBusMessageBroker

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            NotificationEventsHandler.IEmailProvider = app.ApplicationServices.GetService<IEmailProvider>();
            NotificationEventsHandler.ServiceContext = app.ApplicationServices.GetService<StatelessServiceContext>();

            AutoMapperConfiguration.Configure();
            app.UseMvc();

            app.UseServiceBusListener();
        }
    }
}
