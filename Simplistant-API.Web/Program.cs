using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.CookiePolicy;
using Simplistant_API.Data.System;
using Simplistant_API.Data.Users;
using Simplistant_API.Extensions;
using Simplistant_API.Repository;
using Simplistant_API.Utility;
using Simplistant_API.Utility.Interface;

namespace Simplistant_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //Default boilerplate
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            //System repositories
            builder.Services.AddTransient(_ => RepositoryFactory.Create<ConfigItem>(DatabaseSelector.System));
            builder.Services.AddTransient(_ => RepositoryFactory.Create<ExceptionLog>(DatabaseSelector.System));

            //User repositories
            builder.Services.AddTransient(_ => RepositoryFactory.Create<AuthData>(DatabaseSelector.Users));
            builder.Services.AddTransient(_ => RepositoryFactory.Create<EmailData>(DatabaseSelector.Users));
            builder.Services.AddTransient(_ => RepositoryFactory.Create<LoginData>(DatabaseSelector.Users));
            builder.Services.AddTransient(_ => RepositoryFactory.Create<RecoveryData>(DatabaseSelector.Users));

            //Data repositories

            //Misc utilities
            builder.Services.AddTransient<IEmailProvider, EmailProvider>();
            builder.Services.AddTransient<IUserAuthenticator, UserAuthenticator>();

            builder.Services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });

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
                    throw;
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

            //Https Configuration
            app.UseHttpsRedirection();
            app.UseCookiePolicy(new CookiePolicyOptions
            {
                HttpOnly = HttpOnlyPolicy.Always,
                MinimumSameSitePolicy = SameSiteMode.None,
                Secure = CookieSecurePolicy.Always
            });

            //Misc
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
