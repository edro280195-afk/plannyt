namespace Plannyt.Api.BuildingBlocks.Configuration;

public static class FileStorageGuard
{
    public static void Validate(IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "El almacenamiento local de documentos solo puede usarse en Development. "
                + "Configura un proveedor de IFileStorage para otros entornos.");
        }
    }
}
