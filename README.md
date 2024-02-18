# ProcessPal

ProcessPal allows you to easily spin up, tear down, and automatically restart groups of processes through a CLI.

I personally use it for running and debugging multiple code projects at once:
- Using the ProcessPal CLI with my Stream Deck, I spin up and tear down the processes
- If a process crashes, the `RestartOnExit` option will start the process again
- In my IDE, I can debug by attaching to the processes

# Getting Started

1. Install the [.NET 8 runtime or SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
1. Download and extract the zip for your operating system from the [releases](https://github.com/daltonks/ProcessPal/releases) page.
1. In the extracted directory, create the file `_config.json`.
  
   Example:
   ```json
    {
        "Port": 7161,
        "ProcessGroups": 
        {
            "RunTwoExampleScripts": [
                {
                    "FileName": "powershell.exe",
                    "Args": "-File example1.ps1",
                    "RestartOnExit": true
                },
                {
                    "FileName": "powershell.exe",
                    "Args": "-File example2.ps1",
                    "RestartOnExit": true
                }
            ]
        }
    }
   ```

1. In a terminal, run the ProcessPal executable to see the CLI options.
   
   On Windows, this is `ProcessPal.exe`.

   On Mac, this is `ProcessPal`.

## PowerShell example

Toggle the server on/off:

`./ProcessPal.exe toggle-server`

Toggle a process group on/off:

`./ProcessPal.exe toggle-group --name RunTwoExampleScripts`
