# Commands
Several commands can be on the simulator program. These can either be submitted from the network via a "command frequency" or via the Command Window provided. There are both program commands as well as aircraft commands.

## Program Commands
| Command Inputs | Command Name | Command Arguments | Command Description |
|--|--|--|--|
| `upall`, `unpauseall` | Unpause All | none | Unpauses all simulated aircraft |
| `pall`, `pauseall` | Pause All | none | Pauses all simulated aircraft |

## Aircraft Commands
| Command Inputs | Command Name | Command Arguments | Command Description |
|--|--|--|--|
| `up`, `unpause` | Unpause Aircraft | none | Unpauses the aircraft |
| `p`, `pause` | Pause Aircraft | none | Pauses the aircraft |
| `del`, `delete`, `remove` | Delete Aircraft | none | Deletes the aircraft |
| `fh` | Fly Heading | `<Magnetic Heading>` | Flies specified heading |
| `tl` | Turn Left Heading | `<Magnetic Heading>` | Turns left to specified heading |
| `tr` | Turn Right Heading | `<Magnetic Heading>` | Turns right to specified heading |
| `tlb` | Turn Left By | `<Degrees To Turn>` | Turns left by a certain amount |
| `trb` | Turn Right By | `<Degrees To Turn>` | Turns right by a certain amount |
| `fph` | Fly Present Heading | none | Flies present heading |
| `dh`, `lh`, `dephdg`, `leavehdg` | Depart/Leave On Heading | `<Waypoint Name>` `<Magnetic Heading>` | Departs waypoint on specified heading |
| `int`, `intercept` | Intercept Course | `<Waypoint Name>` `<Magnetic Course>` | Intercepts and tracks a course to a specific waypoint |
| `loc`, `ils` | Intercept ILS | `<Runway Designator>` | Intercepts and tracks a localizer to a specific runway |
| `dct`, `dir`, `direct` | Direct To Waypoint | `<Waypoint Name>` | Instructs the navigation to go direct to a waypoint. Will amend the route if waypoint already existed in the flight plan. Otherwise, a discontinuity is inserted |
| `hold` | Insert a Hold | `<Waypoint Name>` (`<Inbound Magnetic Course>/<Turn Direction>/<Leg Length>` | Instructs the navigation to hold at a waypoint. Waypoint must be in flight plan. Assumes published hold unless inbound course is provided. Turn direction and leg length are optional. Leg length is minutes by default. Appending `nm` will result in leg length being a distance in nautical miles. |
| `alt`, `cm`, `dm`, `clm`, `des`, `climb`, `descend` | Change Altitude | `<Altitude (ft) or 'FLXXX' (100s of ft)>` | Aircraft will open climb or open descend to altitude |