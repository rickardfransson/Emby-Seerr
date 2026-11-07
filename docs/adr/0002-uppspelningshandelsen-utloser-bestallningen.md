# Uppspelningshändelsen utlöser beställningen

En beställning görs när Emby rapporterar att en tittare börjat spela en platshållare eller en film i beställningskanalen (`ISessionManager.PlaybackStart`), inte när platshållarens videoadress anropas eller när kanalen lämnar ut sin video. Det är det enda stället där vi vet både vilken film och vilken tittare det gäller, och beställningen ska göras i tittarens eget Seerr-konto.

## Consequences

- Videoadressen (`/Bestall/Video`) gör aldrig en beställning. Det är avsiktligt: Emby kan anropa den vid biblioteksskanning, och där finns ingen tittare.
- Om en framtida Emby-app inte rapporterar uppspelning till servern blir det ingen beställning från den appen, trots att "Beställd!" visas.
