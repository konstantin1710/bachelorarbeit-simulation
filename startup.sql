CREATE TABLE Lagerplaetze (
  PlatzId INT,
  Kurzbezeichnung VARCHAR(20),
  IsBodenzone SMALLINT,
  X DECIMAL,
  Y DECIMAL,
  Distance DECIMAL,
  "Class" INT,
  PRIMARY KEY(PlatzId)
);

CREATE TABLE Artikel(
  Artikelnummer INT,
  Variante INT,
	Length DECIMAL,
  Width DECIMAL,
	Height DECIMAL,
	FlaecheMax DECIMAL,
	FlaecheMin DECIMAL,
  PalletSize INT,
  "Rank" INT,
  "Class" INT,
  PRIMARY KEY(Artikelnummer, Variante)
);

CREATE TABLE Pickpool (
	PlatzId INT,
	Menge INT,
	BelPosId INT,
	BelId INT,
	Artikelnummer INT,
	Variante INT,
	PicklistenId INT,
	Pickzeit TIMESTAMP,
	Liefertermin TIMESTAMP,
	CONSTRAINT fk_lagerplaetze
      FOREIGN KEY(platzId)
	    REFERENCES Lagerplaetze(platzId),
	CONSTRAINT fk_articles
      FOREIGN KEY(Artikelnummer, Variante)
	    REFERENCES Artikel(Artikelnummer, Variante)
);

CREATE TABLE Bestaende(
  PlatzId INT,
  Artikelnummer INT,
  Variante INT,
  Menge INT,
  PRIMARY KEY(PlatzId, Artikelnummer, Variante),
  CONSTRAINT fk_lagerplaetze
      FOREIGN KEY(platzId)
	    REFERENCES Lagerplaetze(platzId),
	CONSTRAINT fk_articles
      FOREIGN KEY(Artikelnummer, Variante)
	    REFERENCES Artikel(Artikelnummer, Variante),
  CONSTRAINT menge_nonnegative
      CHECK (Menge >= 0)
);

CREATE TABLE Reservation (
  PlatzId INT,
  Artikelnummer INT,
  Variante INT,
  Menge INT,
  PRIMARY KEY(PlatzId, Artikelnummer, Variante),
  CONSTRAINT fk_lagerplaetze
      FOREIGN KEY(platzId)
	    REFERENCES Lagerplaetze(platzId),
	CONSTRAINT fk_articles
      FOREIGN KEY(Artikelnummer, Variante)
	    REFERENCES Artikel(Artikelnummer, Variante),
  CONSTRAINT menge_nonnegative
      CHECK (Menge >= 0)
);

create index idx_lagerplaetze_distance_kurzbezeichnung
on lagerplaetze(distance, kurzbezeichnung);

create index idx_lagerplaetze_distance_kurzbezeichnung_class
on lagerplaetze(distance, kurzbezeichnung, "class");

CREATE INDEX idx_artikel_rank
ON artikel("rank");

CREATE INDEX idx_pickpool_pickzeit
ON pickpool(pickzeit);

CREATE INDEX idx_pickpool_liefertermin
ON pickpool(liefertermin);

CREATE INDEX idx_pickpool_liefertermin_pickzeit
ON pickpool(liefertermin,pickzeit);

CREATE SCHEMA dumps;

CREATE TABLE dumps.khklagerplatzbuchungen (
	buchungsid int4 NULL,
	artikelnummer int4 NULL,
	variante int4 NULL,
	herkunftslpkennung int4 NULL,
	ziellpkennung int4 NULL,
	menge float4 NULL,
	bewegungsdatum date NULL
);

CREATE TABLE dumps.verkaufszahlen (
	artikelnummer int4 NULL,
	variante int4 NULL,
	tag int4 NULL,
	monat int4 NULL,
	jahr int4 NULL,
	menge int4 NULL,
  datum date NULL
);

CREATE INDEX idx_lagerplatzbuchungen_bewegungsdatum ON dumps.khklagerplatzbuchungen (bewegungsdatum);

CREATE INDEX idx_lagerplatzbuchungen_ziellpkennung ON dumps.khklagerplatzbuchungen (ZielLPKennung);

CREATE INDEX idx_lagerplatzbuchungen_bewegungsdatum_ziellpkennung ON dumps.khklagerplatzbuchungen (bewegungsdatum,ZielLPKennung);

CREATE INDEX idx_lagerplatzbuchungen_bewegungsdatum_herkunftslpkennung ON dumps.khklagerplatzbuchungen (bewegungsdatum,HerkunftsLPKennung);

CREATE INDEX idx_lagerplatzbuchungen_herkunftslpkennung ON dumps.khklagerplatzbuchungen(herkunftslpkennung);

CREATE INDEX idx_lagerplatzbuchungen_artikelnummer_variante ON dumps.khklagerplatzbuchungen (artikelnummer, variante);

CREATE INDEX idx_verkaufszahlen_artikelnummer_variante ON dumps.verkaufszahlen (artikelnummer, variante);

CREATE INDEX idx_verkaufszahlen_artikelnummer_variante_menge ON dumps.verkaufszahlen (artikelnummer, variante, menge);

CREATE INDEX idx_verkaufszahlen_menge ON dumps.verkaufszahlen (menge);

CREATE INDEX idx_verkaufszahlen_datum ON dumps.verkaufszahlen (datum);

update dumps.verkaufszahlen set datum = make_date(jahr,monat,tag)

create function delete_bestaende_null()
    returns trigger
    language plpgsql
    as $$
    begin
        delete from public.bestaende where menge = 0;
        return null;
    end;
    $$;

create or replace trigger bestaende_zero
   after update on public.bestaende
   execute procedure delete_bestaende_null();


create function delete_reservation_null()
   returns trigger
   language plpgsql
   as $$
   begin
       delete from public.reservation where menge = 0;
       return null;
   end;
   $$;

create or replace trigger reservation_zero
  after update on public.reservation
  execute procedure delete_reservation_null();


CREATE INDEX idx_bestaende_artikel_variante_menge ON bestaende (artikelnummer, variante, menge);
CREATE INDEX idx_lagerplaetze_platzid_kurzbezeichnung_isbodenzone ON lagerplaetze (platzid, kurzbezeichnung, isbodenzone);
CREATE INDEX idx_reservation_platzid_artikel_variante_menge ON reservation (platzid, artikelnummer, variante, menge);
CREATE INDEX idx_reservation_platzid_artikel_variante ON reservation (platzid, artikelnummer, variante);
