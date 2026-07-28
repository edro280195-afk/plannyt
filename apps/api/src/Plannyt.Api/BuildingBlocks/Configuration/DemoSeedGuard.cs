namespace Plannyt.Api.BuildingBlocks.Configuration;

public static class DemoSeedGuard
{
    public static void Validate(IHostEnvironment environment, IConfiguration configuration)
    {
        var options = configuration
            .GetSection(DemoSeedOptions.SectionName)
            .Get<DemoSeedOptions>() ?? new DemoSeedOptions();

        if (options.Enabled && !environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Los datos demo solo pueden habilitarse en Development.");
        }

        if (options.Enabled
            && (string.IsNullOrWhiteSpace(options.PlannerEmail)
                || string.IsNullOrWhiteSpace(options.PlannerPassword)))
        {
            throw new InvalidOperationException(
                "El seed demo requiere correo y contraseña configurados localmente.");
        }
    }
}
