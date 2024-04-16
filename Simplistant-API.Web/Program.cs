using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.CookiePolicy;
using Simplistant_API.Extensions;
using Simplistant_API.Repository;
using Simplistant_API.Utility;
using Simplistant_API.Utility.Interface;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;
using Simplistant_API.DTO;
using Simplistant_API.Models.System;
using Simplistant_API.Models.Users;

namespace Simplistant_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //Misc boilerplate
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                // using System.Reflection;
                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            });
            builder.Services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });
            builder.Services.AddCors();

            //System repositories
            builder.Services.AddTransient(_ => RepositoryFactory.Create<ConfigItem>(DatabaseSelector.System));
            builder.Services.AddTransient(_ => RepositoryFactory.Create<ExceptionLog>(DatabaseSelector.System));

            //User repositories
            builder.Services.AddTransient(_ => RepositoryFactory.Create<AuthData>(DatabaseSelector.Users));
            builder.Services.AddTransient(_ => RepositoryFactory.Create<EmailData>(DatabaseSelector.Users));
            builder.Services.AddTransient(_ => RepositoryFactory.Create<LoginData>(DatabaseSelector.Users));
            builder.Services.AddTransient(_ => RepositoryFactory.Create<RecoveryData>(DatabaseSelector.Users));

            //Data repositories

            //Utilities
            builder.Services.AddTransient<IEmailProvider, EmailProvider>();
            builder.Services.AddTransient<IUserAuthenticator, UserAuthenticator>();

            //-----------
            var app = builder.Build();

            //Exception logging
            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (Exception e)
                {
                    var detailedMessage = e.DetailedExceptionMessage();
                    var exceptionLogRepository = app.Services.GetService<IRepository<ExceptionLog>>();
                    var exceptionLog = new ExceptionLog
                    {
                        ExceptionType = e.GetType().ToString(),
                        Message = detailedMessage,
                        TimeStamp = DateTime.UtcNow
                    };
                    exceptionLogRepository?.Upsert(exceptionLog);

                    if (e is InvalidOperationException && e.Message.Contains("No authenticationScheme was specified"))
                    {
                        context.Response.Clear();
                        context.Response.ContentType = "application/json";
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        var message = new MessageResponse
                        {
                            Status = ResponseStatus.RequiresAuth
                        };
                        var json = JsonConvert.SerializeObject(message);
                        await context.Response.WriteAsync(json);
                    }
                    else throw;
                }
            });

            //User auth
            app.Use(async (context, next) =>
            {
                var userAuthenticator = app.Services.GetService<IUserAuthenticator>();
                await userAuthenticator.Authenticate(context, next);
            });

            //todo: DISABLE THIS FOR PRODUCTION
            //Swagger
            //if (app.Environment.IsDevelopment())
            //{
                app.UseSwagger();
                app.UseSwaggerUI();
            //}

            //Http Configuration
            app.UseHttpsRedirection();
            app.UseCookiePolicy(new CookiePolicyOptions
            {
                HttpOnly = HttpOnlyPolicy.Always,
                MinimumSameSitePolicy = SameSiteMode.None,
                Secure = CookieSecurePolicy.Always
            });

            app.UseCors(options => options.WithOrigins("https://simplistant.azurewebsites.net")
                .AllowAnyHeader()
                .AllowCredentials()
                .AllowAnyMethod());

            //Misc boilerplate
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller}/{action}/{data?}");
            });

            app.Run();
        }
    }
}
