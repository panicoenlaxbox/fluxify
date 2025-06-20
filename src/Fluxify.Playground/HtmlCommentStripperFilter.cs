using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text.RegularExpressions;

namespace AgenticPatterns;

internal class HtmlCommentStripperFilter(ILogger<HtmlCommentStripperFilter> logger) : IPromptRenderFilter
{
    public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
    {
        logger.LogDebug("PromptRenderFilter.RenderInvoking - {FunctionName}", context.Function.Name);

        await next(context);

        if (context.RenderedPrompt is not null)
        {
            context.RenderedPrompt = Regex.Replace(context.RenderedPrompt, @"<!--.*?-->", string.Empty, RegexOptions.Singleline);
        }

        logger.LogDebug("PromptRenderFilter.RenderInvoked - {FunctionName}", context.Function.Name);
    }
}
