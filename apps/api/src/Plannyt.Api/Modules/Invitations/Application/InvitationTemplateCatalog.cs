using System.Text.Json;
using Plannyt.Api.Modules.Invitations.Domain;

namespace Plannyt.Api.Modules.Invitations.Application;

public static class InvitationTemplateCatalog
{
    private static readonly DateTimeOffset SeedDate =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static IReadOnlyList<InvitationTemplate> CreateDefaults()
    {
        var definitions = new[]
        {
            Definition("11111111-1111-1111-1111-111111111111", "Editorial", "Tipografía protagonista y ritmo de revista.", "#F7F2EA", "#FFFFFF", "#26211D", "#9C5C3B", "playfair", "inter"),
            Definition("22222222-2222-2222-2222-222222222222", "Romántica", "Tonos cálidos y detalles delicados.", "#FFF5F6", "#FFFFFF", "#3F2930", "#B55772", "lora", "inter"),
            Definition("33333333-3333-3333-3333-333333333333", "Minimalista", "Composición limpia y directa.", "#FAFAFA", "#FFFFFF", "#171717", "#404040", "inter", "inter"),
            Definition("44444444-4444-4444-4444-444444444444", "Jardín", "Paleta orgánica inspirada en vegetación.", "#F2F6EE", "#FFFFFF", "#263526", "#557A46", "lora", "nunito"),
            Definition("55555555-5555-5555-5555-555555555555", "Noche elegante", "Contraste profundo con acentos dorados.", "#171923", "#232635", "#F7F3E8", "#D3AA58", "playfair", "montserrat"),
            Definition("66666666-6666-6666-6666-666666666666", "Infantil alegre", "Color amable, redondeado y enérgico.", "#FFF8E7", "#FFFFFF", "#26334A", "#F0784B", "nunito", "nunito"),
            Definition("77777777-7777-7777-7777-777777777777", "XV contemporáneo", "Elegancia juvenil con acento violeta.", "#F7F2FF", "#FFFFFF", "#302441", "#8056B3", "playfair", "montserrat"),
            Definition("88888888-8888-8888-8888-888888888888", "Corporativo limpio", "Jerarquía sobria para encuentros profesionales.", "#F2F6F9", "#FFFFFF", "#1D2A35", "#176B87", "montserrat", "inter")
        };
        return definitions.Select(item => InvitationTemplate.CreateGlobal(
            Guid.Parse(item.Id),
            item.Name,
            item.Description,
            JsonSerializer.Serialize(item.Theme),
            JsonSerializer.Serialize(DefaultBlocks(item.Name)),
            SeedDate)).ToList();
    }

    public static InvitationThemeRequest DefaultTheme() =>
        new(
            "#FAF7F2",
            "#FFFFFF",
            "#292421",
            "#A85D43",
            "playfair",
            "inter",
            "lg",
            "comfortable",
            "card",
            "solid",
            InvitationAnimationLevel.Reduced);

    public static IReadOnlyList<InvitationBlockRequest> DefaultBlocks(string title) =>
    [
        Block(InvitationBlockType.Cover, 0, new
        {
            eyebrow = "Estás invitado",
            title,
            subtitle = "{{group.displayName}}"
        }),
        Block(InvitationBlockType.Greeting, 1, new
        {
            title = "Nos encantará compartir este momento contigo",
            body = "Preparamos esta invitación especialmente para tu grupo."
        }),
        Block(InvitationBlockType.Participants, 2, new
        {
            heading = "Esta invitación incluye a",
            format = "list"
        }),
        Block(InvitationBlockType.EventDate, 3, new
        {
            heading = "Fecha del evento",
            dateFormat = "long",
            showTimeZone = true
        }),
        Block(InvitationBlockType.DressCode, 4, new
        {
            heading = "Código de vestimenta",
            value = "Por definir",
            details = ""
        }),
        Block(InvitationBlockType.Footer, 5, new
        {
            text = "Invitación privada creada con Plannyt"
        })
    ];

    private static InvitationBlockRequest Block(
        InvitationBlockType type,
        int order,
        object content) =>
        new(
            Guid.NewGuid(),
            type,
            true,
            BlockVisibility.Everyone,
            null,
            order,
            JsonSerializer.SerializeToElement(content),
            JsonSerializer.SerializeToElement(new
            {
                backgroundToken = "default",
                textAlign = "center",
                emphasis = "normal",
                width = "content"
            }));

    private static TemplateDefinition Definition(
        string id,
        string name,
        string description,
        string background,
        string surface,
        string text,
        string accent,
        string headingFont,
        string bodyFont) =>
        new(
            id,
            name,
            description,
            new InvitationThemeRequest(
                background,
                surface,
                text,
                accent,
                headingFont,
                bodyFont,
                "lg",
                "comfortable",
                "card",
                "solid",
                InvitationAnimationLevel.Reduced));

    private sealed record TemplateDefinition(
        string Id,
        string Name,
        string Description,
        InvitationThemeRequest Theme);
}
