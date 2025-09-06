# Use Case 001 causal

**Use case name:** Administrere reoler  
**Scope:** Systemet er under udvikling  
**Level:** User goal  
**Primary Actors:** Ejer  
**Stakeholders and Interests:** 
- Ejer ønsker at adminstrere reol typer i systemet
- Personale ønsker at se opdaterede reoloplysninger
- Kunder (lejere) ønsker at ændre reoltype.

**Preconditions:** 
- Ejer er logget ind og har åbnet Windows-applikationen  
- Ejer har navigeret til administrations -> reol typer
**Postconditions:**  
- hvis reoltype er tilføjet, slettet eller redigeret, er ændringerne gemt i databasen og vises i reoltypelisten.

**Main success scenario:**  
1. Ejer åbner programmet og navigerer til administrations -> reol typer
1. Systemet viser en formular med felter for navn, beskrivelse.
1. Systemet viser en liste over eksisterende reol typer med mulighed for at redigere eller slette hver type.
1. Ejeren indtaster oplysninger og klikker “Tilføj”
1. Systemet validerer input (tjekker at type er valgt, placering er udfyldt og unik)
1. Systemet opretter reol typen og gemmer den i databasen
1. Systemet viser en bekræftelse og opdaterer reollisten
1. Ejer ser den nye reol type i listen
1. Ejer vælger en reol type fra listen og klikker på ikonet "papirkurv"
1. Systemet fjerner reol typen fra databasen og opdaterer listen
1. Systemet viser en bekræftelse på sletningen
1. Ejer vælger en reol type fra listen og formularen udfyldes med reol type oplysningerne
1. Ejer redigerer oplysningerne og klikker "Gem"
1. Systemet validerer input
1. Systemet opdaterer reol typen i databasen
1. Systemet viser en bekræftelse og opdaterer reol typelisten
1. Ejer ser de opdaterede oplysninger i listen
1. Ejer går tilbage til forsiden ved at klikke på "Forside" knappen

**Extensions (alternatives):**  
- 5a. Type er ikke unik:  
  - Systemet viser en fejlmeddelelse og beder ejeren vælge en anden navn for reol type.  
- 6a/15a. Systemfejl ved oprettelse eller opdatering (f.eks. databasefejl):  
  - Systemet viser en fejlmeddelelse og beder ejeren prøve igen senere.  
  - Fejlen logges til teknisk support.  
- 10a. Systemfejl ved sletning:  
  - Systemet viser en fejlmeddelelse og beder ejeren prøve igen senere.  
  - Fejlen logges til teknisk support.  