using AiAssistant.Domain.Interfaces;
using AiAssistant.Infrastructure.Configuration;
using AiAssistant.Infrastructure.DocumentProcessing;
using AiAssistant.Infrastructure.Ollama;
using AiAssistant.Infrastructure.OpenAI;
using AiAssistant.Infrastructure.Qdrant;
using AiAssistant.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OpenAI;
using Qdrant.Client;

namespace AiAssistant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var provider = config["AI:Provider"] ?? "Ollama";

        if (provider == "OpenAI")
            services.AddOpenAi(config);
        else
            services.AddOllama(config);
        
        AddAgentService(services);
        AddPDocumentProcessors(services);
        AddQdrant(services, config);

        return services;
    }

    public static void AddQdrant(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<QdrantOptions>(
            config.GetSection(QdrantOptions.SectionName));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<QdrantOptions>>().Value;
            return new QdrantClient(options.Host, options.Port);
        });

        services.AddScoped<IVectorStore, QdrantVectorStore>();
    }

    private static void AddPDocumentProcessors(this IServiceCollection services)
    {
        services.AddScoped<IDocumentProcessor, PdfDocumentProcessor>();
        services.AddScoped<IDocumentProcessor, WordDocumentProcessor>();
    }

    private static void AddAgentService(this IServiceCollection services)
    {
        services.AddScoped<IAgentService, AgentService>();
    }
    
    // Models
    private static void AddOpenAi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OpenAiOptions>(
            configuration.GetSection(OpenAiOptions.SectionName));

        services.AddScoped<IEmbeddingService, OpenAiEmbeddingService>();
        services.AddScoped<ILlmService, OpenAiLlmService>();
    }
    
    private static void AddOllama(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OllamaOptions>(
            configuration.GetSection(OllamaOptions.SectionName));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            return new OllamaApiClient(new Uri(options.BaseUrl));
        });

        services.AddScoped<IEmbeddingService, OllamaEmbeddingService>();
        services.AddScoped<ILlmService, OllamaLlmService>();
    }
}
