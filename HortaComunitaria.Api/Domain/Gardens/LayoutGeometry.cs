using HortaComunitaria.Api.Common.Results;
using System.Text.Json.Serialization;

namespace HortaComunitaria.Api.Domain.Gardens;

/// <summary>Ponto no grid de desenho, sem unidade física implícita.</summary>
public sealed record LayoutPoint(double X, double Y);
/// <summary>Parâmetros dos canteiros radiais.</summary>
public sealed record LayoutMandala(string BedStyle, double CenterX, double CenterY, double RingRadius, double AngleDeg, double AngularWidthDeg, double RadialLength);
/// <summary>Forma visual com vínculo opcional a cadastro da mesma horta.</summary>
public sealed record LayoutElement(string Id, string Name, string Role, string Shape, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? EntityId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? SectorId = null, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? SectorName = null, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Source = null, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] bool? IsEstimated = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? X = null, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? Y = null, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? Width = null, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? Height = null, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? RotationDeg = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? CenterX = null, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? CenterY = null, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] double? Radius = null, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] LayoutPoint[]? Outline = null, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] LayoutMandala? Mandala = null);
/// <summary>Documento persistido; dimensões apenas enquadram a geometria livre.</summary>
public sealed record LayoutDocument(double Width, double Height, LayoutPoint[]? Boundary, LayoutElement[] Elements);

/// <summary>Invariantes geométricas do mapa, independentes de transporte e persistência.</summary>
public static class LayoutGeometry
{
    private const double Epsilon = 1e-7;
    /// <summary>Confere conteúdo, formas e hierarquia espacial sem transformar unidades em metros.</summary>
    public static Error? Validate(LayoutDocument document)
    {
        if (!double.IsFinite(document.Width) || !double.IsFinite(document.Height) || document.Width <= 0 || document.Height <= 0 || document.Elements is null || document.Elements.Length > 2000)
            return Error.Validation("layout", "Enquadramento ou quantidade de elementos inválidos.");
        if (document.Boundary is null) return document.Elements.Length == 0 ? null : Error.Validation("boundary", "Defina o terreno antes de salvar setores e canteiros.");
        if (!ValidPolygon(document.Boundary)) return Error.Validation("boundary", "O terreno precisa formar uma área sem cruzamentos.");
        if (document.Elements.Any(x => x is null || string.IsNullOrWhiteSpace(x.Id) || x.Id.Length > 200 || string.IsNullOrWhiteSpace(x.Name) || x.Name.Length > 120) || document.Elements.Select(x => x.Id).Distinct().Count() != document.Elements.Length)
            return Error.Validation("elements", "Elementos exigem IDs únicos e nomes de até 120 caracteres.");
        foreach (var element in document.Elements)
        {
            if (element.Role is not ("bed" or "sector" or "reference") || element.Shape is not ("rectangle" or "circle") || element.EntityId == Guid.Empty)
                return Error.Validation("elements", "Papel, forma ou vínculo do elemento inválido.");
            var values = new[] { element.X, element.Y, element.Width, element.Height, element.RotationDeg, element.CenterX, element.CenterY, element.Radius };
            if (values.Any(x => x.HasValue && !double.IsFinite(x.Value)) || element.Shape == "rectangle" && (element.X is null || element.Y is null || element.Width is not > 0 || element.Height is not > 0 || element.RotationDeg is null) || element.Shape == "circle" && (element.CenterX is null || element.CenterY is null || element.Radius is not > 0))
                return Error.Validation("elements", "Informe coordenadas finitas e dimensões positivas para cada forma.");
            if (element.Outline is not null && (element.Role != "sector" || !ValidPolygon(element.Outline))) return Error.Validation("outline", "Contorno de setor inválido.");
            if (element.Mandala is { } m && (element.Shape != "rectangle" || m.BedStyle is not ("curved" or "rectangle") || new[] { m.CenterX, m.CenterY, m.RingRadius, m.AngleDeg, m.AngularWidthDeg, m.RadialLength }.Any(x => !double.IsFinite(x)) || m.RingRadius <= 0 || m.AngularWidthDeg is <= 0 or > 120 || m.RadialLength <= 0))
                return Error.Validation("mandala", "Parâmetros da mandala inválidos.");
            if (!Contains(document.Boundary, Points(element))) return Error.Validation("elements", "Todos os objetos precisam ficar dentro do terreno.");
        }
        foreach (var bed in document.Elements.Where(x => x.Role == "bed"))
        {
            var sector = document.Elements.FirstOrDefault(x => x.Role == "sector" && x.Id == bed.SectorId);
            if (sector is null || !Overlaps(Points(sector), Points(bed))) return Error.Validation("sectorId", "Cada canteiro precisa sobrepor o setor ao qual foi associado.");
        }
        return null;
    }
    /// <summary>Polígono aproximado das formas curvas e exato das formas lineares.</summary>
    public static LayoutPoint[] Points(LayoutElement element)
    {
        if (element.Outline is not null) return element.Outline;
        if (element.Shape == "circle") return Enumerable.Range(0, 96).Select(i => new LayoutPoint(element.CenterX!.Value + Math.Cos(i * Math.PI / 48) * element.Radius!.Value, element.CenterY!.Value + Math.Sin(i * Math.PI / 48) * element.Radius!.Value)).ToArray();
        if (element.Mandala is { BedStyle: "curved" } m)
        {
            var angles = Enumerable.Range(0, 49).Select(i => (m.AngleDeg - m.AngularWidthDeg / 2 + m.AngularWidthDeg * i / 48) * Math.PI / 180).ToArray();
            return angles.Select(a => new LayoutPoint(m.CenterX + Math.Cos(a) * (m.RingRadius + m.RadialLength / 2), m.CenterY + Math.Sin(a) * (m.RingRadius + m.RadialLength / 2)))
                .Concat(angles.Reverse().Select(a => new LayoutPoint(m.CenterX + Math.Cos(a) * Math.Max(0, m.RingRadius - m.RadialLength / 2), m.CenterY + Math.Sin(a) * Math.Max(0, m.RingRadius - m.RadialLength / 2)))).ToArray();
        }
        var x = element.X!.Value; var y = element.Y!.Value; var w = element.Width!.Value; var h = element.Height!.Value;
        var cx = x + w / 2; var cy = y + h / 2; var a = element.RotationDeg!.Value * Math.PI / 180;
        return new[] { new LayoutPoint(x, y), new(x + w, y), new(x + w, y + h), new(x, y + h) }.Select(p => new LayoutPoint(cx + (p.X - cx) * Math.Cos(a) - (p.Y - cy) * Math.Sin(a), cy + (p.X - cx) * Math.Sin(a) + (p.Y - cy) * Math.Cos(a))).ToArray();
    }
    private static double Cross(LayoutPoint a, LayoutPoint b, LayoutPoint p) => (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X);
    private static bool Crosses(LayoutPoint a, LayoutPoint b, LayoutPoint c, LayoutPoint d) => Cross(a, b, c) * Cross(a, b, d) < -Epsilon && Cross(c, d, a) * Cross(c, d, b) < -Epsilon;
    private static bool OnEdge(LayoutPoint a, LayoutPoint b, LayoutPoint p) => Math.Abs(Cross(a, b, p)) < Epsilon && p.X >= Math.Min(a.X, b.X) - Epsilon && p.X <= Math.Max(a.X, b.X) + Epsilon && p.Y >= Math.Min(a.Y, b.Y) - Epsilon && p.Y <= Math.Max(a.Y, b.Y) + Epsilon;
    private static bool Inside(LayoutPoint p, LayoutPoint[] polygon)
    {
        var inside = false;
        for (var i = 0; i < polygon.Length; i++)
        {
            var a = polygon[i]; var b = polygon[(i + 1) % polygon.Length];
            if (OnEdge(a, b, p)) return true;
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }
        return inside;
    }
    private static bool ValidPolygon(LayoutPoint[] points)
    {
        if (points.Length is < 3 or > 512 || points.Any(p => p is null || !double.IsFinite(p.X) || !double.IsFinite(p.Y) || p.X < 0 || p.Y < 0)) return false;
        if (Math.Abs(points.Select((p, i) => p.X * points[(i + 1) % points.Length].Y - points[(i + 1) % points.Length].X * p.Y).Sum()) < Epsilon) return false;
        for (var i = 0; i < points.Length; i++) for (var j = i + 1; j < points.Length; j++)
        {
            if (points[i] == points[j]) return false;
            if (j != i + 1 && !(i == 0 && j == points.Length - 1) && Crosses(points[i], points[(i + 1) % points.Length], points[j], points[(j + 1) % points.Length])) return false;
        }
        return true;
    }
    private static bool Contains(LayoutPoint[] outer, LayoutPoint[] inner) => inner.All(p => Inside(p, outer)) && !inner.Where((p, i) => outer.Where((q, j) => Crosses(p, inner[(i + 1) % inner.Length], q, outer[(j + 1) % outer.Length])).Any()).Any();
    private static bool Overlaps(LayoutPoint[] a, LayoutPoint[] b) => a.Any(p => Inside(p, b)) || b.Any(p => Inside(p, a)) || a.Where((p, i) => b.Where((q, j) => Crosses(p, a[(i + 1) % a.Length], q, b[(j + 1) % b.Length])).Any()).Any();
}
