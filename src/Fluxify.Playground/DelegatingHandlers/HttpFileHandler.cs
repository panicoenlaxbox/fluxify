using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Fluxify.Playground.DelegatingHandlers;

internal class HttpFileHandler(string path, IEnumerable<string> redactedHeaders) : DelegatingHandler
{
    private static readonly Lock FileLock = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var content = new StringBuilder();

        content.AppendLine($"{request.Method} {request.RequestUri}");

        WriteHeaders(request.Headers, content);

        if (request.Content is not null)
        {
            WriteHeaders(request.Content.Headers, content);

            var originalStream = await request.Content.ReadAsStreamAsync(cancellationToken);

            var memoryStream = new MemoryStream();
            await originalStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Seek(0, SeekOrigin.Begin);

            using var requestReader = new StreamReader(memoryStream, leaveOpen: true);
            var requestContent = await requestReader.ReadToEndAsync(cancellationToken);

            if (request.Content.Headers.ContentType?.MediaType == MediaTypeNames.Application.Json)
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(requestContent);
                requestContent = JsonSerializer.Serialize(jsonElement, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }

            content.AppendLine($"{Environment.NewLine}{requestContent}");

            memoryStream.Seek(0, SeekOrigin.Begin);

            var newContent = new StreamContent(memoryStream);
            foreach (var header in request.Content.Headers)
            {
                newContent.Headers.Add(header.Key, header.Value);
            }

            request.Content = newContent;
        }

        content.AppendLine();

        var response = await base.SendAsync(request, cancellationToken);

        content.AppendLine("###");
        content.AppendLine();

        WriteHeaders(response.Headers, content, "# ");

        WriteHeaders(response.Content.Headers, content, "# ");

        var skipBodyDump =
            response.Headers.TransferEncodingChunked == true ||
            response.Content.Headers.ContentType?.MediaType == MediaTypeNames.Text.EventStream;

        if (!skipBodyDump)
        {
            // ReadAsStringAsync() buffers the content internally, so it won’t interfere with other consumers of response.Content, unlike ReadAsStreamAsync() which consumes the stream directly
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            responseContent = string.Join(Environment.NewLine,
                responseContent.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => "# " + line));

            content.AppendLine(responseContent);
            content.AppendLine();
        }
        else
        {
            content.AppendLine("# Chunked or streaming response detected. Skipping body dump.");
            content.AppendLine();
        }

        lock (FileLock)
        {
            File.AppendAllText(path,
                $"{(File.Exists(path) ? $"###{Environment.NewLine}{Environment.NewLine}{content}" : content.ToString())}");
        }

        return response;
    }

    private void WriteHeaders(HttpHeaders headers, StringBuilder content, string? prefix = null)
    {
        foreach (var header in headers)
        {
            if (redactedHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
            {
                content.AppendLine($"{prefix}{header.Key}: *");
                continue;
            }

            content.AppendLine($"{prefix}{header.Key}: {string.Join(" ", header.Value)}");
        }
    }
}
