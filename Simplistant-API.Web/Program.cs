using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.CookiePolicy;
using Simplistant_API.Utility;
using Simplistant_API.Utility.Interface;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;
using Simplistant_API.DTO;
using Simplistant_API.Models.System;
using Simplistant_API.Models.Users;
using LiteDB;
using Simplistant_API.Models.Data;
using Simplistant_API.Domain.Markdown;
using Simplistant_API.Domain.Stemming;
using Simplistant_API.Domain.NotesRepository;
using Simplistant_API.Models;
using Simplistant_API.Models.Repository;
using Simplistant_API.Domain.Auth;
using Simplistant_API.Domain.Extensions;

namespace Simplistant_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //Misc boilerplate
            builder.Services.AddControllers();
            builder.Services.AddHttpContextAccessor();
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
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("frontend", builder =>
                {
                    //builder.WithOrigins("https://simplistant.azurewebsites.net")
                    //    .AllowCredentials()
                    //    .AllowAnyHeader()
                    //    .AllowAnyMethod();
                    builder.WithOrigins("http://localhost:5173")
                        .AllowCredentials()
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            //Repository configuration
            //(Technically not part of the app config process but not a better place to put it.)
            BsonMapper.Global.RegisterType
            (
                serialize: collection => collection.Serialize(),
                deserialize: bson => new MatchDataCollection(bson)
            );

            //System repositories
            builder.Services.AddTransient(_ => Repositories.System<ConfigItem>());
            builder.Services.AddTransient(_ => Repositories.System<ExceptionLog>());

            //User repositories
            builder.Services.AddTransient(_ => Repositories.Users<AuthData>());
            builder.Services.AddTransient(_ => Repositories.Users<EmailData>());
            builder.Services.AddTransient(_ => Repositories.Users<LoginData>());
            builder.Services.AddTransient(_ => Repositories.Users<RecoveryData>());

            //Data repositories
            static IRepository<T> ResolveDataRepository<T>(IServiceProvider sp) where T : DataItem
            {
                var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
                var userId = httpContextAccessor.HttpContext.GetCurrentUserId();
                return Repositories.Data<T>(userId);
            };
            builder.Services.AddTransient(ResolveDataRepository<IndexData>);
            builder.Services.AddTransient(ResolveDataRepository<NoteData>);

            //Domain logic classes
            builder.Services.AddTransient<IEmailProvider, EmailProvider>();
            builder.Services.AddTransient<IUserAuthenticator, UserAuthenticator>();
            builder.Services.AddTransient<IMarkdownTokenizer, MarkdownTokenizer>();
            builder.Services.AddTransient<IStemmer, Stemmer>();
            builder.Services.AddTransient<INotesRepository, NotesRepository>();

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
                            status = ResponseStatus.RequiresAuth
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
                var authenticated = userAuthenticator.Authenticate(context);
                await next();
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

            app.UseCors("frontend");

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
