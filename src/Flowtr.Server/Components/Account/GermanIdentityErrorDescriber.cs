using Microsoft.AspNetCore.Identity;

namespace Flowtr.Server.Components.Account;

/// <summary>
/// German texts for ASP.NET Core Identity errors shown on the account pages.
/// </summary>
internal sealed class GermanIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => Describe(nameof(DefaultError), "Ein unbekannter Fehler ist aufgetreten.");
    public override IdentityError ConcurrencyFailure() => Describe(nameof(ConcurrencyFailure), "Die Daten wurden in der Zwischenzeit geändert. Bitte erneut versuchen.");
    public override IdentityError PasswordMismatch() => Describe(nameof(PasswordMismatch), "Das Passwort ist falsch.");
    public override IdentityError InvalidToken() => Describe(nameof(InvalidToken), "Der Link ist ungültig oder abgelaufen.");
    public override IdentityError LoginAlreadyAssociated() => Describe(nameof(LoginAlreadyAssociated), "Diese Anmeldung ist bereits mit einem Konto verknüpft.");
    public override IdentityError InvalidUserName(string? userName) => Describe(nameof(InvalidUserName), $"Der Benutzername «{userName}» ist ungültig.");
    public override IdentityError InvalidEmail(string? email) => Describe(nameof(InvalidEmail), $"Die E-Mail-Adresse «{email}» ist ungültig.");
    public override IdentityError DuplicateUserName(string userName) => Describe(nameof(DuplicateUserName), $"Zu «{userName}» existiert bereits ein Konto.");
    public override IdentityError DuplicateEmail(string email) => Describe(nameof(DuplicateEmail), $"Zu «{email}» existiert bereits ein Konto.");
    public override IdentityError InvalidRoleName(string? role) => Describe(nameof(InvalidRoleName), $"Der Rollenname «{role}» ist ungültig.");
    public override IdentityError DuplicateRoleName(string role) => Describe(nameof(DuplicateRoleName), $"Die Rolle «{role}» existiert bereits.");
    public override IdentityError UserAlreadyHasPassword() => Describe(nameof(UserAlreadyHasPassword), "Für dieses Konto ist bereits ein Passwort gesetzt.");
    public override IdentityError UserLockoutNotEnabled() => Describe(nameof(UserLockoutNotEnabled), "Sperren ist für dieses Konto nicht aktiviert.");
    public override IdentityError UserAlreadyInRole(string role) => Describe(nameof(UserAlreadyInRole), $"Das Konto hat die Rolle «{role}» bereits.");
    public override IdentityError UserNotInRole(string role) => Describe(nameof(UserNotInRole), $"Das Konto hat die Rolle «{role}» nicht.");
    public override IdentityError PasswordTooShort(int length) => Describe(nameof(PasswordTooShort), $"Das Passwort muss mindestens {length} Zeichen lang sein.");
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => Describe(nameof(PasswordRequiresUniqueChars), $"Das Passwort muss mindestens {uniqueChars} verschiedene Zeichen enthalten.");
    public override IdentityError PasswordRequiresNonAlphanumeric() => Describe(nameof(PasswordRequiresNonAlphanumeric), "Das Passwort muss mindestens ein Sonderzeichen enthalten.");
    public override IdentityError PasswordRequiresDigit() => Describe(nameof(PasswordRequiresDigit), "Das Passwort muss mindestens eine Ziffer enthalten.");
    public override IdentityError PasswordRequiresLower() => Describe(nameof(PasswordRequiresLower), "Das Passwort muss mindestens einen Kleinbuchstaben enthalten.");
    public override IdentityError PasswordRequiresUpper() => Describe(nameof(PasswordRequiresUpper), "Das Passwort muss mindestens einen Grossbuchstaben enthalten.");
    public override IdentityError RecoveryCodeRedemptionFailed() => Describe(nameof(RecoveryCodeRedemptionFailed), "Der Wiederherstellungscode ist ungültig.");

    private static IdentityError Describe(string code, string description) => new() { Code = code, Description = description };
}
