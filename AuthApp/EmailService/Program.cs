using EmailService.Messaging;
using EmailProcessor = EmailService.Services.EmailService;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOptions<EmailService.Services.SmtpOptions>()
    .Bind(builder.Configuration.GetSection("Smtp"))
    .Validate(options => options.IsValid(), "Configuracao SMTP invalida: confira host, porta, remetente, TLS e credenciais.")
    .ValidateOnStart();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddSingleton<EmailProcessor>();
builder.Services.AddHostedService<RabbitMqConsumer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
