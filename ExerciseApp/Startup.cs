using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using ExerciseApp.Data;
using ExerciseApp.Service;
using System.Text.Json.Serialization;

namespace ExerciseApp
{
    public class Startup(IConfiguration configuration)
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(option =>
                {
                    option.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); 
                    option.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });
            services.AddDbContext<QuoteDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("Quotes")));
            services.AddScoped<QuoteService>();
            services.AddScoped<IQuoteRepository, SqliteQuoteRepository>();
            services.AddTransient<IQuoteStrategy, FullyComprehensiveQuoteStrategy>();
            services.AddTransient<IQuoteStrategy, ThirdPartyFireAndTheftQuoteStrategy>();
            services.AddTransient<IQuoteStrategy, ThirdPartyOnlyQuoteStrategy>();
            services.AddCors();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<QuoteDbContext>();
                dbContext.Database.EnsureCreated();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseCors(builder =>
            {
                builder.WithOrigins("*");
                builder.WithMethods("GET", "PUT", "POST", "DELETE", "HEAD");
                builder.WithHeaders("Origin", "X-Requested-With", "content-type", "Accept");
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
