using HortaComunitaria.Api.Domain.History;
using HortaComunitaria.Api.Domain.Irrigation;
using HortaComunitaria.Api.Domain.Cultivation;
using HortaComunitaria.Api.Domain.Environment;
using HortaComunitaria.Api.Domain.Occurrences;
using HortaComunitaria.Api.Domain.Costs;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace HortaComunitaria.Api.Infrastructure.Persistence;

/// <summary>Registro único compartilhado pelo host, CLI e fixtures PostgreSQL.</summary>
internal static class PostgresEnums
{
    internal static void Configure(NpgsqlDbContextOptionsBuilder options)
    {
        options.MapEnum<IrrigationStatus>("irrigation_status");
        options.MapEnum<VolumeSource>("volume_source");
        options.MapEnum<RecordValidity>("record_validity");
        options.MapEnum<AuditOperation>("audit_operation");
        options.MapEnum<MaterialType>("material_type");
        options.MapEnum<CultivationStatus>("cultivation_status");
        options.MapEnum<PlantingKind>("planting_kind");
        options.MapEnum<DryingStatus>("drying_status");
        options.MapEnum<MeasurementQuality>("measurement_quality");
        options.MapEnum<OccurrenceStatus>("occurrence_status");
        options.MapEnum<FinancialNature>("financial_nature");
        options.MapEnum<HortaComunitaria.Api.Infrastructure.Identity.UserProfile>("user_profile");
    }
}
