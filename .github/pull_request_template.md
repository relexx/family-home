## Was und warum

<!-- Kurz: Was ändert sich, welches Issue/Kapitel der Spezifikation? -->

## Security Gate (docs/SICHERHEIT.md Kap. 7)

- [ ] `scripts/security-gate.sh` lokal grün
- [ ] Keine neuen Geheimnisse, Tokens oder echten personenbezogenen Daten im Diff oder in Testdaten
- [ ] Neue Endpunkte verlangen Autorisierung und sind im Endpunkt-Inventar-Test erfasst (öffentliche nur mit Begründung)
- [ ] Rechte pro Objekt über `PermissionRules` (deny by default), Eingaben validiert
- [ ] Neue Abhängigkeit? → begründet, Lizenz Apache-2.0-kompatibel, Lock-Dateien aktualisiert
- [ ] **Sicherheitsrelevant** (Auth, Rechte, Krypto, Zertifikate, Pipeline, Deployment)? → hier markieren: ☐ ja

### Nur bei Architektur-/Schnittstellenänderungen

- [ ] `docs/THREAT-MODEL.md` fortgeschrieben (Datenflüsse, Vertrauensgrenzen, STRIDE)
- [ ] ASVS-L2- (Server) bzw. MASVS-L2-Punkte (App) geprüft
- [ ] Neue personenbezogene Felder klassifiziert (normal/sensibel), Verschlüsselung, Sichtbarkeit und Löschung beschrieben
- [ ] Ergebnis von `/security-review` (Claude Code) bzw. manuelles Security-Review angehängt
- [ ] App↔Server-Kompatibilität: alte Server (6 Monate) bzw. alte Apps funktionieren weiter oder bekommen eine saubere Mindestversion
