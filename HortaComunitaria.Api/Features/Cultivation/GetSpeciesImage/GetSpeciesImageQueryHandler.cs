using System.Text.Json;
using HortaComunitaria.Api.Common.Results;
using HortaComunitaria.Api.Infrastructure.Observability;
using HortaComunitaria.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HortaComunitaria.Api.Features.Cultivation.GetSpeciesImage;

internal sealed record SpeciesImageResponse(string Base64, string MimeType, string SourceUrl, string Title,
    string Author, string License, string LicenseUrl, string Attribution, string Changes, string Sha256);

internal sealed class GetSpeciesImageQueryHandler(AppDbContext db, ILogger<GetSpeciesImageQueryHandler> logger)
{
    public async Task<Result<SpeciesImageResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        using var process = new ProcessLog(logger, "Cultivation.GetSpeciesImage");
        process.Step("QueryingImage", "Consultando imagem ilustrativa e créditos da espécie");
        var image = await db.Species.AsNoTracking().Where(x => x.Id == id && x.ImageBase64 != null)
            .Select(x => new { x.ImageBase64, x.ImageMetadataJson }).SingleOrDefaultAsync(cancellationToken);
        if (image is null) return process.Complete(Result<SpeciesImageResponse>.Failure(Error.NotFound()));
        using var metadata = JsonDocument.Parse(image.ImageMetadataJson!);
        string Field(string name) => metadata.RootElement.GetProperty(name).GetString()!;
        return process.Complete(Result<SpeciesImageResponse>.Success(new(image.ImageBase64!, Field("mimeType"),
            Field("sourceUrl"), Field("title"), Field("author"), Field("license"), Field("licenseUrl"), Field("attribution"), Field("changes"), Field("sha256"))));
    }
}
