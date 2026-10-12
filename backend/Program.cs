
namespace GDELT_Lab
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Allow requests from the Angular development server.
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AngularFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:4200")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // Apply the Angular CORS policy.
            app.UseCors("AngularFrontend");

            app.UseAuthorization();

            app.MapGet("/api/health", () => Results.Ok(new
            {
                status = "OK",
                message = "GDELT-Lab API is running"
            }));

            app.MapControllers();

            app.Run();
        }
    }
}
