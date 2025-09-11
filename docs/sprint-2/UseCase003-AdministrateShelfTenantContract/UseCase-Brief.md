# Use Case 003 brief - Administrere kontrakter for reol lejere

**Use case name:** UseCase003-AdministrateShelfTenantContract
**Scope:** Systemet er under udvikling
**Level:** User goal
**Primary Actors:** Ejer
**Stakeholders and Interests:** 
- Ejer ønsker at administrere kontrakter for reol lejere i systemet
- Lejere ønsker at se og administrere deres kontrakter

**Preconditions:**  
- Ejer er logget ind og har åbnet Windows-applikationen
- Hvis en kontrakt redigeres, findes mindst én reoltype og én lejer i systemet

**Postconditions:**
- Hvis en kontrakt er oprettet, slettet eller redigeret, er ændringerne gemt i databasen og vises i kontraktlisten.
- Hvis en kontrakt er oprettet, er den tilknyttet den valgte lejer og reoltype.

**Main success scenario:**
1. Aktøren åbner programmet og navigerer til administrations -> kontrakter
1. Systemet viser en formular med felter for lejer, reoltype, startdato og slutdato
1. Systemet viser en liste over eksisterende kontrakter med mulighed for at redigere eller slette hver kontrakt.
1. Hvis aktøren vælger at udfylde formularen, kan aktøren vælger tilføj ny ordrelinje.
1. Aktøren vælger en reol og reoltype fra dropdown-menuer og indtaster start- og slutdatoer.
1. Aktøren klikker "Tilføj"
1. Aktøren gentager trin 4-6 for hver ordrelinje, der skal tilføjes
1. I listen ordrelinjer vises en ny linje med de valgte oplysninger
1. Aktøren klikker "Gem kontrakt"
1. Systemet viser en bekræftelse og opdaterer kontraktlisten.
1. Aktøren ser den nye kontrakt i listen
1. Hvis aktøren vælger en kontrakt fra listen, udfyldes formularen med kontraktens oplysninger.
1. Listen med ordrelinjer opdateres for at vise de tilknyttede ordrelinjer.
1. Aktøren kan redigere eller slette ordrelinjer fra listen.
1. Aktøren redigerer oplysningerne og klikker "Gem kontrakt"
1. Systemet viser en bekræftelse og opdaterer kontraktlisten.
1. Aktøren gentager trin 12-16 for hver kontrakt der skal redigeres.
1. Hvis aktøren vælger en kontrakt fra listen og klikker på ikonet "papirkurv", bliver kontrakten markeret til sletning.
1. Aktøren klikker "Bekræft sletning"
1. Systemet fjerner kontrakten fra databasen og opdaterer listen.
1. Systemet viser en bekræftelse på sletningen.
1. Aktøren gentager trin 18-20 for hver kontrakt der skal slettes.
1. Aktøren går tilbage til forsiden ved at klikke på "Forside" knappen
