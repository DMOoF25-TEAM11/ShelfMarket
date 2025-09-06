# Use Case 001 brief - Administrere reoler

**Use case name:** UseCase001-AddANewShelf
**Scope:** Systemet er under udvikling  
**Level:** User goal  
**Primary Actors:** Ejer  
**Stakeholders and Interests:** 
- Ejer ønsker at adminstrere reoler i systemet
- Personale ønsker at se opdaterede reoloplysninger
- Kunder (lejere) ønsker at tilpasse deres lejeaftaler baseret på tilgængelige reoler
- Kunder (lejere) ønsker at ændre reoltype og placering

**Preconditions:** Ejer er logget ind og har åbnet Windows-applikationen  
**Postconditions:**
- En ny reol er tilføjet til systemet  
- En reol slettes fra systemet
- En reol er redigeret i listen
- Reollisten er opdateret

**Main success scenario:**  
1. Ejer åbner programmet på navigerer til administrations -> reol
1. Systemet viser en formular med felter for placering og type
1. Ejeren indtaster oplysninger og klikker “Tilføj”
1. Systemet validerer input
1. Systemet opretter reolen og gemmer den i databasen.
1. Systemet viser en bekræftelse og opdaterer reollisten.
1. Ejer ser den nye reol i listen
1. Ejer vælger en reol fra listen og klikker på ikonet "papirkurv"
1. Systemet fjerner reolen fra databasen og opdaterer listen.
1. Systemet viser en bekræftelse på sletningen.
1. Ejer vælger en reol fra listen og formularen udfyldes med reolens oplysninger
1. Ejer redigerer oplysningerne og klikker "Gem"
1. Systemet validerer input
1. Systemet opdaterer reolen i databasen.
1. Systemet viser en bekræftelse og opdaterer reollisten.
1. Ejer ser de opdaterede oplysninger i listen
1. Ejer går tilbage til forsiden ved at klikke på "Forside" knappen
