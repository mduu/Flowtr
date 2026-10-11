# Flowtr
Einfache Buchhaltung für Ausgaben und Einnahmen als Nebenerwerb. Tracken von Buchungen mit passenden Belegen.
## Konzept
Flowtr ist eine Web-App, welche das Erfassen von Ein- und Ausgaben für Einzelfirmen bzw. Nebenerwerb möglichst einfach macht. Das Ziel ist, dass einmal im Jahr auf Knopfdruck alle Unterlagen für den Treuhänder bzw. die Schweizer Steuerabrechnung exportiert werden können.

Das Erfassen der Buchungen (Einnahme, Ausgaben) sollte möglichst effizient gehen, da dies oft nebenbei "schnell schnell" via Smartphone, Tablet oder Laptop geschieht. Dazu ist die Idee, dass man Belege wie *Microsoft Azure* Subscriptions, *Anthropic* Belege, *Stripe* Vergütungen, *Google Firebase* Abrechnungen aber auch Belege für neue Computer uvm. in die Anwendung senden kann (z.B. via Beleg-Upload) und *Flowtr* sich smart darum kümmert.

Wenn man einen Beleg hochlädt, soll *Flowtr* diesen u.a. mithilfe von KI (z.B. *Anthropic* API, Claude) anschauen und daraus die notwendigen Daten für die Aufstellung (Buchungen) extrahieren. Die App zeigt die gefundenen Angaben an, sodass sie der User bestätigen oder noch korrigieren kann. Wird die Buchung final gespeichert, legt *Flowtr* den Beleg sauber nummeriert ab und erstellt die entsprechende Buchung (Zeile) im Journal mit Referenz auf den nummerierten Beleg. Belege für Ein- und Ausgaben werden separat gespeichert und sind als Dateien 1:1 abgelegt. Der Dateiname lässt eine Zuordnung zum Journal zu (=umbenennen des Beleges beim Einlesen).

Belege sollten auf unterschiedliche Art eingereicht werden können:
- Interaktives Hochladen in der Web-App
- Als Email an eine bestimmte Mailbox weiterleiten. Diese Mailbox kann von *Flowtr* abgefragt und als Input verwendet werden.
- Denkbar in Zukunft wäre auch eine native App welche als "Senden an" bzw. "Teilen" Ziel fungiert.

Das Journal muss als Informationen alles beinhalten, was für die Schweizer Steuerabrechnung relevant ist (aka Milchbüchlein). Das Journal kann nachbearbeitet werden solange es noch nicht für die Steuern abgegeben wurde. Danach ist der abgegebene Teil read-only (=archiv). Über das Journal sollten auch Abfragen und Analysen möglich sein. Hier bietet sich eine echte Datenbank an.

Wenn es Zeit wird, die Abrechnung für die Steuererklärung zu machen, muss ich als Anwender auf Knopfdruck das Journal sowie alle dazu gehörigen Belege exportieren. Das muss digital sein (Excel + Belegverzeichnisse). Interessant wäre auch, wenn ich dem Treuhänder nur einen Link zum *Dossier* (=Journal + Belege) senden könnte und er das dann direkt herunterladen kann. Das muss allerdings sicher sein - also kein rein öffentlicher Link. Beispielsweise, dass der Treuhänder vor dem Download nochmals einen Code per Email erhält, den er dann eingeben muss, um den Download freizuschalten. Das Journal muss auch als PDF und Excel exportierbar sein. Dabei muss der Datumsbereich jeweils gewählt werden können.

Die Lösung muss zwingend sauber gesichert (Backup) werden. Am besten redundant (z.B. 1x pro Monat zusätzlich noch Offline/On-Prem). Die Daten (Journal und Belege) dürfen auf gar keinen Fall verloren gehen können. Denkbar wäre auch ein laufender offsite Backup von Azure zu on-prem über einen entsprechenden Cron-Job etc. Ich könnte hierzu z.B. einen FTP-Upload oder Cloud Sync auf meinem on-prem Synology NAS freigeben/einrichten wenn *Flowtr* die entsprechenden Funktionen anbietet. Das wäre noch besser als manuelles on-prem sichern. Hier müsste *Flowtr* dann Alerts senden, wenn der Backup eine Zeit lang nicht funktioniert.

Die Lösung muss aktuelle Sicherheitsstandards implementieren, da es um Buchhaltungsdaten geht.

## Journal
Das Journal muss alle Angaben enthalten, damit die Schweizer Steuererklärung (z.B. Aargau) damit gemacht werden kann. Wenn ein Treuhänder die Steuererklärung macht, muss es auch für diesen klar sein.

## Sicherheit
Die Lösung muss sicher sein, da sie mit Buchhaltungsdaten umgeht. Die Security ist ein Key-Feature und Teil der Basis.