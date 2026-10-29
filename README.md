# Robotite planeet (Robot Planet)

WPF-rakendus, kus erinevad robotid elavad samas maailmas, kuid käituvad erinevalt.

* **Vol.1 (Kodutöö 3):** pärilus, liidesed, polümorfism, `CrazyAction()`, kollektsioon.
* **Vol.2 (Kodutöö 4):** tüübipõhised hoidlad (generics), sündmused ja naabrite reaktsioonid, LINQ-päringud ja kohustuslik MVVM.
* **Vol.3 (Ülesanne 5-1):** ressursimahukad CPU-arvutused järjestikku ja paralleelselt (`Task.Run` + `Task.WhenAll`), hetktõmmis, tühistamine (`CancellationToken`), vea- ja tühistuskäsitlus ning raport. UI jääb töö ajal reageerivaks.

Sama maailm ja samad tegelased kasvavad edasi: uut universumit ega uut projekti ei loodud.

## Sisukord

1. [Valitud maailm ja rakenduse eesmärk](#1-valitud-maailm-ja-rakenduse-eesmärk)
2. [Käivitamisjuhend](#2-käivitamisjuhend)
3. [Projekti struktuur](#3-projekti-struktuur)
4. [Klassihierarhia ja liidesed](#4-klassihierarhia-ja-liidesed)
5. [CrazyAction ja olekureeglid](#5-crazyaction-ja-olekureeglid)
6. [Hoidlad ja generics](#6-hoidlad-ja-generics)
7. [Sündmused ja reaktsioonid](#7-sündmused-ja-reaktsioonid)
8. [LINQ-päringud](#8-linq-päringud)
9. [MVVM: Model, ViewModel, View](#9-mvvm-model-viewmodel-view)
10. [Vol.3: ressursimahukad arvutused](#10-vol3-ressursimahukad-arvutused)
11. [Vol.3 lühike käivitusjuhend ja demo](#11-vol3-lühike-käivitusjuhend-ja-demo)
12. [Kasutajaliides](#12-kasutajaliides)
13. [Iseseisvalt õpitud WPF UI elemendid](#13-iseseisvalt-õpitud-wpf-ui-elemendid)
14. [Kontrollitud kasutusjuhud](#14-kontrollitud-kasutusjuhud)
15. [Git-koostöö](#15-git-koostöö)
16. [Esitamine](#16-esitamine)
17. [AI kasutamine](#17-ai-kasutamine)

---

## 1. Valitud maailm ja rakenduse eesmärk

Valitud maailm: **ROBOT PLANET**. Planeedil elavad kaheksa tüüpi roboteid: `CleanerBot`, `ExplorerBot`, `RepairBot`, `GuardBot`, `JesterBot`, `DeliveryBot`, `InspectorBot` ja `RescueBot`. Neil on ühine baastüüp `Robot` (aku ja terviklikkus), kuid igaühel on oma tavaline tegevus, oma võimed (liidesed) ja oma hullumeelne tegevus `CrazyAction()`.

**Praktikumi garaaži põhimõte (Vol.2):** iga robotitüüp saab oma hoidla ning selles olevad robotid reageerivad üksteise tegevusele. Näiteks kui üks `CleanerBot` küürib end värvist puhtaks, koristavad teised `CleanerBot`-id tema segaduse ära. `ExplorerBot`-id seda ei märka, sest nad elavad teises hoidlas.

Kõik domeenireeglid (robotid, hoidlad, sündmused, reaktsioonid) asuvad Core-projektis. WPF-projekt ainult juhib ja kuvab.

## 2. Käivitamisjuhend

**Nõuded:** Windows 10/11, .NET 10 SDK, Visual Studio töökoormusega *.NET desktop development*.

```bash
git clone <REPOSITORY-URL>
cd RobotPlanet
git checkout vol3/parallel-cpu
dotnet run --project RobotPlanet.Wpf -c Release
```

Alternatiiv: ava `RobotPlanet.slnx` Visual Studios, määra `RobotPlanet.Wpf` käivitusprojektiks ja vajuta **F5**.

Kiirenemise nägemiseks käivita **Release**-konfiguratsioonis (Visual Studios vali *Release* ja vajuta **Ctrl+F5**). *Debug*-ehitus on aeglasem ja annab väiksema vahe. Vol.2 töö asub harus `vol2/events-linq-mvvm`.

**Demoandmed** (lisatakse käivitamisel):

| ID | Nimi | Tüüp | Aku | Terviklikkus |
|---|---|---|---|---|
| 1 | Sädel | CleanerBot | 100 | 100 |
| 2 | Läige | CleanerBot | 100 | 100 |
| 3 | Kosmos | ExplorerBot | 80 | 100 |
| 4 | Mutrivõti | RepairBot | 100 | 60 |
| 5 | Vaht | GuardBot | 100 | 100 |

## 3. Projekti struktuur

```
RobotPlanet.slnx
├── RobotPlanet.Core                 MODEL (klassiteek, ei sõltu WPF-ist)
│   ├── entities/                    Robot, CleanerBot, ExplorerBot, RepairBot, GuardBot,
│   │                                JesterBot, DeliveryBot, InspectorBot, RescueBot
│   ├── interface/                   IChargeable, IScan, IRepair, IRepository<T>, IRobotRepository
│   ├── events/                      RobotEventKind, RobotEventArgs, ReactionEventArgs
│   ├── repository/                  Repository<T>
│   ├── computation/                 RobotSnapshot, RobotCalculations, RobotCalculationRunner (Vol.3)
│   ├── RobotKind.cs                 roboti tüüpide loend
│   ├── RobotWorld.cs                hoidlate register + domeenireeglid
│   └── Properties/Resources.resx    teated (eesti keeles)
└── RobotPlanet.Wpf                  VIEWMODEL + VIEW (viitab Core-projektile)
    ├── ViewModels/
    │   ├── ViewModelBase.cs         INotifyPropertyChanged
    │   ├── RelayCommand.cs          ICommand + CanExecute
    │   ├── MainViewModel.cs         valik, käsud, LINQ-tulemused, logi
    │   ├── ViewModelTypes.cs        SortChoice, GroupSummary, RepositoryItem
    │   ├── CalculationViewModel.cs  arvutuste valik, käsud, tühistamine, raportid (Vol.3)
    │   └── CalculationParts.cs      CalculationTarget, OutcomeRow, ModeReportViewModel
    ├── MainWindow.xaml              VIEW: neli lehte (TabControl), logi (Expander), ainult sidumised (binding)
    ├── MainWindow.xaml.cs           ainult paigutus (kitsal ekraanil)
    └── Properties/Resources.resx    kasutajaliidese tekstid
```

## 4. Klassihierarhia ja liidesed

```mermaid
classDiagram
    class Robot {
        <<abstract>>
        +int Id
        +string Name
        +int Battery
        +int Integrity
        +DoWork() string
        +CrazyAction() string*
        +ReactToAdded(Robot) string
        +ReactToRemoved(Robot) string
        +ReactToCrazyAction(Robot) string
        +CreateCalculation() IRobotCalculation
    }
    class IChargeable {
        <<interface>>
        +Charge(int) string
    }
    class IScan {
        <<interface>>
        +Scan() string
    }
    class IRepair {
        <<interface>>
        +Repair(Robot) string
    }

    Robot <|-- CleanerBot
    Robot <|-- ExplorerBot
    Robot <|-- RepairBot
    Robot <|-- GuardBot
    Robot <|-- JesterBot
    Robot <|-- DeliveryBot
    Robot <|-- InspectorBot
    Robot <|-- RescueBot

    IChargeable <|.. CleanerBot
    IChargeable <|.. ExplorerBot
    IScan <|.. ExplorerBot
    IChargeable <|.. RepairBot
    IRepair <|.. RepairBot
    IScan <|.. GuardBot
    IScan <|.. JesterBot
    IChargeable <|.. JesterBot
    IChargeable <|.. DeliveryBot
    IChargeable <|.. InspectorBot
    IScan <|.. InspectorBot
    IChargeable <|.. RescueBot
    IScan <|.. RescueBot
```

### Klassid

| Klass | Liidesed | `DoWork()` (tavaline tegevus) | `CrazyAction()` |
|---|---|---|---|
| `CleanerBot` | `IChargeable` | koristab põrandat, −5 akut | küürib end puhtaks, pesi värvi maha: −15 akut, −10 terviklikkust |
| `ExplorerBot` | `IChargeable`, `IScan` | uurib ümbrust, −10 akut | avastab "uue planeedi", mis on tema enda vari: −20 akut |
| `RepairBot` | `IChargeable`, `IRepair` | ootab katkiseid (olek ei muutu) | parandab kõike järjest: −10 akut, −5 terviklikkust |
| `GuardBot` | `IScan` | patrullib (olek ei muutu) | arreteerib oma varju: −5 terviklikkust |
| `JesterBot` | `IChargeable`, `IScan` | teeb nalja, −8 akut | etendab liiga hoogsalt: −20 akut |
| `DeliveryBot` | `IChargeable` | toimetab pakke, −5 akut | veab pakke liiga kiiresti: −10 akut, −5 terviklikkust |
| `InspectorBot` | `IChargeable`, `IScan` | kontrollib objekte, −5 akut | inspekteerib kõike korraga: −10 akut, −5 terviklikkust |
| `RescueBot` | `IChargeable`, `IScan` | valmistub päästeks, −7 akut | ryhib päästma kõiki: −12 akut, −8 terviklikkust |

`ExplorerBot`, `RepairBot`, `JesterBot`, `InspectorBot` ja `RescueBot` täidavad kaht rolli korraga (kaks liidest).

### Liidesed

| Liides | Meetod | Kirjeldus |
|---|---|---|
| `IChargeable` | `string Charge(int amount)` | roboti akut saab laadida |
| `IScan` | `string Scan()` | robot oskab ümbrust skaneerida |
| `IRepair` | `string Repair(Robot target)` | robot oskab teist robotit parandada |
| `IRepository<T>` | `Add`, `Remove`, `GetAll`, `Find` | tüübipõhine hoidla (vt punkt 6) |
| `IRobotRepository` | `Title`, `Count`, `Robots` | hoidla kirjutuskaitstud vaade registrile ja UI-le |

`GuardBot` ei ole laetav (tal on reaktor). See on teadlik valik, et võimete kontroll `is` / `as` operaatoriga annaks tüüpide vahel päris erineva tulemuse.

### Polümorfism ja võimete kontroll

* `DoWork()` on `virtual`, `CrazyAction()` on `abstract`. Alamklassid muudavad neid `override` abil.
* Samamoodi on `ReactToAdded`, `ReactToRemoved` ja `ReactToCrazyAction` `virtual` meetodid: iga tüüp võib reageerida omamoodi.
* `CreateCalculation()` on samuti `virtual`: iga tüüp valib oma CPU-arvutuse (vt punkt 10), vaikimisi saab uus tüüp üldise arenguprognoosi.
* Käitumist **ei valita** tüübipõhise `if` / `switch` plokiga. Hoidla ja ViewModel kutsuvad meetodeid baastüübi `Robot` kaudu.
* `RobotWorld` ja ViewModel kontrollivad võimeid operaatoritega `is` ja `as` (`robot is IChargeable`, `robot as IScan`, `healer is IRepair`).

## 5. CrazyAction ja olekureeglid

### CrazyAction tegevused

| Robot | Tingimus | Tulemus |
|---|---|---|
| `CleanerBot` | aku < 15 | keeldub, olek ei muutu |
| `CleanerBot` | aku ≥ 15 | aku −15, terviklikkus −10 |
| `ExplorerBot` | aku < 20 | keeldub, olek ei muutu |
| `ExplorerBot` | aku ≥ 20 | aku −20 |
| `RepairBot` | terviklikkus < 20 | laguneb ise koost, keeldub |
| `RepairBot` | aku < 10 | keeldub, olek ei muutu |
| `RepairBot` | muidu | aku −10, terviklikkus −5 |
| `GuardBot` | terviklikkus < 30 | jääb vahipostil magama, olek ei muutu |
| `GuardBot` | muidu | terviklikkus −5 |
| `JesterBot` | aku < 20 | keeldub, olek ei muutu |
| `JesterBot` | aku ≥ 20 | aku −20 |
| `DeliveryBot` | terviklikkus < 20 | keeldub (liiga katki), olek ei muutu |
| `DeliveryBot` | aku < 10 | keeldub, olek ei muutu |
| `DeliveryBot` | muidu | aku −10, terviklikkus −5 |
| `InspectorBot` | terviklikkus < 20 | keeldub (liiga katki), olek ei muutu |
| `InspectorBot` | aku < 10 | keeldub, olek ei muutu |
| `InspectorBot` | muidu | aku −10, terviklikkus −5 |
| `RescueBot` | terviklikkus < 23 | keeldub (liiga katki), olek ei muutu |
| `RescueBot` | aku < 12 | keeldub, olek ei muutu |
| `RescueBot` | muidu | aku −12, terviklikkus −8 |

### Olekureeglid ja valideerimine

* **Identiteet** (`Id`, `Name`) määratakse konstruktoris ja on ainult loetav.
* `Id`: 0–999999 ja **unikaalne kogu maailmas** (kontrollib `RobotWorld.TryAdd`, ka teise tüübi robotite vastu).
* `Name`: pärast `Trim()` 2–20 märki, mitte tühi.
* `Battery` ja `Integrity`: 0–100. Setter on `private`, muutmine käib ainult kaitstud meetoditega `TrySpendBattery`, `TryAddBattery`, `TryDamage`, `TryRestoreIntegrity`.
* Need meetodid **keelduvad vigasest sisendist ja ei muuda olekut** (negatiivne kogus, liiga vähe akut).
* `ReceiveRepair(int)` on ainus avalik remondisisend: kogus 1–100, ideaalses seisus robotit ei paranda.
* Vigane konstruktoriargument annab `ArgumentException`, mille `RobotWorld.TryAdd` püüab kinni ja tagastab sõnumina. Rakendus ei sulgu.
* Tekstisisend muudetakse arvuks ViewModelis (`int.TryParse`), ülejäänud reeglid kontrollib Core.

## 6. Hoidlad ja generics

```csharp
public interface IRepository<T> where T : Robot
{
    void Add(T item);
    bool Remove(T item);
    IEnumerable<T> GetAll();
    T? Find(Func<T, bool> predicate);
}

public class Repository<T> : IRepository<T>, IRobotRepository where T : Robot
```

* **Iga konkreetse tüübi jaoks eraldi hoidla:** `Repository<CleanerBot>`, `Repository<ExplorerBot>`, `Repository<RepairBot>`, `Repository<GuardBot>`. Hoidlad on loodud selle maailma enda klassidest.
* **Piirang `where T : Robot`** tuleneb maailma olemasolevast ühisest baastüübist. Vale tüübi lisamist takistab **kompilaator**, mitte nimede võrdlemine kasutajaliideses:

  | Katse | Kompilaatori vastus |
  |---|---|
  | `new Repository<string>()` | `CS0311`: `string` ei ole `Robot` |
  | `Repository<CleanerBot>.Add(new GuardBot(...))` | `CS1503`: `GuardBot` ei ole `CleanerBot` |

* `Repository<T>` hoiab robotid `ObservableCollection<T>`-is ja annab UI-le välja kirjutuskaitstud `ReadOnlyObservableCollection<T>` (`Items`). UI ei saa roboteid otse lisada ega eemaldada.
* `RobotWorld` on hoidlate **register**. Konstruktoris on iga tüübi kohta üks rida:

  ```csharp
  Register(RobotKind.Cleaner, (id, name, battery, integrity) => new CleanerBot(id, name, battery, integrity));
  ```

  Uue tüübi lisamiseks piisab uuest klassist, uuest `RobotKind` väärtusest ja ühest `Register`-reast. Hoidla, sündmused, tellimused ja UI-kuvamine tekivad automaatselt.
* `RobotWorld` leiab roboti hoidla **täpse tüübi** järgi (`GetType() == typeof(T)`), seega robot ei satu kunagi teise tüübi hoidlasse.

```mermaid
flowchart LR
    W[RobotWorld] --> C["Hoidla: CleanerBot"]
    W --> E["Hoidla: ExplorerBot"]
    W --> R["Hoidla: RepairBot"]
    W --> G["Hoidla: GuardBot"]
    W --> J["Hoidla: JesterBot"]
    W --> D["Hoidla: DeliveryBot"]
    W --> I["Hoidla: InspectorBot"]
    W --> RS["Hoidla: RescueBot"]
    C --> C1[Sädel]
    C --> C2[Läige]
    E --> E1[Kosmos]
    R --> R1[Mutrivõti]
    G --> G1[Vaht]
```

Sama hoidla robotid kuulavad üksteist. Eri hoidlate robotid ei kuule üksteist.

## 7. Sündmused ja reaktsioonid

Sündmused ja reaktsioonid kuuluvad **domeeniloogikasse (Core)**, mitte WPF-nupu sündmuskäsitleja sisse.

### Kolm sündmust (`Repository<T>`)

| Sündmus | Millal | Kes reageerib |
|---|---|---|
| `ItemAdded` | hoidlasse lisatakse robot | kõik **senised** selle hoidla robotid tervitavad uut tulijat; uus robot ei tervita iseennast |
| `ItemRemoved` | robot eemaldatakse hoidlast | allesjäänud robotid reageerivad lahkumisele; eemaldatud robot enam ei reageeri |
| `CrazyActionPerformed` | robot teeb oma `CrazyAction()` | teised sama hoidla robotid reageerivad; tegevuse põhjustaja ei reageeri iseendale |

Iga sündmus kannab `RobotEventArgs`: `Source` (milline robot tegevuse põhjustas) ja `Kind` (mis juhtus). Iga reaktsioon jätab logikirje: hoidla tõstab `ReactionOccurred` (`ReactionEventArgs` koos reageerija, põhjuse ja sõnumiga).

### Tellimuste haldus

* `Add`: robot lisatakse kollektsiooni ja **tellitakse** kõigile kolmele sündmusele.
* `Remove`: robot **tühistab** kõik kolm tellimust enne, kui `ItemRemoved` tõstetakse. Seetõttu ei reageeri eemaldatud robot ei oma lahkumisele ega hilisematele sündmustele.
* `React(...)` jätab sündmuse vahele, kui `e.Source` on reageerija ise.
* Tellimuste hoidmiseks on `Dictionary<T, Subscription>`, et sama delegaadi saaks hiljem tühistada.

### Reaktsioonid (igal tüübil oma)

| Robot | Sündmus | Reaktsioon | Muudab reageerija olekut? |
|---|---|---|---|
| kõik (vaikimisi) | lisamine / eemaldamine / CrazyAction | teade, et robot tervitab / vaatab kurvalt / vaatab pealt | ei |
| `CleanerBot` | CrazyAction | koristab teise tekitatud segaduse ära | **jah:** aku −3 (liiga väsinuna ei korista) |
| `ExplorerBot` | lisamine | asub uurima tulijat | **jah:** aku −2 (liiga väsinuna ei uuri) |
| `ExplorerBot` | eemaldamine | märgib kaardile, et robot on kadunud | ei |
| `RepairBot` | CrazyAction | märkab pahandust ja lappab end | **jah:** terviklikkus +3 (kuni 100) |
| `RepairBot` | eemaldamine | kogub lahkunu varuosad | ei |
| `GuardBot` | lisamine | kontrollib tulija ID-d | ei |
| `GuardBot` | CrazyAction | nõuab tegijalt seletust | ei |
| `DeliveryBot` | CrazyAction | märkab pahandust ja parandab end | **jah:** terviklikkus +3 (kuni 100) |
| `DeliveryBot` | eemaldamine | märgib lahkunu kohaletoimetuse nimekirjast | ei |
| `InspectorBot` | CrazyAction | kontrollib tegija tegevust ja parandab end | **jah:** terviklikkus +3 (kuni 100) |
| `InspectorBot` | eemaldamine | kirjutab lahkumise kohta aruande | ei |
| `RescueBot` | CrazyAction | tõttab appi ja parandab end | **jah:** terviklikkus +6 (kuni 100) |
| `RescueBot` | eemaldamine | märgib lahkunu päästenimekirja | ei |

Vähemalt kolm reaktsiooni muudavad reageerija olekut (aku või terviklikkus).

### Ühe sündmuse teekond (näide: `Gamma` teeb `CrazyAction()`)

```mermaid
sequenceDiagram
    actor User as Kasutaja
    participant View as MainWindow View
    participant VM as MainViewModel
    participant World as RobotWorld
    participant Repo as Hoidla CleanerBot
    participant G as Gamma tegija
    participant S as Sädel reageerija

    User->>View: klikk Hullumeelne tegevus
    View->>VM: CrazyCommand.Execute
    VM->>World: CrazyAction(Gamma)
    World->>Repo: TriggerCrazyAction(Gamma)
    Repo->>G: CrazyAction (muudab Gamma olekut)
    G-->>Repo: teade tegevusest
    Repo-->>S: CrazyActionPerformed Source Gamma
    S->>S: ReactToCrazyAction muudab oma akut
    Repo-->>World: ReactionOccurred
    World-->>VM: ReactionOccurred
    VM->>VM: logi + LINQ-nimekirjade uuendus
    VM-->>View: CollectionChanged / PropertyChanged
```

Sammud sõnadega:

1. **View:** nupp on seotud käsuga `CrazyCommand` (`Command="{Binding CrazyCommand}"`), koodis pole `Click`-sündmust.
2. **ViewModel:** `RelayCommand` käivitab `Crazy()`, mis läheb läbi `Run(...)`.
3. **Model:** `RobotWorld` leiab roboti hoidla ja kutsub `Repository<T>.TriggerCrazyAction(actor)`.
4. Hoidla kutsub polümorfselt `actor.CrazyAction()` (muudab tegija olekut) ja tõstab `CrazyActionPerformed`.
5. Iga tellitud robot (v.a tegija) käivitab oma `ReactToCrazyAction(...)`, mis võib muuta tema enda olekut, ja hoidla tõstab `ReactionOccurred`.
6. `RobotWorld` edastab reaktsiooni ühe sündmusena. ViewModel kogub selle tegevuse sõnumi alla.
7. `Run` kirjutab logisse esmalt tegevuse rea ja selle alla reaktsioonid (`↳`), seejärel uuendab LINQ-nimekirjad, kokkuvõtte ja käskude `CanExecute`.
8. `Robot` teavitab oleku muutusest (`INotifyPropertyChanged`), seega aku- ja terviklikkuseribad uuenevad ise.

## 8. LINQ-päringud

Kolm päringut asuvad `MainViewModel`-is ja nende tulemused kuvatakse kasutajaliideses:

| Päring | Operaator | Kus UI-s | Kood |
|---|---|---|---|
| **Otsimine** | `Where` | otsinguväli "Otsi nime või ID järgi" | `query.Where(r => r.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase) \|\| r.Id.ToString().Contains(text))` |
| **Sorteerimine** | `OrderBy` / `OrderByDescending` | valik "Sorteeri" (nimi, aku, terviklikkus, ID) ja märkeruut "Kahanevalt" | `SortDescending ? query.OrderByDescending(r => r.Battery) : query.OrderBy(r => r.Battery)` |
| **Grupeerimine** | `GroupBy` | kaart "Kokkuvõte" | `_world.Robots.GroupBy(r => r.Kind).Select(g => new GroupSummary(g.Key, g.Count(), (int)Math.Round(g.Average(r => r.Battery)), (int)Math.Round(g.Average(r => r.Integrity))))` |

* Otsing ja sorteerimine töötavad valitud hoidla robotitel (`VisibleRobots`).
* Kokkuvõte ühendab kõigi hoidlate lugemisvaated (`RobotWorld.Robots`) ja näitab iga tüübi robotite arvu ning keskmist akut ja terviklikkust.
* Pärast iga tegevust arvutatakse päringud uuesti, seega nimekiri ja kokkuvõte uuenevad automaatselt.
* Nimekirja uuendab `Sync(...)` meetod (`Remove` / `Move` / `Insert`, mitte `Clear` + `Add`), et valitud robot jääks valituks.

## 9. MVVM: Model, ViewModel, View

| Kiht | Vastutus | Kus |
|---|---|---|
| **Model (Core)** | robotid, hoidlad, sündmused, reaktsioonide reeglid, valideerimine. Ei sõltu WPF-ist | `RobotPlanet.Core` |
| **ViewModel** | valitud hoidla ja robot, kuvatavad loendid, LINQ-päringute tulemused, logi, kasutaja käsud (`ICommand`) | `RobotPlanet.Wpf/ViewModels` |
| **View** | ainult kujundus ja sidumised (`Binding`). Kood-taga on ainult paigutus | `MainWindow.xaml` |

### Käsud ja `CanExecute`

Tegevused käivitatakse `ICommand`-käskudega (`RelayCommand`). `CanExecute` keelab käsu (nupp tuhmub), kui sobivat valikut või vajalikku sisendit pole:

| Käsk | Lubatud, kui |
|---|---|
| `WorkCommand`, `CrazyCommand`, `RemoveCommand` | on valitud robot |
| `ChargeCommand` | valitud robot `is IChargeable` |
| `ScanCommand` | valitud robot `is IScan` |
| `RepairCommand` | valitud robot `is IRepair` ja on valitud sihtmärk |
| `AddCommand` | on valitud tüüp ning ID ja nimi pole tühjad |
| `ClearLogCommand` | alati |
| `RunSequentialCommand`, `RunParallelCommand`, `RunBothCommand` (Vol.3) | on valitud vähemalt üks objekt ja töö ei käi |
| `CancelCommand` (Vol.3) | töö käib ja tühistamist pole veel küsitud |
| `SelectAllCommand`, `ClearSelectionCommand` (Vol.3) | töö ei käi |

Vol.3 käsud asuvad eraldi alam-ViewModelis `CalculationViewModel`, mida `MainViewModel` annab välja omadusena `Calculation`. Nii jääb `MainViewModel` väikeseks.

### Automaatsed uuendused

* **Loend:** `ObservableCollection<T>` / `ReadOnlyObservableCollection<T>` (`CollectionChanged`).
* **Roboti olek:** `Robot` realiseerib `INotifyPropertyChanged` (liides on .NET baasteegist, mitte WPF-ist, seega Core ei sõltu WPF-ist).
* **Kokkuvõte ja logi:** `Run(...)` uuendab need pärast iga tegevust.
* **Käskude lubatavus:** ViewModel kutsub `RaiseCanExecuteChanged()` valiku ja sisendi muutudes.
* **UI-koodis puudub** hoidlate ega tegelaste äriloogika: kood-taga ei sisalda `Click`-käsitlejaid ega `_world` viiteid.

## 10. Vol.3: ressursimahukad arvutused

Vol.3 lisab samasse projekti ja hoidlasse **CPU-mahukad arvutused**. Kasutaja valib objektid ja töömahu, arvutused käivitatakse **järjestikku** ja **paralleelselt** samal sisendil, UI jääb töö ajal reageerivaks ning töö saab **tühistada**. Tulemused, vead ja tühistused koondatakse raportisse.

### Objektid ja nende arvutused

Valitud on kõik neli robotitüüpi (nõutud vähemalt kolm). Iga tüüp valib oma arvutuse polümorfselt (`Robot.CreateCalculation()` on `virtual`, ilma `switch`-ita):

| Robot | Arvutus | Mida arvutab | Mida hetktõmmisest kasutab | Tulemus |
|---|---|---|---|---|
| `ExplorerBot` | Retkevariantide võrdlus (`RouteComparisonCalculation`) | võrdleb 300 000 × töömaht juhuslikku retkevarianti läbi 8–13 teekonnapunkti | aku (punktide arv = 8 + aku / 20) | lühima retke pikkus |
| `CleanerBot` | Koristussimulatsioon (`CleaningSimulationCalculation`) | 2 500 × töömaht korduvat juhuslikku koristusrännakut 20×20 ruudustikul, eesmärk 60 % kaetust | aku (sammude piir = aku × 12) | keskmine kaetus (%) |
| `GuardBot` | Patrulli simulatsioon (`PatrolSimulationCalculation`) | 4 000 000 × töömaht tiksu patrulli ringil, sissetungijad ilmuvad juhuslikult | terviklikkus (avastamise tõenäosus) | tuvastatud sissetungijate osa (%) |
| `RepairBot` ja iga uus tüüp (vaikimisi) | Arenguprognoos (`WearForecastCalculation`) | 15 000 × töömaht Monte Carlo prognoosi 200 päeva kohta | aku (kulumise kiirus) ja terviklikkus | tõenäosus, et terviklikkus langeb alla 20 (%) |

**Töömaht** on liugur 1–50 ja see skaleerib iteratsioonide arvu. Vaikimisi töömaht 10 võtab ühe objekti kohta murdosa sekundist kuni umbes poole sekundi, töömaht 50 mitu sekundit (sõltub arvutist ja ehitusest).

Arvutuste ülesehitus on **mall-meetod**: baasklass `RobotCalculation` kontrollib ühist eeltingimust (robot peab olema töökorras) ja alamklass täidab oma algoritmi meetodis `Compute`.

### Hetktõmmis (snapshot)

`RobotSnapshot` on muutumatu `record` (Id, Name, Kind, Battery, Integrity). Hetktõmmis tehakse **UI-lõimes käivitamise hetkel** (`RobotCalculationJob.From(robot)`). Taustalõimed loevad ainult seda koopiat, mitte elavat `Robot` objekti, mis on seotud kasutajaliidesega ja muutub töö ajal. Mõlemad režiimid kasutavad **samu hetktõmmiseid** ja sama töömahtu.

### Kaks režiimi

```csharp
// Järjestikune: oota ühe objekti lõppu enne järgmise alustamist
foreach (RobotCalculationJob job in jobs)
    outcomes.Add(await Task.Run(() => RunOne(job, workload, token, progress)));

// Paralleelne: käivita kõik korraga Task.Run-iga ja oota kõiki Task.WhenAll-iga
Task<CalculationOutcome>[] tasks = jobs.Select(job => Task.Run(() => RunOne(job, workload, token, progress))).ToArray();
CalculationOutcome[] outcomes = await Task.WhenAll(tasks);
```

* **Sama sisend:** sama hetktõmmiste loend ja sama töömaht. Iga robot kasutab oma `Random`-it seemnega `Id * 7919 + töömaht`, seega tulemus on deterministlik ja kahe režiimi tulemusi saab võrrelda.
* **Aja mõõtmine:** `Stopwatch` mõõdab iga objekti kestuse ja kogu töö aja (`RunReport.TotalDuration`).
* **Võrdlus:** `RunComparison` kontrollib, et sisend oli sama (`SameInput`), kas kõik tulemused ühtivad (`ResultsMatch`) ja arvutab kiirenemise (järjestikune aeg / paralleelne aeg). Tühistatud käivitust ei võrrelda.

### UI jääb reageerivaks

* Arvutus jookseb Core-is `Task.Run`-iga niidikogus. ViewModel kasutab `async`/`await` ning **ei kasuta** `.Result` ega `.Wait()`, seega UI-lõim ei blokeeru.
* Edenemist teatab `Progress<CalculationOutcome>`, mis on loodud UI-lõimes, seega selle tagasikutse jookseb UI-lõimes ja lisab raportisse rea kohe, kui objekt on valmis.
* Elav kell "Kulunud aeg" uuendub `PeriodicTimer`-iga ja näitab, et UI ei ole külmunud.
* `ObservableCollection`-id ja omadused muutuvad ainult UI-lõimes. Taustalõimed puutuvad ainult hetktõmmist.
* Töö ajal on valik ja liugur tuhmid (`CanEdit`), et sisend ei muutuks käivituse keskel.

### Tühistamine

* **Üks ühine `CancellationTokenSource`** kogu käivituse kohta. Nupp **Tühista** kutsub `Cancel()`, mis jõuab kõigi arvutusteni.
* **Iga CPU-tsükkel kontrollib seda:** iga ring (nii välimine kui sisemine) algab `token.ThrowIfCancellationRequested()`-iga. Seetõttu peatub töö kiiresti, ka töömahu 50 puhul.
* Objektid, mille tööd polnud veel alanud (järjestikune režiim), märgitakse tühistatuks **enne alustamist**.
* **Võrdle mõlemat**: kui tühistatakse esimese režiimi ajal, teist režiimi ei alustata.
* Pärast tühistamist saab kohe uue käivituse teha.

### Edu, viga ja tühistus

`RunOne` püüab iga objekti tulemuse kinni ja muudab selle kolmeks lõppolekuks, nii et ükski erand ei välju meetodist:

| Lõppolek | Millal | Raportis |
|---|---|---|
| **Õnnestus** | arvutus lõppes | tulemuse tekst |
| **Viga** | arvutus viskas erandi (nt robot pole töökorras) | veateade; ülejäänud tööd jätkuvad |
| **Tühistatud** | token tühistati | "Tühistati töö ajal." või "Tühistati enne alustamist." |

Kuna iga ülesanne püüab oma erandid kinni, ei viska `Task.WhenAll` kunagi ja ühe objekti viga ei peata teisi.

### Raport

Iga rida: **objekti nimi**, **lõppolek**, **tulemus või veateade**, **kestus**. Raporti lõpus **kogu töö aeg** ja loendurid (Õnnestus / Viga / Tühistatud). Mõlema režiimi raport on eraldi (leht **Raportid**), võrdluskaart on lehel **Arvutused**.

```mermaid
sequenceDiagram
    participant UI as UI-lõim
    participant VM as CalculationViewModel
    participant R as RobotCalculationRunner
    participant P as Niidikogu

    UI->>VM: klikk Paralleelselt
    VM->>VM: hetktõmmised UI-lõimes
    VM->>R: RunAsync jobs, töömaht, Parallel, token
    R->>P: Task.Run iga objekti kohta
    Note over UI: UI on vaba, kell tiksub
    P-->>VM: Progress: üks objekt valmis
    R->>R: await Task.WhenAll
    R-->>VM: RunReport nimi, lõppolek, tulemus, kestus, kogu aeg
    VM-->>UI: raport ja võrdlus
    UI->>VM: klikk Tühista
    VM->>R: token.Cancel
    P-->>R: iga tsükkel kontrollib tokenit
```

## 11. Vol.3 lühike käivitusjuhend ja demo

```bash
git clone <REPOSITORY-URL>
cd RobotPlanet
git checkout vol3/parallel-cpu
dotnet run --project RobotPlanet.Wpf -c Release
```

Visual Studios: vali *Release*, määra `RobotPlanet.Wpf` käivitusprojektiks ja vajuta **Ctrl+F5**. Vajalik on .NET 10 SDK ja Windows.

Kolm demostsenaariumi (leht **Arvutused**, tulemused lehel **Raportid**):

| Demo | Sammud | Mida näed |
|---|---|---|
| **1. Mõlemad režiimid** | Vajuta **Vali kõik**, töömaht 10, **Võrdle mõlemat** | Lehel **Arvutused** täitub edenemisriba, kell tiksub ja laimikaardil on kiirenemine ning "Tulemused ühtivad: jah". Lehel **Raportid** on kaks raportit (**Järjestikku**, **Paralleelselt**) neljarealiselt, kõik **Õnnestus** |
| **2. Viga** | Lehel **Haldus** lisa vormis tüüp `Explorer`, nimi `Tühiaku`, aku `0`. Lehel **Arvutused** vali kõik objektid, **Võrdle mõlemat** | Lehel **Raportid** on `Tühiaku` rida punane **Viga** koos veateatega, ülejäänud neli on **Õnnestus**. Töö lõpeb olekuga "Valmis." |
| **3. Tühistamine** | Töömaht 50, vajuta **Paralleelselt** ja varsti **Tühista** | Lehel **Raportid** muutuvad kõik read halliks **Tühistatud**, lehel **Arvutused** kell peatub, olek "Tühistatud." Uus käivitus töötab normaalselt |

Kiirenemine sõltub protsessori tuumade arvust: mitmetuumalisel arvutil on paralleelne režiim märgatavalt kiirem, ühe tuumaga arvutil on vahe väike.

## 12. Kasutajaliides

Ühel ekraanil on vähe elemente: sisu on jaotatud **neljale lehele** (`TabControl`), logi on all kokkuvolditav (`Expander`) ja nähtav igal lehel.

| Leht | Sisu |
|---|---|
| **Robotid** | vasakul hoidlate "chip"-id koos robotite arvuga, otsing ja sorteerimine ning valitud hoidla robotid (aku- ja terviklikkuseribadega); paremal valitud roboti üksikasjad (laimikaart) ja tegevusnupud (Tööta, Hullumeelne tegevus!, Lae, Skanni, Paranda) |
| **Haldus** | vorm "Lisa robot" koos nupuga **Eemalda valitud** ning tüüpide **Kokkuvõte** (`GroupBy`) |
| **Arvutused** (Vol.3) | objektide märkeruudud, töömahu liugur (1–50), nupud **Järjestikku**, **Paralleelselt**, **Võrdle mõlemat** ja **Tühista**, edenemisriba, olek ja elav kell, laimikaart **Võrdlus** (kiirenemine ja tulemuste kokkulangevus) |
| **Raportid** (Vol.3) | kaks raportit (järjestikune, paralleelne): iga rida näitab nime, värvilist lõppolekut, kestust ning tulemust või veateadet, all loendurid ja kogu töö aeg |

* **Logi** (all): tegevused ja naabrite reaktsioonid. Vorm "Lisa robot" ja veateated asuvad eri lehel kui logi vaatamise koht, seetõttu on logi alati näha.
* **Lehtede vahetamine ei mõjuta andmeid:** kogu seis (valitud hoidla ja robot, vormi väljad, käimasolev arvutus) on ViewModelis. Arvutus jätkub ka siis, kui vahetad lehte, ja read ilmuvad lehele **Raportid** ükshaaval.
* **Kitsal ekraanil** (alla ~860 px) liigub lehel **Robotid** parem veerg vasaku alla. Iga leht kerib vajadusel ise vertikaalselt, pealkiri, lehed ja logi jäävad paigale.
* Tekstid on ressursifailides (`Resources.resx`), mitte koodis.

## 13. Iseseisvalt õpitud WPF UI elemendid

Valitud elemendid: **`DataTemplate`**, **`ProgressBar`**, **`Slider`**, **`TabControl`** ja **`Expander`**, lisaks `ControlTemplate`, `DataTrigger` ja `ItemsControl`.

**Põhjendus:**
* `DataTemplate` määrab, kuidas `Robot`, `RepositoryItem` ja `GroupSummary` objektid loendis välja näevad. Kuvamine on andmetest eraldatud: ViewModel annab andmed, mall otsustab välimuse. Uue `Robot` alamklassi lisamisel ei pea nimekirja koodi muutma.
* `ProgressBar` sobib aku ja terviklikkuse jaoks loomulikult: väärtus 0–100 on vahemik, mille muutumist on kohe näha. Sidumine `Mode=OneWay` ja `INotifyPropertyChanged` muudavad ribad "elavaks" ilma lisakoodita (reaktsioon muudab teise roboti akut ja riba liigub ise).
* `ControlTemplate` ja `DataTrigger` võimaldavad nuppe, kaarte ja nimekirjaridu kujundada ümber ning kuvada vihje "Vali nimekirjast robot." ainult siis, kui valikut pole. Nupu tuhmumine põhineb `IsEnabled` olekul, mille määrab `CanExecute`.
* `ItemsControl` + `DataTemplate` ehitavad `GroupBy` kokkuvõtte tabeli ilma vaikimisi tabelikomponendita.
* `Slider` (Vol.3) sobib töömahu valimiseks: täisarvuline vahemik 1–50, sidumine `IsSnapToTickEnabled`-iga annab ainult täisarvud. Piirid tulevad ViewModelist (`WorkloadMin`, `WorkloadMax`), seega number 50 on kirjas ainult Core-projektis. Liuguri välimuse tegin ise `ControlTemplate`-iga (`Track`, `Thumb`, `RepeatButton`) ning see tuhmub, kui töö käib (`IsEnabled` ← `CanEdit`).
* `ProgressBar` näitab ka arvutuste edenemist (valmis objekte / kõik objektid), mille uuendab `Progress<T>` UI-lõimes.
* `TabControl` jagab sisu neljale lehele ja `Expander` voldib logi kokku, et ühel ekraanil oleks vähem elemente. Ülesehitus ja ViewModel jäid samaks: leht vahetab ainult seda, mida kuvatakse. Mõlema välimuse (pillikujulised lehepealkirjad, noolega päis) tegin `ControlTemplate`-iga.

## 14. Kontrollitud kasutusjuhud

Lehed: **Robotid** (hoidlad, nimekiri, üksikasjad, tegevused), **Haldus** (vorm "Lisa robot", **Eemalda valitud**, **Kokkuvõte**), **Arvutused** ja **Raportid**. Valik (valitud hoidla ja robot) säilib lehtede vahetamisel.

Märgi tulemus pärast käsitsi katsetamist (☑ = kontrollitud). Kõik juhud eeldavad **värskelt käivitatud rakendust** demoandmetega (punkt 2), kui pole öeldud teisiti.

### Kontrollnäide A / B / C (sama hoidla: `CleanerBot`)

A = Sädel, B = Läige. Hoidlas olevad C-d lisatakse kasutajaliidese kaudu.

| # | Tegevus | Oodatud tulemus | ☑ |
|---|---|---|---|
| A1 | Lehel **Robotid** vali hoidla `CleanerBot`. Lehel **Haldus** lisa vormis tüüp `Cleaner`, nimi `Gamma` (C) | Logis "Lisatud: Gamma (#6) [CleanerBot]." ja **kaks** reaktsiooni (Sädel ja Läige tervitavad). Gamma ise ei reageeri | ☑ |
| A2 | Vali Gamma ja vajuta **Hullumeelne tegevus!** | Gamma aku 85, terv. 90. Sädel ja Läige reageerivad ja nende aku on 97. Logis 1 tegevuse rida + 2 reaktsiooni. Gamma ei reageeri iseendale | ☑ |
| A3 | Lehel **Robotid** vali Läige (B), lehel **Haldus** vajuta **Eemalda valitud** | Logis "Eemaldatud: Läige (#2) [CleanerBot]." ja reageerivad **Sädel ja Gamma**. Läige ei reageeri | ☑ |
| A4 | Vali Gamma ja vajuta uuesti **Hullumeelne tegevus!** | Gamma aku 70, terv. 80. Reageerib **ainult Sädel** (aku 94). Läige reaktsiooni logis pole | ☑ |
| A5 | Vaata hoidlaid `ExplorerBot`, `RepairBot`, `GuardBot` | Kosmos (aku 80), Mutrivõti (terv. 60) ja Vaht (100/100) ei ole muutunud ega esine logis (teised hoidlad ei reageeri) | ☑ |

### Muud kasutusjuhud

| # | Tegevus | Oodatud tulemus | ☑ |
|---|---|---|---|
| 1 | Lehel **Haldus** lisa vormis tüüp `Explorer`, nimi `Uurija` | Kosmos reageerib ja tema aku muutub 80 → 78. Uurija ei reageeri. Puhastajate hoidlat see ei puuduta | ☑ |
| 2 | Lehel **Haldus** lisa tüüp `Repair`, nimi `Võti`. Võti valitakse automaatselt, lehel **Robotid** vajuta **Hullumeelne tegevus!** | Võti aku 90, terv. 95. Mutrivõti reageerib ja lappab end: terviklikkus 60 → 63 | ☑ |
| 3 | Lehel **Haldus** lisa tüüp `Guard`, nimi `Valvur` | Vaht kontrollib tulija ID-d (#6). Olek ei muutu. Valvur ei reageeri | ☑ |
| 4 | Sädel teeb **Hullumeelne tegevus!**. Siis vali hoidla `RepairBot`, vali Mutrivõti, sihtmärgiks Sädel, **Paranda** | Sädel terv. 90 → 100, Mutrivõtme aku 100 → 90. Logis "Mutrivõti asub tööle. Sädel sai remonditud: terviklikkus 90 → 100." | ☑ |
| 5 | Lehel **Haldus** lisa tüüp `Explorer`, nimi `Väsinud`, aku `15`. Uus robot valitakse automaatselt, lehel **Robotid** vajuta **Hullumeelne tegevus!** | Robot keeldub (aku jääb 15). Kosmos reageerib katsele üldise reaktsiooniga (vt punkt "Teadlikud otsused") | ☑ |
| 6 | Vali Vaht (`GuardBot`) | **Lae** on tuhm (`is IChargeable` ei ole täidetud), **Skanni** aktiivne | ☑ |
| 7 | Vali Sädel (`CleanerBot`) | **Skanni** on tuhm (`is IScan` ei ole täidetud), **Lae** aktiivne | ☑ |
| 8 | Ära vali ühtegi robotit | **Tööta**, **Hullumeelne tegevus!**, **Lae**, **Skanni**, **Paranda** ja **Eemalda valitud** (lehel **Haldus**) on tuhmid. Üksikasjade kaardil on vihje | ☑ |
| 9 | Käivita rakendus ja vaata lehel **Haldus** vormi "Lisa robot" (tüüp valimata, nimi tühi) | **Lisa** on tuhm. Muutub aktiivseks, kui tüüp on valitud ja nimi sisestatud (ID pakutakse automaatselt) | ☑ |
| 10 | Lehel **Haldus** vali tüüp, sisesta nimi ja ID `abc` ning vajuta **Lisa** | Logis "ID peab olema täisarv.", robotit ei lisata | ☑ |
| 11 | Lehel **Haldus** lisa robot ID-ga, mis on juba kasutusel (nt `1`) | Logis "ID 1 on juba kasutusel.", robotit ei lisata (kehtib ka teise tüübi puhul) | ☑ |
| 12 | Lehel **Haldus** lisa robot nimega `A` | Logis nime viga, robotit ei lisata | ☑ |
| 13 | Lehel **Haldus** lisa robot akuga `150` | Logis "Aku laetus peab olema vahemikus 0–100.", robotit ei lisata | ☑ |
| 14 | Vali Sädel, sisesta laadimiskogus `abc` ja vajuta **Lae** | Logis "Kogus peab olema täisarv.", aku ei muutu | ☑ |
| 15 | Vali Sädel, sisesta laadimiskogus `-5` ja vajuta **Lae** | Logis "Sädel: vigane laadimiskogus (-5), olek jäi muutmata.", aku ei muutu | ☑ |
| 16 | **LINQ Where:** kirjuta otsingusse `säd` (hoidla `CleanerBot`) | Nimekirjas on ainult Sädel (suur- ja väiketähti ei arvestata). Tühjenda väli: mõlemad tagasi. Otsing `2` leiab Läige (ID järgi) | ☑ |
| 17 | **LINQ OrderBy:** juhu 4 või A2 järel vali sorteerimine "Aku" | Nimekiri järjestub aku järgi kasvavalt. Märkeruut "Kahanevalt" pöörab järjekorra. Valitud robot jääb valituks | ☑ |
| 18 | **LINQ GroupBy:** vaata lehel **Haldus** kaarti "Kokkuvõte" | Neli rida (`CleanerBot` 2, 100 % / 100 %; `ExplorerBot` 1, 80 % / 100 %; `GuardBot` 1, 100 % / 100 %; `RepairBot` 1, 100 % / 60 %). Pärast **Sädeli Hullumeelset tegevust** on `CleanerBot` keskmine aku 91 % ja terviklikkus 95 % | ☑ |
| 19 | Lisa või eemalda robot (leht **Haldus**) ja vaata lehel **Robotid** hoidla "chip"-i | Hoidla "chip" näitab uut robotite arvu kohe | ☑ |
| 20 | Vähenda akent alla ~860 px | Parem veerg liigub vasaku alla, leht kerib vertikaalselt, midagi ei lõigata ära | ☑ |

### Vol.3 kasutusjuhud (arvutused)

Lehel **Arvutused** (tulemused lehel **Raportid**). Kui pole öeldud teisiti, on töömaht 10 ja kõik neli demorobotit valitud (**Vali kõik**).

| # | Tegevus | Oodatud tulemus | ☑ |
|---|---|---|---|
| V1 | **Võrdle mõlemat** | Lehel **Raportid** on mõlemad raportid (**Järjestikku**, **Paralleelselt**) täidetud, kummaski neli rida staatusega **Õnnestus**. Mõlemal on "Kogu töö aeg" ja nimede/tulemuste kokkuvõte. Laimikaardil on "Kiirenemine: ×…" ja "Tulemused ühtivad: jah". Olek "Valmis." | ☑ |
| V2 | Ainult **Järjestikku** | Ilmub ainult järjestikune raport. Paralleelne raport on peidus ja võrdluskaart näitab vihjet "Võrdluseks käivita mõlemad režiimid…" | ☑ |
| V3 | Ainult **Paralleelselt** | Ilmub ainult paralleelne raport, nelja rea järjekord on sama mis valikus | ☑ |
| V4 | Ilma valikuta (**Tühjenda valik**) | **Järjestikku**, **Paralleelselt** ja **Võrdle mõlemat** on tuhmid, **Tühista** on tuhm. **Vali kõik** teeb need aktiivseks | ☑ |
| V5 | Töö ajal (töömaht 30 ja **Võrdle mõlemat**) | Kell "Kulunud aeg" tiksub, edenemisriba täitub, akent saab liigutada ning lehti vahetada (lehel **Raportid** ilmuvad read ükshaaval). Märkeruudud ja liugur on töö ajal tuhmid | ☑ |
| V6 | **Viga:** lehel **Haldus** lisa tüüp `Explorer`, nimi `Tühiaku`, aku `0`; lehel **Arvutused** vali kõik, **Võrdle mõlemat** | Tühiaku rida on punase staatusega **Viga** ja veateatega ("… ei ole töökorras …"). Ülejäänud neli on **Õnnestus**. Loendur "Viga: 1". Töö lõpeb olekuga "Valmis." (mitte tühistatud). Mõlemas režiimis sama tulemus | ☑ |
| V7 | **Tühistamine paralleelselt:** töömaht 50, **Paralleelselt**, varsti **Tühista** | Kõik read on halli staatusega **Tühistatud**, töö peatub vähem kui sekundiga, **Tühista** tuhmub, olek "Tühistatud." | ☑ |
| V8 | **Tühistamine järjestikku:** töömaht 50, **Järjestikku**, varsti **Tühista** | Käimasoleva objekti rida: "Tühistati töö ajal.", järgmiste ridade tekst: "Tühistati enne alustamist." (neid ei alustatud) | ☑ |
| V9 | **Võrdle mõlemat** ja tühista järjestikuse režiimi ajal | Paralleelne režiim ei alga, paralleelset raportit pole, võrdluskaart näitab vihjet | ☑ |
| V10 | Pärast tühistamist käivita uuesti töömahuga 5 | Uus käivitus töötab tavaliselt ja lõpeb olekuga "Valmis." | ☑ |
| V11 | **Hetktõmmis:** vali ainult Sädel, käivita ja jäta tulemus meelde. Tee Sädelile kaks korda **Hullumeelne tegevus!** (aku langeb), käivita uuesti | Uus tulemus on teine (väiksem kaetus), sest uus hetktõmmis on muutunud andmetest | ☑ |
| V12 | Käivita sama sisend kaks korda (sama valik ja töömaht) | Tulemuste tekstid ja numbrid on iga kord täpselt samad (deterministlik), ainult kestused erinevad | ☑ |
| V13 | Vähenda akent alla ~860 px | Märkeruudud, nupud ja raporti read murduvad uuele reale, midagi ei lõigata ära | ☑ |

## 15. Git-koostöö

<!-- TÄIDA: nimed, lingid ja commit'ide hash'id pärast pull request'ide merge'i. Kustuta see kommentaar. -->

### Vol.1

| | |
|---|---|
| Kaasüliõpilane | `<NIMI>` |
| Issue | `<ISSUE-LINK>` |
| Pull request | `<PR-LINK>` |

### Vol.2

| | |
|---|---|
| Vol.1 lähte-commit | tag `vol1-final`, commit `<HASH>` |
| Vol.2 haru | `vol2/events-linq-mvvm` |
| Vol.2 lõpp-commit | `<HASH>` |
| Partner (kolm uut tüüpi minu maailma) | `<NIMI>` |
| Partneri pull request minu repos | `<PR-LINK>` |
| Minu pull request partneri repos | `<PR-LINK>` |

**Töövoog:**
1. Vol.1 viimane seis märgiti sildiga `vol1-final` ja lükati `main`-i.
2. Vol.2 töö tehti eraldi harus `vol2/events-linq-mvvm`, väikeste commit'idena (hoidlad, sündmused, `RobotWorld`, ViewModel, LINQ, View, README).
3. Partner lisas minu maailma kolm uut tegelasetüüpi **oma harus** ja esitas pull request'i. Uued tüübid kasutavad sama hoidlate, sündmuste ja MVVM-lahendust (uus klass, uus `RobotKind` väärtus, üks `Register`-rida, oma reaktsioonid).
4. Mina kontrollisin muudatused (checklist allpool), käivitasin rakenduse, jätsin **review**, **approve**'isin ja **merge**'isin.
5. Samamoodi tegin partneri projektis mina kolm uut tüüpi ja esitasin pull request'i.

**Partnerile antud ülesanne:** lisa maailma kolm uut tegelasetüüpi/klassi, igaühel erinev käitumine, vähemalt üks eksemplar igast uuest tüübist (demoandmetesse). Kolmest olemasoleva tüübi eksemplarist ei piisa.

**Review checklist (mida kontrollisin):**

* [ ] Uus klass pärib `Robot`-ist ja kasutab `override`-i (`CrazyAction`, vähemalt üks `ReactTo...`).
* [ ] Uus `RobotKind` väärtus ja üks `Register`-rida `RobotWorld`-is.
* [ ] Tüübil on oma hoidla ja ta ei satu teise hoidlasse.
* [ ] Vähemalt üks reaktsioon jätab logikirje ja muudab reageerija olekut.
* [ ] Tegija ei reageeri iseendale ja eemaldatud robot enam ei reageeri.
* [ ] Domeeniloogika on Core-projektis, kood-taga ja View ei sisalda äriloogikat.
* [ ] Tekstid on ressurssides, vigane sisend ei muuda olekut.
* [ ] Lahendus kompileerub ja rakendus käivitub puhtalt clone'itud repost.

### Vol.3

| | |
|---|---|
| Vol.3 haru | `vol3/parallel-cpu` |
| Vol.3 pull request | `<PR-LINK>` (base: `<BASE-HARU>`) |
| Vol.3 lõpp-commit | `<HASH>` |

**Töövoog:**
1. Haru `vol3/parallel-cpu` loodi harust `vol2/events-linq-mvvm` pärast partneri pull request'i merge'i.
2. Muudatused tehti väikeste commit'idena: hetktõmmis ja arvutused, jooksutaja (`RobotCalculationRunner`), `CalculationViewModel`, XAML, README.
3. Avati pull request, mille kirjelduses on lühike käivitusjuhend (vt punkt 11) ja demo kolm stsenaariumi.

## 16. Esitamine

<!-- TÄIDA: sama info, mis läheb Moodle'isse. -->

Moodle'isse lisatud andmed:

| | |
|---|---|
| Git-repo link | `<REPOSITORY-URL>` |
| Vol.1 lähte-commit | `<HASH>` (tag `vol1-final`) |
| Vol.2 haru | `vol2/events-linq-mvvm` |
| Vol.2 lõpp-commit | `<HASH>` |
| Partneri MR/PR link | `<PR-LINK>` |

Hoidla ajaloost on nähtav partneri panus, põhiautori (minu) review/approve ja merge.

**Enne esitamist kontrolli (vt punkt 14):**

| Nõue | Kontrollitud juhud |
|---|---|
| Reageerivad ainult sama hoidla teised objektid | A1–A5, 1–3 |
| Eemaldatud objekt enam ei reageeri | A3, A4 |
| Vähemalt üks reaktsioon muudab olekut | A2, 1, 2 |
| UI ja logi uuenevad automaatselt | A2, 4, 17, 18, 19 |
| LINQ-päringud töötavad | 16, 17, 18 |
| Käsu `CanExecute` töötab | 6–9 |
| Vigane sisend ei muuda olekut | 10–15 |

### Vol.3

| | |
|---|---|
| Vol.3 pull request | `<PR-LINK>` |
| Käivitusjuhend | punkt 11 (ja PR-i kirjeldus) |
| Demo | punkt 11: mõlemad režiimid, viga, tühistamine |

**Ülesande nõuded ja kus need kontrollitakse:**

| Nõue | Kus / kontrollitud juhud |
|---|---|
| Vähemalt kolm objekti, igaühel ressursimahukas CPU-arvutus | punkt 10, V1 |
| Objektide andmete hetktõmmis | `RobotSnapshot`, V11 |
| Kasutaja valib objektid ja töömahu | märkeruudud ja liugur, V4 |
| Järjestikune režiim: oodatakse ühe objekti lõppu enne järgmist | punkt 10, V1, V2 |
| Paralleelne režiim: `Task.Run` + `Task.WhenAll` | punkt 10, V1, V3 |
| Sama sisend mõlemas režiimis, tulemuste võrdlus, `Stopwatch` | V1, V12 |
| UI reageerib töö ajal | V5 |
| "Tühista" saadab ühise `CancellationToken`-i, iga tsükkel kontrollib | V7–V10 |
| Edu, viga ja tühistus iga objekti kohta, ühe viga ei peata teisi | V6 |
| Raport: nimi, lõppolek, tulemus või veateade, kestus, kogu töö aeg | V1, V6 |
| PR ja lühike käivitusjuhend, demo (mõlemad režiimid, viga, tühistus) | punkt 11 |

## 17. AI kasutamine

<!-- KONTROLLI: jäta alles ainult need punktid, mida tegid ise. -->

* **Tööriist:** Claude (Anthropic).
* **Kasutusotstarve:** Vol.1 kavandamine ja koodimustandid. Vol.2-s hoidlate, sündmuste ja reaktsioonide kavandamine, `RobotWorld` registri, ViewModeli ja XAML-i mustandid, LINQ-päringute näited, README mustand ning vigade põhjuste otsimine. Vol.3-s CPU-arvutuste, jooksutaja (`RobotCalculationRunner`), `CalculationViewModel`i ja XAML-i mustandid.
* **AI poolt tehtud kontroll:** Core- ja ViewModel-loogikat kontrolliti ajutise konsoolprogrammiga väljaspool minu projekti (A/B/C stsenaarium, valideerimine, `CanExecute`, LINQ). Vol.3 jooksutajat (järjestikune ja paralleelne režiim, viga, tühistamine, hetktõmmis) kontrolliti samuti ajutise programmiga, kuid ühe protsessorituumaga keskkonnas, seega **tegelikku kiirenemist** mõõtsin ise oma arvutis. See **ei asenda** minu enda käsitsi kontrolli: WPF-i välimuse ja kasutusjuhud punktis 14 kontrollisin ise.
* **Enda tehtud kontrollid ja muudatused:**
  * lõin harud ja commit'id ise ning kontrollisin, et Core ei sõltuks WPF-ist;
  * viisin kõik teated ressursifailidesse (eesti keeles) ja parandasin vigased ressursid;
  * katsetasin käsitsi kõiki punktis 14 kirjeldatud kasutusjuhte;
  * mõõtsin ise järjestikuse ja paralleelse režiimi kestused ning kontrollisin vea ja tühistamise demo;
  * kontrollisin partneri pull request'i (review, approve, merge);
  * kohandasin AI pakutud koodi (nimeruumid, kaustade ja failide nimed, väärtuste piirid);
  * oskan seletada sündmuse teekonna (punkt 7) ning Modeli, ViewModeli ja View vastutused (punkt 9).
* Vastutan lõpliku lahenduse eest ja oskan selgitada iga koodiosa.

## Teadlikud otsused ja piirangud

* **Reaktsioon ka luhtunud katsele:** hoidla tõstab `CrazyActionPerformed` iga katse järel, ka siis, kui tegija keeldus (nt liiga vähe akut). Naabrid "näevad katset".
* **`GuardBot` ei ole laetav** ja ei kuluta akut: see on teadlik jutuline erand, mis näitab `is IChargeable` kontrolli.
* **Reaktsioonid on tüübisisesed:** kõik `CleanerBot`-id kuuluvad ühte hoidlasse. Teise tüübi robotid ei reageeri neile.
* **ID on unikaalne kogu maailmas**, mitte ainult hoidlas.
* **Remondi sihtmärk** võib olla mis tahes robot (ka teisest hoidlast). `Repair` ise ei tekita sündmust.
* **Arvutused on puhtad:** nad loevad ainult hetktõmmist ega muuda robotit. Sama sisend annab alati sama tulemuse (igal robotil on oma `Random`, mille seeme tuleb ID-st ja töömahust), seega kahte režiimi saab ausalt võrrelda.
* **Viga tekib loomulikult:** robot, mis ei ole töökorras (aku või terviklikkus 0), ei saa arvutust teha. Demos lisatakse selline robot käsitsi. Ühe objekti viga läheb raportisse ja teised tööd jätkuvad, sest iga ülesanne püüab oma erandid kinni ja `Task.WhenAll` ei visata.
* **`Task.Run` + `Task.WhenAll`:** paralleelne režiim kasutab täpselt neid, nagu ülesanne nõuab (mitte `Parallel.ForEach`).
* **Kiirenemine sõltub tuumadest:** neljal objektil on parim võimalik kiirendus umbes tuumade arvu ulatuses, aga ühe tuumaga arvutis on see ≈ ×1. Kui objekte on vähem kui tuumi, piirab tulemust kõige pikem arvutus. *Debug*-ehitus on aeglasem kui *Release*.
* **Tühistamine on koostööpõhine:** token ei peata lõime jõuga, vaid iga CPU-tsükkel kontrollib seda ja viskab `OperationCanceledException`-i. Objektid, mille tööd polnud veel alanud, märgitakse tühistatuks enne alustamist.
* **Arvutused ei tekita sündmusi:** Vol.2 reaktsioonid ja Vol.3 arvutused on eraldi teemad. Arvutus ei muuda roboti olekut ega käivita naabrite reaktsioone.