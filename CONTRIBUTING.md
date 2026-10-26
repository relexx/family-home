# Mitmachen bei FamilyHome

Danke für dein Interesse! FamilyHome steht unter der [Apache License 2.0](LICENSE).

## Developer Certificate of Origin (DCO)

Statt eines CLA nutzen wir das [DCO](https://developercertificate.org/). Jeder Commit braucht eine Sign-off-Zeile:

```bash
git commit -s -m "Beschreibung"
```

Mit dem Sign-off bestätigst du, dass du den Beitrag unter der Projektlizenz einreichen darfst.

## Ablauf

1. Issue eröffnen oder an ein bestehendes anknüpfen. **Sicherheitslücken bitte nie öffentlich** melden, sondern wie in [SECURITY.md](SECURITY.md) beschrieben.
2. Feature-Branch anlegen und Commits **signieren** (GPG/SSH) und mit Sign-off versehen.
3. **Security Gate lokal ausführen**, es muss grün sein:

   ```bash
   scripts/security-gate.sh
   ```

4. PR öffnen und die Checkliste im PR-Template ausfüllen. Bei Architekturänderungen gehört eine Aktualisierung von [docs/THREAT-MODEL.md](docs/THREAT-MODEL.md) dazu.

## Grundregeln

- Maßgeblich sind [docs/SPEZIFIKATION.md](docs/SPEZIFIKATION.md) und [docs/SICHERHEIT.md](docs/SICHERHEIT.md).
- Keine Tracking-, Werbe- oder Analyse-SDKs.
- Neue Abhängigkeiten brauchen eine Begründung und eine Apache-2.0-kompatible Lizenz.
