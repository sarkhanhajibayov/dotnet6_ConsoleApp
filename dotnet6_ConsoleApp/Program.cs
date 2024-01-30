using System;
using System.IO;
using System.Text;
using dotnet6_ConsoleApp.Data;
using dotnet6_ConsoleApp.Models;
using dotnet6_ConsoleApp.Services;
using KissLog;
using KissLog.AspNetCore;
using KissLog.CloudListeners.Auth;
using KissLog.CloudListeners.RequestLogsListener;
using KissLog.Formatters;
using KissLog.Listeners.FileListener;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

class Program
{
    private readonly ILogger logger;
    static void Main()
    {
        Logger.SetFactory(new KissLog.LoggerFactory(new Logger(url: "ConsoleApp/Main")));

        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var serviceProvider = ConfigureServices(configuration);

        ConfigureKissLog(configuration);

        ILogger logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        IFooService fooService = serviceProvider.GetRequiredService<IFooService>();
        fooService.Foo();
        AddProduct(logger);
        var loggers = Logger.Factory.GetAll();
        Logger.NotifyListeners(loggers);
    }

    static IServiceProvider ConfigureServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();

        services.AddLogging(logging =>
        {
            logging
                .AddConfiguration(configuration.GetSection("Logging"))
                .AddSimpleConsole()
                .AddKissLog(options =>
                {
                    options.Formatter = (FormatterArgs args) =>
                    {
                        if (args.Exception == null)
                            return args.DefaultValue;

                        string exceptionStr = new ExceptionFormatter().Format(args.Exception, args.Logger);
                        return string.Join(Environment.NewLine, new[] { args.DefaultValue, exceptionStr });
                    };
                });
        });

        services.AddTransient<IFooService, FooService>();

        return services.BuildServiceProvider();
    }
    public static void AddProduct(ILogger logger)
    {
        // Your logic to add the product to the database
        AppDbContext context = new AppDbContext();
        User user = new User();
        user.Name = "Sarkhan";
        context.Add(user);
        Product product1 = new Product();
        product1.Name = "Milk";
        product1.CreatedBy = user;
        product1.CreatedAt = DateTime.Now;
        context.SaveChanges();
        
        // Log the creation message using KissLog
        string logMessage = $"Product {product1.Name} created by {product1.CreatedBy.Name}";
        logger.LogInformation(logMessage);
    }


    static void ConfigureKissLog(IConfiguration configuration)
    {
        KissLogConfiguration.Listeners
            .Add(new RequestLogsApiListener(new Application(configuration["KissLog.OrganizationId"], configuration["KissLog.ApplicationId"]))
            {
                ApiUrl = configuration["KissLog.ApiUrl"],
                UseAsync = false
            });

        KissLogConfiguration.Listeners
            .Add(new LocalTextFileListener(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs")));

        // KissLog internal logs
        KissLogConfiguration.InternalLog = (message) =>
        {
            Console.WriteLine(message);
        };
    }
}
