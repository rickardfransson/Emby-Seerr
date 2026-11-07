# Platshållare som filer på disk med egen NFO

Varje platshållare är en mapp med en `.strm`, en `.nfo` och ett omslag, skriven av pluginet i en egen mapp som läggs till som ett vanligt filmbibliotek. Det fungerar med Embys vanliga sök och föräldrakontroll i alla appar utan att pluginet behöver skriva direkt i Embys databas, och NFO-filen gör att Emby inte hämtar information från nätet för tusentals filmer.

## Considered Options

- **Virtuella objekt direkt i Embys databas**: ingen disk, men odokumenterat, och riskerar att gå sönder vid Emby-uppdateringar.
- **Låta Emby hämta information själv**: enklare, men tusentals onödiga anrop vid varje uppdatering och titeln skulle skrivas över utan "(beställ)".

## Consequences

- Biblioteket måste ställas in för att läsa NFO och inte hämta från nätet (se README).
- Första synken skriver alla platshållare och hämtar tusentals omslag; senare synkar ändrar bara skillnaden.
