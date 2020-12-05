using AutoMapper;
using System.Net;
using System.Text;
using Microsoft.OpenApi.Models;
using QueenOfDreamer.API.Repos;
using Microsoft.AspNetCore.Http;
using QueenOfDreamer.API.Context;
using QueenOfDreamer.API.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using QueenOfDreamer.API.Interfaces.Repos;
using QueenOfDreamer.API.Const;
using System.Collections.Generic;
using Newtonsoft.Json;
using QueenOfDreamer.API.Interfaces.Services;
using QueenOfDreamer.API.Services;
using System.IO;
using log4net;
using System.Reflection;
using log4net.Config;
using QueenOfDreamer.Repos;

namespace QueenOfDreamer
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;
        }

        public IConfiguration Configuration { get; }

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {            
                var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
                XmlConfigurator.Configure(logRepository, new FileInfo(Configuration.GetSection("appSettings:log4netFile").Value));

                QueenOfDreamerConst.loadConfigData();

                services.AddDbContext<QueenOfDreamerContext>(x => x.UseSqlServer
                    (QueenOfDreamerConst.DB_CONNECTION));

                services.AddControllers().AddNewtonsoftJson(opt => {
                    opt.SerializerSettings.ReferenceLoopHandling =
                        Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                    opt.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                });

                services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo { Title = "QueenOfDreamer API", Version = "v1",Description="Swagger for QueenOfDreamer system authorized by Myanmar High Society" });
                    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer"
                    });
                    options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            new List<string>()
                        }
                    });
                });

                services.AddCors();
        
                services.AddAutoMapper(typeof(Startup));
                services.AddScoped<IMiscellaneousRepository, MiscellaneousRepository>();
                services.AddScoped<IProductRepository, ProductRepository>();
                services.AddScoped<IOrderRepository, OrderRepository>();
                services.AddScoped<IQueenOfDreamerServices, QueenOfDreamerServices>();
                services.AddScoped<IPaymentGatewayServices, PaymentGateWayServices>();
                services.AddScoped<IUserServices, UserServices>();
                services.AddScoped<IDeliveryService, DeliveryService>();
                services.AddScoped<IMemberPointServices, MemberPointServices>();
                services.AddScoped<IMemberPointRepository, MemberPointRepository>();
                services.AddScoped<IReportRepository, ReportRepository>();
                services.AddScoped<IFacebookRepository, FacebookRepository>();
                
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options => {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateLifetime =QueenOfDreamerConst.TOKEN_VALIDATELIFETIME,
                            ValidateIssuerSigningKey =QueenOfDreamerConst.TOKEN_VALIDATEISSUERSIGNINGKEY,
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII
                                .GetBytes(QueenOfDreamerConst.TOKEN_SECRET)),
                            ValidateIssuer =QueenOfDreamerConst.TOKEN_VALIDATEISSUER,
                            ValidateAudience =QueenOfDreamerConst.TOKEN_VALIDATEAUDIENCE,
                            ValidIssuer =QueenOfDreamerConst.TOKEN_ISSUER,
                        };
                    });

                services.AddScoped<ActionActivity>();
                services.AddScoped<ActionActivityLog>();

                services.AddControllers();
                services.AddHttpContextAccessor();
                
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {           
            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("v1/swagger.json", "API");
                c.RoutePrefix = "swagger";                
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(builder => {
                    builder.Run(async context => {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                        var error = context.Features.Get<IExceptionHandlerFeature>();
                        if (error != null)
                        {
                            context.Response.AddApplicationError(error.Error.Message);
                            await context.Response.WriteAsync(error.Error.Message);
                        }
                    });
                });
            }

            app.UseRouting();

            app.UseStaticFiles();

            app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
