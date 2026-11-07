# Beställ film för Emby

Ett Emby-plugin som låter tittare beställa filmer direkt från Embys TV-appar (Android TV, LG, Apple TV). Beställningen görs i tittarens eget konto i [Seerr](https://docs.seerr.dev/), som skickar den vidare till Radarr.

- **Beställningskanalen** "Beställ film": förslagslistor (Populärt, Trendande, Kommande) och tittarens egna beställningar med status.
- **Platshållare**: tusentals beställningsbara filmer i ett eget bibliotek, märkta med "(beställ)" och ett band på omslaget, så att de hittas med Embys vanliga sök.
- **Spela = beställ**: en kort "Beställd!"-video visas och beställningen hamnar i Seerr. Om Seerr är nere visas "Inte tillgängligt just nu" i stället.

Begrepp finns i [CONTEXT.md](CONTEXT.md) och viktiga beslut i [docs/adr](docs/adr).

## Krav

- Emby Server 4.8 eller senare
- Seerr, med Emby som medieserver och Radarr kopplat
- .NET SDK 8 för att bygga

## Hämta färdigbyggt plugin

GitHub bygger pluginet automatiskt vid varje uppladdning. Öppna fliken **Actions**, välj senaste körningen av "Bygg" och ladda ner `Emby-Seerr` under Artifacts (en zip med `Emby-Seerr.dll`). Taggar som börjar på `v` (t.ex. `v1.0.0`) skapar också en release med `Emby-Seerr.dll` bifogad.

## Bygga själv

```
dotnet build src/Bestall/Bestall.csproj -c Release
dotnet test tests/Bestall.Tests
```

Ändra versionen av `mediabrowser.server.core` i `src/Bestall/Bestall.csproj` om din server inte är 4.8.

## Installera

1. Kopiera `src/Bestall/bin/Release/netstandard2.0/Emby-Seerr.dll` till Embys `plugins`-mapp och starta om Emby.
2. **Seerr**: importera de Emby-användare som ska få beställa (Användare → Importera Emby-användare). Bara de blir tittare. Kopiera API-nyckeln från Inställningar → Allmänt.
3. **Pluginet**: Dashboard → Plugins → Beställ film. Fyll i Seerr-adress, API-nyckel och en tom mapp för platshållare, t.ex. `/media/bestall`.
4. **Bibliotek**: lägg till platshållarmappen som ett nytt bibliotek av typen Filmer, med namnet "Beställ film". I bibliotekets inställningar:
   - stäng av alla nedladdare av metadata och bilder från nätet,
   - låt NFO vara på som läsare av lokal metadata,
   - låt realtidsövervakning vara på.
5. **Behörighet**: i Embys användarinställningar, ge bara tittare (användare med Seerr-konto) tillgång till biblioteket "Beställ film". Beställningskanalen döljs automatiskt för andra, men biblioteket måste begränsas för hand.
6. **Första synken**: Dashboard → Schemalagda uppgifter → "Uppdatera platshållare för beställning" → kör. Första gången skapas alla platshållare och omslag hämtas, vilket kan ta 30–60 minuter. Sedan körs den varje natt kl. 04.00 och ändrar bara det som ändrats.

## Inställningar

| Inställning | Standard | Betydelse |
|---|---|---|
| Minsta antal röster på TMDB | 300 | Lägre värde ger ett större urval |
| Högsta antal platshållare | 7 500 | Tak för urvalet (max 10 000) |
| Filmer per förslagslista | 40 | Antal filmer i varje lista i kanalen |
| Språk | sv | Språk för titlar och handling |
| Länder för åldersgräns | US,SE | Embys föräldrakontroll förstår US-gränser bäst |

## Felsökning

Allt pluginet gör loggas under namnet `Bestall` i Embys logg (Dashboard → Loggar). Sök på "Beställning av tmdb" för att se varje beställning, vem som gjorde den och från vilken app.

## Att kontrollera vid första installationen

Pluginet är skrivet mot Embys plugin-API ur dokumentation och erfarenhet, men inte kompilerat eller provat mot en riktig server. Kontrollera särskilt:

- att bygget går igenom mot din Emby-version (namn i Embys API kan skilja mellan versioner),
- att beställningskanalen syns i alla tre TV-apparna,
- att det blir en beställning när du spelar både från kanalen och från en platshållare, i varje app (loggraden "Beställning av tmdb …"),
- att omslagen får bandet "BESTÄLL" (annars finns en varning i loggen om SkiaSharp),
- att TV-apparna, särskilt Apple TV, bläddrar och söker smidigt med tusentals platshållare.
