# UX & Design

- Modernes, freundliches Web-Interface. Es soll sich wenn möglich wie eine Desktop-Anwendung anfühlen.
- Responsive Design. User arbeiten mit Mobile (iPhone), Tablet und Laptop/Desktop.
- Farben gemäss Material Design und MudBlazor. Primärfarbe: *Indigo*.
- Header und Navigation hell (weiss, feine Trennlinie statt Schatten) mit farbigem Logo.
- Indigo ist die Akzentfarbe für Buttons, Links und Auswahl. Keine grossen Flächen in Indigo.
- UI-Texte auf Deutsch, Anrede «du».
- Konto-Seiten (Login, Registrierung, Konto verwalten) haben den gleichen MudBlazor-Look. Technisch sind sie statisches SSR mit MudBlazor.StaticInput ([ADR-003](adrs/003-mudblazor-staticinput-fuer-konto-seiten.md)).
- Lange Operationen zeigen einen Fortschritt oder mindestens einen Spinner.
- Uploads haben einen Upload-Dialog oder ein Upload-Control. Es zeigt den Fortschritt und gibt Feedback.
