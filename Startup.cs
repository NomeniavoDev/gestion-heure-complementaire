﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Hosting;
using AspNetCoreMvcVueJs.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using AspNetCoreMvcVueJs.Repositories;
using IdentityModel.AspNetCore.OAuth2Introspection;

namespace AspNetCoreMvcVueJs;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        var connection = Configuration.GetConnectionString("DefaultConnection");

        services.Configure<AuthConfiguration>(Configuration.GetSection("AuthConfiguration"));
        services.Configure<AuthSecretsConfiguration>(Configuration.GetSection("AuthSecretsConfiguration"));
        services.AddScoped<IDataEventRecordRepository, DataEventRecordRepository>();

        var authConfiguration = Configuration.GetSection("AuthConfiguration");
        var authSecretsConfiguration = Configuration.GetSection("AuthSecretsConfiguration");
        var stsServerIdentityUrl = authConfiguration["StsServerIdentityUrl"];

        services.AddDbContext<DataEventRecordContext>(options =>
            options.UseSqlite(connection)
        );

        services.AddAuthentication(OAuth2IntrospectionDefaults.AuthenticationScheme)
            .AddOAuth2Introspection(options =>
            {
                options.Authority = $"{authConfiguration["StsServerIdentityUrl"]}/";
                options.ClientId = "DataEventRecordsApi"; //$"{authConfiguration["StsServerIdentityUrl"]}/resources";
                options.ClientSecret = authSecretsConfiguration["ApiSecret"];
                options.NameClaimType = "email";
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("dataEventRecordsAdmin", policyAdmin =>
            {
                policyAdmin.RequireClaim("role", "dataEventRecords.admin");
            });
            options.AddPolicy("dataEventRecordsUser", policyUser =>
            {
                policyUser.RequireClaim("role", "dataEventRecords.user");
            });
            options.AddPolicy("dataEventRecords", policyUser =>
            {
                policyUser.RequireClaim("scope", "dataEventRecords");
            });
        });

        services.AddControllers()
            .AddNewtonsoftJson();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            IdentityModelEventSource.ShowPII = true;
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        //Registered before static files to always set header
        app.UseXContentTypeOptions();
        app.UseReferrerPolicy(opts => opts.NoReferrer());
        app.UseCsp(opts => opts
            .BlockAllMixedContent()
            .ScriptSources(s => s.Self())
            .ScriptSources(s => s.UnsafeEval())
            .ScriptSources(s => s.UnsafeInline())
            .StyleSources(s => s.UnsafeInline())
            .StyleSources(s => s.Self())
        );

        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        app.UseDefaultFiles();
        app.UseStaticFiles();

        //Registered after static files, to set headers for dynamic content.
        //app.UseXfo(xfo => xfo.Deny());
        app.UseRedirectValidation(t => t.AllowSameHostRedirectsToHttps(44348)); 
        app.UseXXssProtection(options => options.EnabledWithBlockMode());

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}