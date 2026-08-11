# FenixModbusS7 - Script Templates

Skrypty w tym folderze są **szablonami** (template). Możesz je dodać do projektu z poziomu aplikacji.

## Jak używać

1. Otwórz projekt w FenixModbusS7
2. W drzewie projektu → **Scripts Engine** → kliknij prawym przyciskiem → **Add Script From Template**
3. Wybierz szablon z listy i podaj nazwę nowego skryptu
4. Wybierz Timer (lub utwórz nowy w `Scripts Engine` → `Timers`)
5. Przypisz Timer do skryptu
6. Uruchom komunikację - skrypt zacznie działać

> Przy tworzeniu nowego projektu (`Add Project`) szablony z tego folderu są automatycznie kopiowane do katalogu `Scripts/` projektu.

## API skryptów

| Metoda | Opis |
|---|---|
| `GetTag("TagName")` | Odczytuje wartość taga (szuka w Tag i InTag) |
| `SetTag("TagName", value)` | Zapisuje wartość do taga |
| `Write("message")` | Wypisuje komunikat do panelu Output |
| `GetITag("TagName")` | Zwraca obiekt ITag (z wszystkimi właściwościami) |

## Lista skryptów

| # | Skrypt | Timer | Opis |
|---|---|---|---|
| 01 | `PID_Controller` | 100ms | Regulator PID z anti-windup, derivative-on-PV |
| 02 | `AlarmManager` | 500ms | Monitorowanie wielu tagów z progami alarmowymi |
| 03 | `MovingAverageFilter` | 50-100ms | Filtr średniej ruchomej do wygładzania sygnałów |
| 04 | `FlowTotalizer` | 1000ms | Akumulacja przepływu (licznik objętości) |
| 05 | `SignalGenerator` | 100ms | Generator sygnałów testowych (sin, square, saw, noise, ramp) |
| 06 | `RateOfChange` | 100-200ms | Monitor prędkości zmian sygnału (derivative) |
| 07 | `StateMachine` | 200ms | Maszyna stanów procesu (Idle→Start→Run→Stop) |
| 08 | `HeartbeatWatchdog` | 1000ms | Heartbeat i monitorowanie systemu |
| 09 | `DeadbandFilter` | 100ms | Filtr strefy nieczułości - redukuje transmisje |
| 10 | `DataCollector` | 1000ms | Rejestracja danych do CSV w panelu Output |

## Wskazówki

- **HeartbeatWatchdog** powinien być zawsze pierwszym skryptem na liście
- Używaj **Internal Tags** (InTag) dla zmiennych pomocniczych skryptów
- Każdy skrypt musi rozszerzać `ScriptModel` i implementować `Cycle()`
- Timer jest przypisywany per-script w właściwościach pliku skryptu
