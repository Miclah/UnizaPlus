[English](README.md) | **Slovenčina**

# UnizaPlus

Osobný rozvrh pre študentov Žilinskej univerzity. Miesto, kde si držíte rozvrh, na ktorý naozaj chodíte, keď sa už nezhoduje s tým, čo vám univerzita vygenerovala v septembri. ASP.NET Core Razor Pages aplikácia na .NET 10.

[![CI](https://github.com/Miclah/UnizaPlus/actions/workflows/ci.yml/badge.svg)](https://github.com/Miclah/UnizaPlus/actions/workflows/ci.yml)
[![Licencia: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)

## Obsah

- [Živé demo](#živé-demo)
- [O projekte](#o-projekte)
- [Funkcie](#funkcie)
- [Screenshoty](#screenshoty)
- [Technológie](#technológie)
- [Architektúra](#architektúra)
- [Režim CSV](#režim-csv)
- [Spustenie lokálne](#spustenie-lokálne)
- [Testy](#testy)
- [Konfigurácia](#konfigurácia)
- [Deployment](#deployment)
- [Odinštalovanie](#odinštalovanie)
- [Licencia](#licencia)
- [Autor](#autor)

## Živé demo

**[miclah-unizaplus.azurewebsites.net](https://miclah-unizaplus.azurewebsites.net)**

Nie je sa kam prihlasovať. Hneď na prvej stránke je rozvrh, načítaný zo vzorových dát, ktoré sú súčasťou aplikácie, a každý návštevník dostane vlastnú kópiu. Môžete v ňom presúvať hodiny, pridávať nové, nechať si vygenerovať iné rozloženie alebo si ho vyexportovať. Nikomu inému sa z toho nič nezobrazí.

Beží na bezplatnej úrovni Azure App Service, ktorá nepodporuje Always On, takže po asi pol hodine bez návštev sa stránka uspí. Prvá požiadavka po takej pauze ju musí zobudiť a chvíľu potrvá. Nie je tu databáza, ktorú by bolo treba budiť spolu s ňou, takže je to otázka chvíle a všetko ďalšie je už okamžité.

![Ukážka](docs/screenshots/walkthrough.gif) <!-- TODO: nahrať krátky GIF: otvorenie rozvrhu, presun jednej hodiny do voľného slotu, generátor, export do .ics -->

## O projekte

Na začiatku každého semestra univerzitný systém na `vzdelavanie.uniza.sk` vygeneruje podľa študijnej skupiny rozvrh a zverejní ho. Na to slúži a spraví to raz.

To, čo príde potom, už nepokrýva. Cvičenie sa presunie na iný termín, laboratórne cvičenie si vymeníme s inou skupinou, predmet sa nakoniec ustáli v inej miestnosti, než bola v pôvodnom pláne. Tieto zmeny sa dohodnú v prvých týždňoch a platia po zvyšok semestra, ale v portáli stále svieti verzia vygenerovaná v septembri. Opravu nie je kam zapísať, takže rozvrh, na ktorý študent naozaj chodí, žije niekde inde: v poznámkach, v ručne prepísanom kalendári v telefóne alebo len v hlave.

UnizaPlus je to niekde inde, len s tým rozdielom, že nezačína prázdnou tabuľkou, ale vygenerovaným rozvrhom. Hodiny sa dajú presúvať, pridávať a mazať, kolízie sa označia hneď ako vzniknú a výsledok odíde ako CSV alebo ako `.ics` kalendár na zadané obdobie semestra. Ak má predmet viac paralelných skupín, generátor vie dopočítať, ktorá ich kombinácia sa prekrýva najmenej.

Vznikol ako univerzitný projekt. Rozhranie je po slovensky aj po anglicky, prepína sa v hlavičke a voľba sa pamätá v cookie.

## Funkcie

- Týždenná mriežka od 7:00 do 20:00, farebne odlíšená podľa typu hodiny, ktorá odreže prázdne hodiny na začiatku a na konci, aby rozvrh zložený z doobedných hodín nekreslil osem prázdnych stĺpcov.
- Presúvanie hodín myšou cez Pointer Events, takže myš, dotyk aj pero idú tou istou cestou v kóde. Presun sa prejaví okamžite a na server sa ukladá na pozadí, s undo/redo zásobníkom na celú session.
- Detekcia prekryvov. Editačný formulár odmietne uložiť hodinu do obsadeného slotu, presun myšou to naopak dovolí a kolíziu iba zvýrazní. Rozdiel je zámerný: keď rozťahujete dve hodiny od seba, jednu z nich musíte spravidla najprv položiť niekam, kde ešte prekáža.
- Generátor rozvrhu, ktorý vyberie po jednej paralelnej skupine z každého predmetu tak, aby výsledok mal čo najmenej kolízií. Voliteľne uprednostní neskoré začiatky, menšie okná medzi hodinami alebo voľný piatok.
- Štatistiky nad mriežkou: počet hodín podľa typu, koľko hodín celkovo padne na okná medzi hodinami, najskorší začiatok, najneskorší koniec a ktoré dni sú voľné.
- Export do CSV alebo do `.ics` podľa RFC 5545, kde sa z každej hodiny stane týždenne sa opakujúca udalosť ohraničená zadanými dátumami semestra.
- Nahranie vlastného CSV, ktorým sa vzorové dáta nahradia skutočným rozvrhom.
- Reset, ktorý vráti vzorový rozvrh, a to iba vo vašej vlastnej session.

## Screenshoty

<!-- TODO: doplniť tieto tri -->

### Týždenná mriežka so štatistikami
![Týždenná mriežka](docs/screenshots/schedule-grid.png)

### Generátor s variantom bez kolízií
![Generátor](docs/screenshots/generate-schedule.png)

### Export s rozsahom dátumov semestra pre .ics
![Export](docs/screenshots/schedule-export.png)

## Technológie

| Vrstva | Technológia | Verzia |
|---|---|---|
| Framework | .NET | 10.0 |
| Web | ASP.NET Core Razor Pages, jeden API kontroler pre presúvanie hodín | 10.0 |
| Frontend | Bootstrap | 5.1.0 |
| Presúvanie hodín | Čistý JavaScript nad Pointer Events API | - |
| Lokalizácia | ASP.NET Core resource súbory, slovenčina a angličtina | 10.0 |
| Scraper | Selenium WebDriver, Selenium.Support (len `UnizaPlusBackEnd`) | 4.46.0 |
| Testy | xUnit s `WebApplicationFactory` | 2.9.3 |
| Test SDK | Microsoft.NET.Test.Sdk | 18.8.1 |
| Hosting | Azure App Service, Linux, F1 Free | - |

V celom riešení nie je databáza ani ORM. Na rozvrhu, ktorý žije počas jednej session v prehliadači, nie je nič, čo by muselo prežiť reštart, takže pridať perzistenciu by znamenalo nasadiť, migrovať a zabezpečiť databázu, v ktorej by nikdy nebolo nič, čo stojí za uchovanie.

## Architektúra

![Architektúra](docs/architecture.svg)

Riešenie tvoria štyri projekty. `UnizaPlus.Web` je samotná aplikácia, `UnizaPlus.Models` drží `ScheduleItem` a prácu s názvami dní, ktoré zdieľa zvyšok, `UnizaPlusBackEnd` je konzolový scraper a `UnizaPlus.Tests` pokrýva všetky tri.

### Prečo je zdroj dát za rozhraním

Pôvodná verzia projektu vedela získať rozvrh jediným spôsobom: cez Selenium prekliknúť skutočný Chrome univerzitným portálom, prihlásiť sa ako študent a prečítať vykreslenú mriežku. Funguje to, ale webová aplikácia je tým pádom nepoužiteľná pre kohokoľvek bez prihlasovacích údajov do UNIZA a nenasaditeľná kdekoľvek, kde nie je prehliadač a sieťová cesta na univerzitu.

`IScheduleProvider` je tam preto, aby zdroj rozvrhu bola otázka konfigurácie, nie štruktúry kódu. Má jedinú metódu, ktorá vráti zoznam `ScheduleItem`, a dve implementácie: `CsvScheduleProvider` číta súbor z disku, `SeleniumScheduleProvider` spustí scraper ako samostatný proces a načíta CSV, ktoré scraper zapíše.

Podstatné je, kde sa rozhoduje. `Program.cs` prečíta pri štarte `UnizaPlus:DataSource` a zaregistruje práve jednu z nich. V predvolenom režime `Csv` sa `SeleniumScheduleProvider` do kontajnera služieb vôbec nedostane, takže ho nemá ako vytiahnuť, vytvoriť ani zavolať žiadna cesta v kóde. To je silnejšie než `if` vnútri jedného providera: scraper nie je vypnutý za behu, jednoducho v grafe objektov nie je. `UnizaPlus.Web` navyše nereferencuje Selenium ako balík, ani priamo, ani tranzitívne, a Docker image nikdy nekopíruje `UnizaPlusBackEnd` do build kontextu, takže závislosti na automatizácii prehliadača v nasadenom kontajneri neležia ani ako mŕtva váha.

Druhá polovica toho rozhodnutia je, že oba providery dodajú vždy len *východiskový stav*. Len čo session svoj rozvrh má, všetko ďalšie, teda presuny, úpravy, nahraté súbory aj vygenerované varianty, beží nad kópiou v pamäti a provider sa už nepýta, pokiaľ si návštevník sám nevyžiada refresh. Vďaka tomu môže jedno rozhranie obslúžiť dva zdroje, ktoré sa správajú úplne inak: čítanie súboru za milisekundu aj sedenie s prehliadačom za pol minúty prebehnú presne raz za návštevníka.

Scraper zostáva v repozitári ako pôvodná cesta k reálnym dátam, overiť sa však bez študentského prístupu do portálu UNIZA nedá.

### Izolácia návštevníkov

`SessionScheduleStore` drží jeden zoznam `ScheduleItem` na každé session ID v `IMemoryCache` s klznou tridsaťminútovou platnosťou. Žiadne prihlásenie ani účet: session začne prvou návštevou a store do nej zapíše jeden zahadzovací kľúč, aby sa session cookie naozaj vydala. ASP.NET Core ju totiž neodošle, kým sa do session niečo neuloží.

Na disk sa nezapisuje nič. Úpravy jedného návštevníka sa nemajú ako dostať do vzorového CSV, k inému návštevníkovi ani do ďalšieho nasadenia, a práve preto sa dá plne editovateľný rozvrh pokojne vystaviť na verejnú adresu bez akejkoľvek autentifikácie. Opustené session vyhodí cache sama, nekopia sa.

### Generátor

`ScheduleGenerator` berie úlohu ako prehľadávanie s obmedzeniami. Hodiny, ktoré majú rovnaký predmet aj typ, ale patria rôznym študijným skupinám, zoskupí do *blokov* zameniteľných alternatív. Čo nemá súrodenca, je pevné a nehýbe sa. Potom backtrackingom prechádza po jednej alternatíve z každého bloku a počíta pritom prekrývajúce sa dvojice.

Na tomto priebežnom počte prehľadávanie aj orezáva. Pridaním hodiny kolízia nikdy nezmizne, takže vo chvíli, keď má rozpracované priradenie viac kolízií než najlepší doteraz nájdený kompletný rozvrh, sa celá vetva zahodí. Ako prvé idú na rad bloky s najmenším počtom alternatív, lebo tie narazia na diskvalifikujúcu kolíziu skôr a dajú orezávaniu s čím pracovať. Tvrdý strop 200 000 navštívených uzlov drží patologický vstup od toho, aby zavesil požiadavku.

Preferencie nikdy nemenia, ktorý rozvrh je platný. Iba zoraďujú tie, ktoré majú rovnaký počet kolízií, a práve preto zapnutý voľný piatok nad rozvrhom, kde sa piatku nedá vyhnúť, stále vráti výsledok namiesto chyby.

`ScheduleGenerator`, `ScheduleOverlapChecker`, `ScheduleGridLayout` aj `ScheduleStatisticsCalculator` berú obyčajné zoznamy a vracajú obyčajné hodnoty, bez závislosti na ASP.NET, takže sa testujú priamo a nie cez webovú vrstvu.

### Klient a server sa zhodnú na rozložení

Presun hodiny prekreslí mriežku v prehliadači bez volania na server, čo znamená, že si JavaScript musí skupiny prekryvov a pozície stĺpcov dopočítať sám. Tá logika je zámerne jedna k jednej prepísaný `ScheduleOverlapChecker` a `ScheduleGridLayout`, takže sa okamžité vykreslenie na klientovi a serverové vykreslenie tých istých dát nemajú ako rozísť. Presun potom ide na `POST /api/schedule/move`, kde sa overí deň aj rozsah hodín, než sa zapíše do session.

## Režim CSV

`Csv` je predvolený režim a jediný, ktorý podporuje demo aj Docker image. Číta `sample-data/schedule.csv`, prípadne to, na čo ukazuje `UnizaPlus:CsvPath`, a je to zároveň formát, ktorý berie stránka „Upload CSV“.

Hlavičkový riadok je povinný. Prítomné musia byť len `Subject`, `Type`, `Day`, `Start` a `End`, zvyšok môže zostať prázdny.

```
Subject,SubjectCode,Type,Day,Start,End,Room,Teacher,Group
Databázové systémy,6BI0101,P,Pondelok,8,10,RA1A3,"doc. Ing. Ján Novák, PhD.",FRI22
```

`Type` je `P` pre prednášku, `C` pre cvičenie a `L` pre laboratórne cvičenie. `Day` berie slovenské aj anglické názvy bez ohľadu na veľkosť písmen, pretože rozhranie bývalo slovenské a súbory z tých čias sa majú dať načítať aj dnes. `Start` a `End` sú celé hodiny od 7 do 21, dĺžka sa dopočíta ako `End - Start` a je zhora obmedzená na štyri hodiny, čo je maximum, ktoré mriežka vykreslí. Polia s čiarkami alebo úvodzovkami sa uzatvárajú do úvodzoviek, ako je pri CSV zvykom.

Parsovanie sa nezosype, iba stráca. Chýbajúci povinný stĺpec zastaví celý súbor s vysvetlením a bez výnimky. Prázdny riadok, neznámy deň, neplatný typ alebo nezmyselný časový rozsah preskočí iba ten jeden riadok a zapíše prečo. Nahratie, z ktorého vyjde nula riadkov *a zároveň* varovania, sa berie ako pokazený súbor a pôvodný rozvrh zostane nedotknutý. Nula riadkov bez varovaní je platný súbor so samotnou hlavičkou a berie sa ako vedomá požiadavka na prázdny rozvrh.

Scraper zapisuje úplne iný formát (`Id,Day,StartHour,Duration,Type,Professor,Classroom,Subject,SubjectCode,StudentGroups,Color`) a oba parsery sú zámerne oddelené. Jeden je verejný výmenný formát, ktorý ľudia píšu a upravujú ručne, druhý je interné odovzdanie dát medzi dvoma procesmi. Zlúčiť ich by znamenalo, že každá budúca zmena vo výstupe scrapera obmedzí to, čo si smie študent naklikať v tabuľkovom procesore.

**Reset** vždy načíta priložený vzorový súbor, nech je `UnizaPlus:DataSource` nastavený akokoľvek. Jeho úlohou je vrátiť návštevníka do známeho stavu, nie znova spustiť ten zdroj, z ktorého začínal. Refresh naopak ide späť na nakonfigurovaný provider.

## Spustenie lokálne

### Bez Dockera

Treba [.NET SDK 10.0](https://dotnet.microsoft.com/download). Nič iné: žiadnu databázu, prehliadač ani prihlasovacie údaje.

```bash
git clone https://github.com/Miclah/UnizaPlus.git
cd UnizaPlus
dotnet run --project UnizaPlus.Web/UnizaPlus.Web.csproj
```

Aplikácia beží na **http://localhost:5021**, prípadne na **https://localhost:7124** s profilom `https`.

### S Dockerom

```bash
cp .env.example .env
docker compose up --build
```

Aplikácia beží na **http://localhost:8080**. Image zostavuje iba `UnizaPlus.Web` a `UnizaPlus.Models`, beží na čistom ASP.NET runtime pod non-root používateľom a neobsahuje Chrome ani chromedriver, keďže režim `Csv` ich nepotrebuje. `docker-compose.yml` pripája `./sample-data` len na čítanie, takže sa demo rozvrh dá vymeniť a kontajner reštartovať bez nového buildu.

Konfiguráciu, ako aj vypnutie, odinštalovanie a odstránenie samotného Dockera, rieši [DOCKER.sk.md](DOCKER.sk.md).

### Režim Live

Režim `Live` ovláda skutočný portál a potrebuje Windows s nainštalovaným Google Chrome, sieťový prístup na `vzdelavanie.uniza.sk` a platné študentské údaje do UNIZA v `UnizaPlus:Live:Username` a `UnizaPlus:Live:Password`. `SeleniumScheduleProvider` hľadá scraper v `UnizaPlusBackEnd/bin/Debug/net10.0/UnizaPlusBackEnd.exe`, takže konzolový projekt musí byť zostavený vopred. Skutočné údaje nikdy necommitujte do `appsettings.json`, použite user secrets alebo premenné prostredia.

## Testy

```bash
dotnet test
```

77 testov v 8 triedach. Pokrývajú parsovanie CSV (platné riadky, slovenské aj anglické názvy dní, prázdne riadky, chýbajúce stĺpce, pokazené časové rozsahy, hraničné hodiny) a zápis CSV vrátane spätného prechodu cez parser; v generátore extrakciu blokov, orezávanie, determinizmus a vplyv každej preferencie na poradie; rozloženie mriežky pri prekrývajúcich sa aj susediacich hodinách; kontrolu prekryvov; mapovanie typov hodín v scraperi; a sadu end-to-end testov, ktoré cez `WebApplicationFactory` poháňajú bežiacu aplikáciu a overujú, že presun v rámci session vydrží, že neplatný deň alebo hodina mimo rozsahu skončia odmietnutím a nič nezmenia, že presun do obsadeného slotu prejde a označí sa ako kolízia a že nahratie nezmyselného súboru nechá pôvodný rozvrh na pokoji.

Testovací krok v CI odfiltruje `Category=RequiresNetwork` a `Category=Selenium`. Dnes tieto vlastnosti nemá žiadny test, takže v CI beží všetko, čo je v repozitári. Filter je tam preto, aby sa budúce testy pre režim Live vedeli odhlásiť cez trait namiesto toho, aby ich niekto zmazal, a aby CI nespadlo len preto, že univerzitná stránka práve nebeží.

## Konfigurácia

Nastavenia prídu z `appsettings.json`, z premenných prostredia alebo lokálne z user secrets. Na Azure App Service sú to application settings zapísané s dvojitým podčiarkovníkom, čiže z `UnizaPlus:DataSource` sa stane `UnizaPlus__DataSource`.

| Nastavenie | Účel |
|---|---|
| `UnizaPlus__DataSource` | `Csv` (predvolené) alebo `Live`. Určuje, ktorý `IScheduleProvider` sa pri štarte zaregistruje |
| `UnizaPlus__CsvPath` | Cesta k CSV súboru pre režim `Csv`. Relatívne cesty sa počítajú od priečinka aplikácie. Predvolene priložené `sample-data/schedule.csv` |
| `UnizaPlus__Live__Username` | Používateľské meno do portálu UNIZA. Povinné v režime `Live`, bez neho refresh zlyhá a session zostane nedotknutá |
| `UnizaPlus__Live__Password` | Heslo do portálu UNIZA. To isté |
| `ASPNETCORE_ENVIRONMENT` | `Development` zapína vývojársku stránku s výnimkami. Čokoľvek nasadené má zostať na `Production` |
| `HOST_PORT` | Len pre Docker Compose. Port na hostiteľovi mapovaný na 8080 v kontajneri |

## Deployment

Demo beží na Azure App Service, Linux, na bezplatnom pláne F1. Infraštruktúru popisuje jediná Bicep šablóna, [azure/main.bicep](azure/main.bicep): App Service plan, web app na runtime `DOTNETCORE|10.0`, iba HTTPS so spodnou hranicou TLS 1.2, a application settings. Žiadnu databázu nedeklaruje, lebo nie je čo uchovávať.

[.github/workflows/ci.yml](.github/workflows/ci.yml) pri každom pushi a pull requeste do `main` spustí restore, build a testy. Push do `main` aplikáciu navyše publikuje a nasadí. V repozitári neleží žiadny prístup do Azure: deploy job sa prihlasuje cez OIDC, teda GitHub vydá pre každý beh krátkodobý token a Azure mu verí vďaka federated credential zviazanej s prostredím `production` tohto repozitára. Tá identita má rolu `Website Contributor` iba na samotnú web app, takže vie nahrať kód, ale nie zmeniť plán alebo zmazať site.

## Odinštalovanie

Mimo priečinka s repozitárom a prípadne úložiska Dockera sa nič neinštaluje.

S Dockerom:

```bash
docker compose down --rmi all
```

Nie je čo mazať z volume: jediný mount je `./sample-data`, čo je bind mount priečinka v repozitári v režime len na čítanie, takže v Docker volume nikdy nič neležalo. Potom zmažte súbor `.env`, ktorý ste si vytvorili, a naklonovaný repozitár.

Bez Dockera je celá práca zmazať naklonovaný repozitár, čím pôjdu preč aj `bin/` a `obj/`. Nie je čo dropovať a mimo priečinka sa nič nezapísalo. Ak ste si pre režim `Live` nastavovali user secrets, vyčistite ich:

```bash
dotnet user-secrets clear --project UnizaPlus.Web/UnizaPlus.Web.csproj
```

NuGet balíky sedia v zdieľanej cache v `~/.nuget/packages` a používa ich každý .NET projekt na počítači, takže ich nechajte tak, pokiaľ ju nechcete zámerne vyprázdniť cez `dotnet nuget locals all --clear`.

## Licencia

MIT License. Pozri [LICENSE](LICENSE).

## Autor

Michal Petrán
[GitHub](https://github.com/Miclah) · [LinkedIn](https://www.linkedin.com/in/mpetran)
