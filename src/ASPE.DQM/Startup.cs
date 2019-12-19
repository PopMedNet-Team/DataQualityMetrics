using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;

namespace ASPE.DQM
{
    public class Startup
    {
        public const string CookieScheme = ".ASPE-DQM";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ASPE.DQM.Identity.IdentityContext>(options => {
                options.UseSqlServer(
                    Configuration.GetConnectionString("IdentityContextConnection"),
                    x => x.MigrationsAssembly("ASPE.DQM.Data").EnableRetryOnFailure());
            });
            services.AddDbContext<ASPE.DQM.Model.ModelDataContext>(options => {
                options.UseSqlServer(
                    Configuration.GetConnectionString("IdentityContextConnection"),
                    x => x.MigrationsAssembly("ASPE.DQM.Data").EnableRetryOnFailure());
            });
            services.AddDbContext<DQM.Sync.SyncDataContext>(options => {
                options.UseSqlServer(
                    Configuration.GetConnectionString("IdentityContextConnection"),
                    x => x.MigrationsAssembly("ASPE.DQM.Data").EnableRetryOnFailure()
                    );
            });

            //by adding the password hasher before the default identity call it will replace the default implementation.
            services.AddScoped<IPasswordHasher<Identity.IdentityUser>, Identity.PasswordHasher>();

            services.AddIdentity<Identity.IdentityUser, IdentityRole<Guid>>()
                .AddEntityFrameworkStores<Identity.IdentityContext>()
                .AddDefaultTokenProviders();

            services.Configure<IdentityOptions>(o =>
            {
                o.User.AllowedUserNameCharacters = o.User.AllowedUserNameCharacters + " ";
            });

            services.ConfigureApplicationCookie(options => {
                options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
                options.SlidingExpiration = true;
                options.LoginPath = "/Home/Index";
                options.LogoutPath = "/Home/Index";
                options.AccessDeniedPath = "/NotAuthorized";
            });

            services.AddControllersWithViews().AddRazorRuntimeCompilation().AddNewtonsoftJson();

            services.AddAuthorization((auth) => {
                auth.AddPolicy(Identity.Claims.SystemAdministrator_Key, policy => policy.RequireClaim(Identity.Claims.SystemAdministrator_Key));
                auth.AddPolicy(Identity.Claims.AuthorMetric_Key, policy => policy.RequireClaim(Identity.Claims.AuthorMetric_Key));
                auth.AddPolicy(Identity.Claims.SubmitMeasure_Key, policy => policy.RequireClaim(Identity.Claims.SubmitMeasure_Key));
            });

            services.AddTransient(Type.GetType(Configuration["Files:Type"]));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
