# UX & Design

- Modernes, freundliches Web-Interface. Es sollte sich nach Möglichkeit anfühlen wie eine Desktop-Anwendung.
- Responsive Design, da die Software sowohl via Mobile (iPhone) als auch via Tablet und Laptop/Desktop genutzt wird.
- Farben gemäss Material Design bzw. MudBlazor. Primärfarbe: *Indigo*.
- Header und Navigation hell (weiss, feine Trennlinie statt Schatten) mit farbigem Logo. Indigo als Akzentfarbe für Buttons, Links und Auswahl, nicht als grosse Fläche.
- UI-Texte auf Deutsch, Anrede «du».
- Konto-Seiten (Login, Registrierung, Konto verwalten) im gleichen MudBlazor-Look; technisch statisches SSR mit MudBlazor.StaticInput ([ADR-003](adrs/003-mudblazor-staticinput-fuer-konto-seiten.md)).
- Für Operationen, die länger dauern, muss ein Fortschritt oder mind. ein Spinner angezeigt werden.
- Upload mit entsprechendem Upload-Dialog/-Control, wo man den Fortschritt sieht bzw. Feedback erhält.
