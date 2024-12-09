var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// builder.Services.AddCors(o =>
//     o.AddPolicy("MyPolicy", builder =>
//         {
//             builder.AllowAnyOrigin()
//                     .AllowAnyMethod()
//                     .AllowAnyHeader();
//         })
// );

var redisConnection = builder.Configuration.GetSection("ExternalServices:Redis:ConnectionString").Get<string>();

builder.Services.AddSingleton((sp) =>
{
    return StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnection, null);
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("MyPolicy",
                      policy  =>
                      {
                          policy.AllowAnyOrigin()
                                .AllowAnyMethod()
                                .AllowAnyHeader();
                      });
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();    

app.UseAuthorization();

app.UseCors("MyPolicy");

app.MapControllers();

app.Run();
