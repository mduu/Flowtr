using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Flowtr.Server.Data;

namespace Flowtr.Server.Components.Account;

/// <summary>
/// Placeholder until Flowtr can send e-mail. It sends nothing.
/// </summary>
internal sealed class IdentityNoOpEmailSender : IEmailSender<ApplicationUser>
{
    private readonly IEmailSender emailSender = new NoOpEmailSender();

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        emailSender.SendEmailAsync(email, "E-Mail-Adresse bestätigen", $"Bitte bestätige dein Konto: <a href='{confirmationLink}'>hier klicken</a>.");

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        emailSender.SendEmailAsync(email, "Passwort zurücksetzen", $"Setze dein Passwort hier zurück: <a href='{resetLink}'>hier klicken</a>.");

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        emailSender.SendEmailAsync(email, "Passwort zurücksetzen", $"Code zum Zurücksetzen deines Passworts: {resetCode}");
}
