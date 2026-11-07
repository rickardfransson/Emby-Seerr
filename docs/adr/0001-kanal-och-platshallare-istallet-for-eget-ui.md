# Beställning via kanal och platshållare i stället för eget gränssnitt

Tittarna beställer från Embys TV-appar (Android TV, LG, Apple TV), och där kan ett plugin inte lägga till egna knappar, sidor eller sökrutor. Vi använder därför bara sådant appen redan visar: en beställningskanal för att bläddra bland förslag, plus platshållare i ett eget bibliotek så att ett urval filmer syns i Embys vanliga sök. Att spela en film som inte finns är det som gör en beställning.

## Considered Options

- **Injicerad JavaScript i webbklienten**: ger ett riktigt sökfält, men fungerar inte i TV-apparna och går sönder vid Emby-uppdateringar.
- **Bara Seerrs egen webbsida**: ingen utveckling, men tittarna lämnar inte soffan och Emby för att använda den, vilket var hela problemet.
- **Bara kanal**: enkel, men fritextsökning i kanaler stöds dåligt i TV-apparna.

## Consequences

- En film utanför urvalet kan inte hittas genom sök från TV:n. Det är en medveten begränsning.
- Eftersom "spela = beställ" utan bekräftelse är en felbeställning möjlig; vi förlitar oss på Seerrs dubblettskydd och möjlighet att avslå i stället för ett bekräftelsesteg.
