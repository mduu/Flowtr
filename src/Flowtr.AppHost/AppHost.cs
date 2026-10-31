var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sql")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);
var database = sql.AddDatabase("flowtr");

var blobs = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite => azurite
        .WithDataVolume()
        .WithLifetime(ContainerLifetime.Persistent))
    .AddBlobs("blobs");

var server = builder.AddProject<Projects.Flowtr_Server>("server")
    .WithReference(database)
    .WithReference(blobs)
    .WaitFor(database)
    .WaitFor(blobs);

// A test mailbox (GreenMail) for the e-mail inbox: SMTP on localhost:3025 to send test mails, IMAP for the server.
// For a real mailbox: set "TestMailbox" to false here and the section "Mailbox" in the user secrets of the server (README).
if (!string.Equals(builder.Configuration["TestMailbox"], "false", StringComparison.OrdinalIgnoreCase))
{
    var mail = builder.AddContainer("mail", "greenmail/standalone", "2.1.14")
        .WithEnvironment("GREENMAIL_OPTS",
            "-Dgreenmail.setup.test.smtp -Dgreenmail.setup.test.imap -Dgreenmail.hostname=0.0.0.0 " +
            "-Dgreenmail.users=belege:belege@flowtr.test -Dgreenmail.users.login=email")
        .WithEndpoint(port: 3025, targetPort: 3025, name: "smtp", scheme: "tcp")
        .WithEndpoint(targetPort: 3143, name: "imap", scheme: "tcp");

    var imap = mail.GetEndpoint("imap");
    server
        .WithEnvironment("Mailbox__Host", imap.Property(EndpointProperty.Host))
        .WithEnvironment("Mailbox__Port", imap.Property(EndpointProperty.Port))
        .WithEnvironment("Mailbox__Security", "None")
        .WithEnvironment("Mailbox__UserName", "belege@flowtr.test")
        .WithEnvironment("Mailbox__Password", "belege")
        .WithEnvironment("Mailbox__Interval", "00:01:00")
        .WaitFor(mail);
}

builder.Build().Run();
