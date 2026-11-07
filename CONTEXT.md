# Emby-beställningar

Låter tittare hitta och beställa filmer direkt från Embys TV-appar, utan att lämna Emby. Beställningen går via Seerr, som skickar den vidare till Radarr för nedladdning.

## Language

### Aktörer

**Tittare**:
En Emby-användare som tittar via en TV-app och kan beställa filmer. Kopplad till ett eget konto i Seerr.
_Avoid_: Användare, kund, medlem

**Seerr**:
Den beställningstjänst som tar emot beställningar, kontrollerar behörighet och vad som redan finns, och skickar godkända filmbeställningar till Radarr.
_Avoid_: Overseerr, Jellyseerr

### Beställa

**Beställning**:
En tittares önskan om att en film ska läggas till i biblioteket, registrerad i Seerr i tittarens namn. Följer Seerrs regler för godkännande.
_Avoid_: Förfrågan, request, nedladdning

**Beställningsbar film**:
En film som inte redan finns i Emby och inte redan är beställd. Bara sådana visas i beställningskanalen och som platshållare.
_Avoid_: Saknad film, ny film

**Beställningsstatus**:
Var en beställning befinner sig, så som Seerr anger det: väntar på godkännande, godkänd, laddas ner, avslagen.
_Avoid_: Progress, tillstånd

### Ytor i Emby

**Beställningskanal**:
En kanal i Emby där tittaren bläddrar bland beställningsbara filmer i förslagslistor och ser sina egna beställningar.
_Avoid_: Beställningsvy, Seerr-kanalen, meny

**Förslagslista**:
En rad i beställningskanalen med beställningsbara filmer hämtade från Seerr, t.ex. populärt, trendande, kommande.
_Avoid_: Kategori, upptäck-lista

**Mina beställningar**:
En rad i beställningskanalen med tittarens egna pågående beställningar och deras beställningsstatus.
_Avoid_: Kö, beställningshistorik

**Platshållare**:
En beställningsbar film som visas som ett objekt i ett eget Emby-bibliotek, så att den går att hitta via Embys vanliga sök. Är synligt märkt så att den inte kan förväxlas med en film som finns. Att spela den gör en beställning.
_Avoid_: Spökfilm, dummy, stub

**Urval**:
Den mängd beställningsbara filmer som just nu finns som platshållare: förslagslistorna plus alla filmer över en inställbar popularitetsgräns. Avgör vad som går att hitta via sök.
_Avoid_: Katalog, sökindex
