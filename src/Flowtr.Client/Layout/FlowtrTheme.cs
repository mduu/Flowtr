using MudBlazor;

namespace Flowtr.Client.Layout;

/// <summary>
/// MudBlazor theme with Material Indigo as primary colour (see specs/ux.md).
/// </summary>
public static class FlowtrTheme
{
    public static readonly MudTheme Theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = Colors.Indigo.Default,
            AppbarBackground = Colors.Shades.White,
            AppbarText = "#171731",
        },
        PaletteDark = new PaletteDark
        {
            Primary = Colors.Indigo.Lighten1,
        },
        Typography = new Typography
        {
            Button = new ButtonTypography { TextTransform = "none" },
        },
    };
}
