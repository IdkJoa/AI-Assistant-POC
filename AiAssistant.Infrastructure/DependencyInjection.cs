using System.Net.Http.Headers;
using AiAssistant.Domain.Interfaces;
using AiAssistant.Infrastructure.Configuration;
using AiAssistant.Infrastructure.DocumentProcessing;
using AiAssistant.Infrastructure.Ollama;
using AiAssistant.Infrastructure.OpenAi;
using AiAssistant.Infrastructure.OpenAI;
using AiAssistant.Infrastructure.Qdrant;
using AiAssistant.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OllamaSharp;
using Qdrant.Client;

namespace AiAssistant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        //AddGemini(services, config);
        AddMbSuite(services, config);
        AddClaude(services, config);
        AddAgentService(services);
        AddPDocumentProcessors(services);
        AddQdrant(services, config);

        return services;
    }

    private static void AddQdrant(this IServiceCollection services, IConfiguration config)
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
    
    // AI Models
    private static void AddOpenAi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));
        services.AddScoped<IEmbeddingService, OpenAiEmbeddingService>();
        services.AddScoped<ILlmService, OpenAiLlmService>();
    }
    
    private static void AddOllama(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OllamaOptions>(configuration.GetSection(OllamaOptions.SectionName));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            return new OllamaApiClient(new Uri(options.BaseUrl));
        });

        services.AddScoped<IEmbeddingService, OllamaEmbeddingService>();
        services.AddScoped<ILlmService, OllamaLlmService>();
    }
    
    private static void AddGemini(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.Configure<GeminiOptions>(config.GetSection(GeminiOptions.SectionName));
        services.AddScoped<IEmbeddingService, GeminiEmbeddingService>();
        services.AddScoped<ILlmService, GeminiLlmService>();
    }

    private static void AddClaude(this IServiceCollection services, IConfiguration configuration)
    {
            services.Configure<ClaudeOptions>(configuration.GetSection(ClaudeOptions.SectionName));
            services.Configure<OllamaOptions>(configuration.GetSection(OllamaOptions.SectionName));

            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
                return new OllamaApiClient(new Uri(options.BaseUrl));
            });

            services.AddScoped<IEmbeddingService, OllamaEmbeddingService>();
            services.AddScoped<ILlmService, ClaudeLlmService>();
    }
    
    private static void AddMbSuite(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<MbSuiteOptions>(config.GetSection(MbSuiteOptions.SectionName));
        
        services.AddHttpClient<MbSuiteClient>((sp, http) =>
        {
            var options = sp.GetRequiredService<IOptions<MbSuiteOptions>>().Value;
            http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", options.AccessToken.Trim());
            http.DefaultRequestHeaders.Add("X-Organization-Id", options.OrganizationId.Trim());
            http.DefaultRequestHeaders.Add("X-Branch-Id",       options.BranchId.Trim());
        });
    }
}
