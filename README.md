# ProcessPal

ProcessPal allows you to easily spin up, tear down, and automatically restart groups of processes through a CLI, which can be handy when paired with a Stream Deck.

# Getting Started

1. Download and extract the zip for your operating system from the [releases](https://github.com/daltonks/ProcessPal/releases) page.
2. In the extracted directory, create the file `_config.json`.
  
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

3. In a terminal, run the ProcessPal executable to see the CLI options.
   
   On Windows, this is `ProcessPal.exe`.

   On Mac, this is `ProcessPal`.

## PowerShell example

Toggle the server on/off:

`./ProcessPal.exe toggle-server`

Toggle a process group on/off:

`./ProcessPal.exe toggle-group --name RunTwoExampleScripts`
