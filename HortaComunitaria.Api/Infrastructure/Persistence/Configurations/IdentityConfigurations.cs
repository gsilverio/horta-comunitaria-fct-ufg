using HortaComunitaria.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HortaComunitaria.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Usuários do Identity na tabela <c>users</c>, que também identifica a autoria dos registros. O autor técnico do
/// protótipo permanece como linha sem credenciais, marcada por <c>IsDemonstration</c>.
/// </summary>
internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("users", table =>
            table.HasCheckConstraint("ck_users_demonstration_without_credentials", "NOT \"IsDemonstration\" OR (\"PasswordHash\" IS NULL AND NOT \"IsActive\")"));
        builder.Property(x => x.Id).HasColumnType("uuid").ValueGeneratedNever();
        builder.Property(x => x.DisplayName).HasMaxLength(AppUser.DisplayNameMaxLength).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.Profile).HasColumnType("user_profile").IsRequired();
        builder.Property(x => x.MustChangePassword).IsRequired();
        builder.Property(x => x.IsDemonstration).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
    }
}

internal sealed class UserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder) => builder.ToTable("user_claims");
}

internal sealed class UserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder) => builder.ToTable("user_logins");
}

internal sealed class UserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder) => builder.ToTable("user_tokens");
}
