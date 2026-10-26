# FamilyHome – Bedrohungsmodell (STRIDE)

Stand: 2026-10-26 · Wird bei **jeder** Architektur- oder Schnittstellenänderung fortgeschrieben (docs/SICHERHEIT.md Kap. 7.1).

## Vertrauensgrenzen

```
[Internet] ─TB1─ [Caddy: TLS + Gerätezertifikat] ─TB2─ [Server (internes Netz)] ─TB3─ [PostgreSQL (internes Netz)]
                                                          └─TB4─ [externe CalDAV/CardDAV/ICS-Server]
[App-Prozess] ─TB5─ [Android Keystore / SQLCipher-DB]     [GitHub CI] ─TB6─ [GHCR / Releases] ─TB7─ [update.sh / Updater]
```

## Risiken

| # | Grenze | STRIDE | Bedrohung | Gegenmaßnahme | Status |
|---|---|---|---|---|---|
| 1 | TB1 | S | Fremdes Gerät gibt sich als Familiengerät aus | mTLS mit Gerätezertifikat der Familien-CA, Schlüssel nicht exportierbar im Keystore | Phase 0 |
| 2 | TB1 | D | Überlastung bzw. Brute Force auf den Join-Endpunkt | Join nur bei offener Einladung, Rate Limit, Einmal-Code, Bestätigung durch Eltern | Phase 0 |
| 3 | TB2 | S/E | Gefälschter Zertifikat- oder Forwarded-Header umgeht mTLS | Server nur im internen Netz; Header nur von der festen Proxy-IP akzeptiert (Filter läuft vor `UseForwardedHeaders`); Caddy überschreibt ihn; Server prüft das Zertifikat erneut gegen CA und Gerätetabelle | **umgesetzt + getestet** |
| 4 | TB2 | E | Kind ändert fremde Termine oder Notfallkarte | `PermissionRules` pro Objekt, Endpunkt-Inventar-Test, Rechte-Matrix-Tests | teilweise |
| 5 | TB3 | T/I | SQL-Injection | Nur EF Core/parametrisiert; `FromSqlRaw`/`ExecuteSqlRaw` verboten (BannedApiAnalyzers); App-Rolle ohne DDL | **umgesetzt** (Analyzer) |
| 6 | TB4 | I/T | Bösartige iCal/vCard-Antwort (Parser-Exploit, riesige Antwort) | Größen- und Zeitlimits, Fuzzing, keine Ausführung von Inhalten | Phase 1 |
| 7 | TB4 | I | Abfluss von DAV-Passwörtern | Data-Protection-Verschlüsselung, nie an Clients, Step-up zum Ansehen | Phase 1 |
| 8 | TB5 | I | Gestohlenes Handy | SQLCipher, Keystore, Gerät aus der Ferne sperrbar, kurze Zertifikatslaufzeit (7 Tage) | Phase 1 |
| 9 | TB6 | T | Kompromittierte Abhängigkeit oder Action | Lock-Dateien, Source Mapping, SHA-gepinnte Actions, zizmor, Dependency Review, NuGet-Audit | **umgesetzt** |
| 10 | TB7 | T | Gefälschtes Server-Image oder gefälschte APK | cosign-Prüfung gegen die Workflow-Identität, Ed25519-Manifest, Android-Signaturprüfung | teilweise |
| 11 | alle | R | Bestreiten kritischer Aktionen | Audit-Log (nur anhängen) mit Gerät und Account | Phase 0 |
