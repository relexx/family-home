# Sicherheitsrichtlinie

FamilyHome verwaltet Daten von Familien und Kindern. Sicherheitsmeldungen haben deshalb Vorrang.

## Schwachstellen melden

Bitte **keine öffentlichen Issues, Diskussionen oder Pull Requests** für Sicherheitsprobleme.

**Melden:** <https://github.com/relexx/family-home/security/advisories/new>
(GitHub → Reiter **Security** → **Report a vulnerability**, „Private Vulnerability Reporting“).

Hilfreich sind:
- betroffene Komponente (App, Server, Deployment, Pipeline) und Version bzw. Commit;
- Schritte zum Nachvollziehen bzw. ein Proof of Concept;
- erwartete Auswirkung (z. B. welche Daten oder Rechte betroffen sind).

## Ablauf und Fristen

| Schritt | Ziel |
|---|---|
| Eingangsbestätigung | innerhalb von **3 Werktagen** |
| Erste Einschätzung (Schweregrad nach CVSS) | innerhalb von **7 Tagen** |
| Fix für kritische/hohe Lücken | angestrebt innerhalb von **30 Tagen** |
| Veröffentlichung | koordiniert, nach Bereitstellung des Fixes, als [GitHub Security Advisory](https://github.com/relexx/family-home/security/advisories) mit CVE, sofern sinnvoll |

Wir nennen Meldende auf Wunsch im Advisory. Einen Bug-Bounty gibt es nicht.

## Unterstützte Versionen

Unterstützt wird nur die jeweils **neueste Version** von App und Server.
- Ältere Apps werden über die vom Server erzwungene Mindestversion aktualisiert.
- Selbst-Hoster erhalten Sicherheitsupdates des Server-Images automatisch über `deploy/update.sh` und werden zusätzlich in der App informiert.

## Geltungsbereich

Im Geltungsbereich: der Code in diesem Repository, die offiziellen Releases (APK, Play-Store-App) und das offizielle Image `ghcr.io/relexx/familyhome-server`.

Nicht im Geltungsbereich:
- Fehlkonfigurationen einzelner Selbst-Hoster;
- Angriffe, die physischen Zugriff auf ein entsperrtes Gerät voraussetzen;
- Social Engineering.

## Sicherheitskonzept

Siehe [docs/SICHERHEIT.md](docs/SICHERHEIT.md) und das Bedrohungsmodell in [docs/THREAT-MODEL.md](docs/THREAT-MODEL.md).
