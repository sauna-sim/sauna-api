# Sauna API

## About
This is a project that will allow for simulated ATC sessions (sweatbox sessions). It can be used on the VATSIM sweatbox server or on private FSD servers. By allowing airport and aircraft scenario configurations, it will allow ARTCCs and FIRs to better train their controllers for situations that they may encounter on the VATSIM network.

The idea was to create a realistic sweatbox simulator that could account for performance data, atmospheric conditions, routes and procedures, etc. The program should be able to handle ANY command that would be given over the network and the aircraft should respond in a realistic manner.

This project is the API that contains the simulator itself. This is a stateful API and has several endpoints to manage the current scenario. Third party applications can bundle the API and provide their own interface to manage a sweatbox scenario.

The API is written in C# and runs on ASP.NET Core. The application is cross-platform.

The solution contains 2 projects. `sauna-api` handles the actual API requests and `sauna-sim-core` contains the core simulation code.

### [Sauna UI](https://github.com/Sauna-ATC-Training-Simulator/sauna-ui)
The Sauna UI project will provide a user-friendly interface for the API.

## Dependencies
The project depends on the following frameworks and packages:
- **.NET 6.0 & ASP .NET Core 6.0**
- **[Aviation Calc Util NET](https://github.com/997R8V10/aviation-calc-util-net)**
- **[FSD Connector NET](https://github.com/caspianmerlin/FsdConnectorNet)**
- **[Newtonsoft JSON](https://github.com/JamesNK/Newtonsoft.Json)**

## Building
The API is a .NET 6.0 project. Simply build the sauna-api solution for your desired platform. All dependencies are pulled by NuGet automatically.

## Usage
Ensure that settings are sent to the API on initial connect via the `POST /api/data/settings` endpoint. Current settings can be obtained via `GET /api/data/settings`.

**Settings Example:**
```
{
  "commandFrequency": "133.125",
  "posCalcRate": "100"
}
```

### Sector Data
The program currently **REQUIRES** that waypoint and airway data be loaded from a sector file. It accepts both `*.sct2` and `*.sct` from Euroscope or VRC. Use the `POST /api/data/loadSectorFile` endpoint.

**Data Format:**
```
{
  "fileName": "/Users/asdf/Downloads/test.sct2"
}
```

### Scenario Files
Currently, a modified Euroscope 3.2 scenario file format (`*.txt`) is used. The main difference is that appending a ` HOLD` at the end of the ROUTE string will cause the aircraft to automatically hold. Original ES 3.2 file should work without modification.

*Note: This file format may be replaced by a more robust file format in the future.*

Load scenario files through the `POST /api/data/loadEuroscopeScenario` endpoint.

**Data Format:**
```
{
  "fileName": "/Users/asdf/Downloads/scenario.txt",
  "cid": "111111",
  "password": "password",
  "server": "fsdserver.example.com",
  "port": 6809,
  "protocolRevision": "Classic"
}
```

### Running the Scenario
Aircraft are paused by default when the scenario is loaded in. All aircraft are currently controlled via text commands sent through the *Command Frequency* or via the API.

[Command Reference Guide](Commands.md)

#### Command Frequency
If a command frequency has been specified in the Settings, this method can be used to control the aircraft. Simply sending a text message to the desired aircraft over the command frequency will cause the command to be registered. The output is then returned back over frequency.
