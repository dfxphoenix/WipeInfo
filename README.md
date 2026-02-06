## Overview
**Wipe Info** is a plugin who shows in chat time until wipe date.

The plugin shows:
* the date of the **last map wipe**
* the **time remaining until the next map wipe**

Wipe dates are calculated automatically based on the **first Thursday of the month at 19:00 UTC** (hardcoded, non-configurable), matching Rust force wipe behavior.

## Commands
* `wipe` - Displays last wipe date and time until the next wipe.

## Configuration
This plugin supports optional wipe announcements on player join or on a repeating timer.

Announce timer is expressed in **hours**.

```json
{
  "Date format": "MM/dd/yyyy",
  "Announce on join": false,
  "Announce on timer": false,
  "Announce timer": 3
}
```

## Languages
**Wipe Info** have two languages by default (**English** and **Romanian**), but you can add more in Oxide lang folder.

## API Hooks
```csharp
string API_GetLastWipe()
string API_GetNextWipe()
DateTime API_GetLastWipeUtc()
DateTime API_GetNextWipeUtc()
int API_GetTimeUntilNextWipeSeconds()
void API_Refresh()
```

### API Details
* **API_GetLastWipe** - Returns the last wipe date as a formatted string (using `Date format`).
* **API_GetNextWipe** - Returns the next wipe date as a formatted string.
* **API_GetLastWipeUtc** - Returns the exact UTC `DateTime` of the last wipe (first Thursday at 19:00 UTC).
* **API_GetNextWipeUtc** - Returns the exact UTC `DateTime` of the next wipe.
* **API_GetTimeUntilNextWipeSeconds** - Returns the number of seconds remaining until the next wipe (never negative).
* **API_Refresh** - Forces an internal recalculation of wipe dates (useful for long-running servers).
