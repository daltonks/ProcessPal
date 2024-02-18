# ProcessPal

ProcessPal was initially created to assist in debugging multiple services at once.

It allows you to easily start and stop groups of processes through a CLI, which can be handy when paired with a Stream Deck.

To start:

1. Download and extract `ProcessPal.win-64.zip` or `ProcessPal.osx-64.zip` from the [releases](https://github.com/daltonks/ProcessPal/releases) page.
2. In the extracted directory, create the file `_config.json`.
  
   Example:
   ```json
    {
        "Port": 7161,
        "ProcessGroups": 
        {
            "RunTwoTestScripts": [
                {
                    "FileName": "powershell.exe",
                    "Args": "-File test1.ps1",
                    "RestartOnExit": true
                },
                {
                    "FileName": "powershell.exe",
                    "Args": "-File test2.ps1",
                    "RestartOnExit": true
                }
            ]
        }
    }
   ```
3. For Windows, run `ProcessPal.exe` in a terminal to see the CLI options.
   
   For Mac, run `ProcessPal` in a terminal to see the CLI options.

   Windows PowerShell example: `./ProcessPal.exe toggle -n RunTwoTestScripts`
