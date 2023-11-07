# Installationsanweisung

1. Repository klonen
2. Datenbank erstellen (siehe Punkt "[Aufbau der Datenbank](https://github.com/konstantin1710/bachelorarbeit-simulation#aufbau-der-datenbank)")
3. In Ordner "simulation" eine Datei "connectionstrings.json" nach folgendem Schema erstellen:

```
{
  "ConnectionStrings": {
    "LocalPostgres": "Host=localhost;Port=1896;Username=admin;Password=password;Database=db;Enlist=true;MaxPoolSize=500;IncludeErrorDetail=True;Timeout=60"
  }
}
```
4. Im ConnectionString die Einträge `Host`, `Port`, `Username`, `Password` und `Database` anpassen. 
Die übrigen Einträge sind für die parallele Abarbeitung bestimmter Schleifen innerhalb der Simulation notwendig.
5. Projekt ausführen (z.B. in Visual Studio)
6. Aufruf der Endpunkte über localhost:port, im Browser wird die Oberfläche von [Swagger](https://swagger.io/) benutzerfreundlich angezeigt

# Endpunkte

Die Simulation stellt mehrere Endpunkt zur Verfügung, die im Laufe der Bearbeitung der Arbeit verwendet wurden. Um die Simulation auszuführen sind folgende Endpunkte notwendig:

### /api/article/calculate-pallet-sizes

Der Endpunkt nutzt die Tabellen `artikel` und `dumps.khklagerplatzbuchungen`, um anhand von allen vergangenen Buchungen die Größe einer Palette zu berechnen. Die Ergebnisse werden im Feld `palletsize` der Tabelle `artikel` gespeichert.
Wenn die Palettengrößen im Datensatz nicht vorhanden sind, muss der Endpunkt vor der Simulation einmal aufgerufen werden.

### /api/simulation/simulate-picking

Hier wird die eigentliche Simulation ausgeführt. Die Parameter müssen folgendermaßen gesetzt werden:

- `Strategy` (enum): Strategie der Einlagerung. Mögliche Werte sind: `Current` (aktuelle Strategie), `Random` (zufällige Strategie), `RandomWithPreferredGroundZone` (Strategie mit bevorzugter Bodenzone), `PreferredLowDistance` (Bevorzugt geringe Distanz), `DistanceBySalesRank` (Verkaufszahlenstrategie), `Classes` (Klassenstrategie)
- `Date` (DateTime): Datum des gewünschten Starttages der Simulation
- `NumberOfDays` (int): Anzahl an Tagen, die simuliert werden sollen
- `BetterPicklists` (bool): Gibt an, ob bessere Picklisten mit Berücksichtigung der lokalen Zusammenhänge von Lagerplätzen erstellt werden sollen
- `NumberOfClasses` (int): Anzahl der Klassen für Classes-Strategy. Optional für alle anderen Strategien, Standardwert 2
- `ExactForecast` (bool): Gibt an, ob eine perfekte Verkaufsprognose verwendet werden soll
- `OptimizedGroundzone` (bool): Nur in Verbindung mit Verkaufszahlenstrategie. Gibt an, ob die verbesserte Verteilung in die Bodenzone genutzt werden soll (Kapitel 5.5.3)

# Aufbau der Datenbank

Die Simulation nutzt eine Postgres-Datenbank. Damit alle Abfragen funktionieren, muss die Datei `startup.sql` in einer Postgres-Datenbank ausgeführt werden.
Der Datensatz ist folgendermaßen aufgebaut:

1. Schema `public`:

![public](https://github.com/konstantin1710/bachelorarbeit-simulation/assets/87207595/205c2b1e-9caa-4613-9829-8ab2b26a3793)

#### Artikel
Hier sind die Artikel gespeichert. Daten müssen vor einem Simulationsdurchlauf vorhanden sein.

- `Artikelnummer`: Id des Artikels
- `Variante`: Variantennummer des Artikels, standardmäßig 0
- `Length`: Länge des Artikels
- `Width`: Breite des Artikels
- `Height`: Höhe des Artikels
- `FlaecheMax`: Größte Seitenfläche des Artikels
- `FlaecheMin`: Kleinste Seitenfläche des Artikels
- `PalletSize`: Anzahl an Artikeln, die maximale auf eine Palette passen
- `Class`: Klasse des Artikels, verwendet für Klassenstrategie (wird am Anfang der Berechnung gesetzt)

#### Lagerplaetze
Hier sind die Lagerplätze gespeichert. Daten müssen vor einem Simulationsdurchlauf vorhanden sein.

- `PlatzId`: Id des Lagerplatzes
- `Kurzbezeichnung`: Bezeichnung des Lagerplatzes nach Schema "<Unit>;<Gang>;<Lagerplatz>;<Ebene>"
- `IsBodenzone`: 1 wenn der Lagerplatz in der Bodenzone liegt, sonst 0
- `X`: X-Koordinate des Lagerplatzes
- `Y`: Y-Koordinate des Lagerplatzes
- `Distance`: Distanz des Lagerplatzes zum Depot
- `Class`: Klasse des Lagerplatzes, verwendet für Klassenstrategie (wird am Anfang der Berechnung gesetzt)

#### Reservation
Wird für die Reservierungen verwendet und automatisch für jeden Simulationstag gesetzt.

- `PlatzId`: Id des Lagerplatzes (Referenz auf Lagerplaetze)
- `Artikelnummer`: Id des Artikels (Referenz auf Artikel)
- `Variante`: Variantennummer des Artikels (Referenz auf Artikel)
- `Menge`: Menge der reservierten Artikel

#### Pickpool
Tabelle für den historischen Pickpool zur Rekonstruierung des Pickpools in der Simulations. Daten müssen vor einem Simulationsdurchlauf vorhanden sein.

- `PlatzId`: Id des Lagerplatzes (Referenz auf Lagerplaetze)
- `Menge`: Stückzahl
- `BelPosId`: Referenz auf Belegposition des Verkaufsbeleges
- `BelId`: Id des Verkaufsbeleges
- `Artikelnummer`: Id des Artikels (Referenz auf Artikel)
- `Variante`: Variantennummer des Artikels (Referenz auf Artikel)
- `PicklistenId`: Id der historischen Pickliste
- `Pickzeit`: Zeit der historischen Kommissionierung
- `Liefertermin`: Historischer Liefertermin

#### Bestaende
Abbildung der Bestände des Lagers. Wird automatisch verwaltet.

- `PlatzId`: Id des Lagerplatzes (Referenz auf Lagerplaetze)
- `Artikelnummer`: Id des Artikels (Referenz auf Artikel)
- `Variante`: Variantennummer des Artikels (Referenz auf Artikel)
- `Menge`: Menge der vorrätigen Artikel

2. Schema `dumps`:

![dumps](https://github.com/konstantin1710/bachelorarbeit-simulation/assets/87207595/62243d30-7dbc-4184-8d15-9d1aab02abc0)

#### KhkLagerplatzBuchungen
Hier sollten alle historischen Buchungen des Lagers eingetragen sein.

- `BuchungsId`: Id der Buchung
- `Artikelnummer`: Id des Artikels
- `Variante`: Variantennummer des Artikels
- `HerkunftsLpKennung`: Id des Herkunftslagerplatzes der Buchung
- `ZielLpKennung`: Id des Ziellagerplatzes der Buchung
- `Menge`: Menge der gebuchten Artikel
- `Bewegungsdatum`: Datum der Buchung

#### Verkaufszahlen
Historische Verkäufe, zur Aufstellung der benötigten Verkaufsprognosen und Analyse der Verkaufszahlen

- `Artikelnummer`: Id des Artikels
- `Variante`: Variantennummer des Artikels
- `Tag`: Tag des Verkaufs
- `Monat`: Monat des Verkaufs
- `Jahr`: Jahr des Verkaufs
- `Menge`: Verkaufte Menge
- `Datum`: Datum des Verkaufs, Kombination aus Tag, Monat und Jahr
