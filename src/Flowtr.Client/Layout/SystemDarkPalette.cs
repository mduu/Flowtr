using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using MudBlazor;

namespace Flowtr.Client.Layout;

/// <summary>
/// Writes the dark palette into a CSS media query for a dark system setting.
/// Use it after a light <see cref="MudThemeProvider"/> for <see cref="ColourScheme.Auto"/>.
/// It needs no interactivity, thus it also works on the static account pages.
/// </summary>
public sealed class SystemDarkPalette : MudThemeProvider
{
    public SystemDarkPalette()
    {
        IsDarkMode = true;
        ObserveSystemDarkModeChange = false;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddMarkupContent(0, $"<style>@media (prefers-color-scheme: dark) {{\n{ThemeRules()}}}\n</style>");
    }

    private string ThemeRules()
    {
        var style = BuildTheme();
        var start = style.IndexOf('>') + 1;
        var end = style.LastIndexOf("</style>", StringComparison.Ordinal);

        return style[start..end];
    }
}
