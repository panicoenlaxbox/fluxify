#pragma warning disable SKEXP0001

using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;

namespace Fluxify.Playground.Steps;

public class SupportStep : ActionStepBase<string>
{
    private readonly Kernel _kernel;
    private readonly VectorStoreCollection<ulong, Document> _collection;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _textEmbeddingGenerationService;

    public SupportStep(Kernel kernel, VectorStoreCollection<ulong, Document> collection, IEmbeddingGenerator<string, Embedding<float>> textEmbeddingGenerationService)
    {
        _kernel = kernel;
        _collection = collection;
        _textEmbeddingGenerationService = textEmbeddingGenerationService;

        var textSearch = new VectorStoreTextSearch<Document>(_collection, _textEmbeddingGenerationService);
        var searchPlugin = textSearch.CreateWithGetTextSearchResults("SearchPlugin");
        _kernel.Plugins.Add(searchPlugin);
    }

    protected override async Task<string?> ExecuteCoreAsync(string input, ExecutionPlanContext context, CancellationToken cancellationToken = default)
    {        
        var text = await EmbeddedResourceLoader.LoadAsync("Support.yaml", cancellationToken: cancellationToken);
        var function = _kernel.CreateFunctionFromPromptYaml(text, new HandlebarsPromptTemplateFactory());
        var arguments = new KernelArguments
        {
            ["query"] = context.Input,
            ["history"] = context.GetHistoryForPrompt()
        };
        var functionResult = await _kernel.InvokeAsync(function, arguments, cancellationToken);
        return functionResult.GetValue<string>();
    }
}