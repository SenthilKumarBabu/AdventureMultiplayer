Start the ML-Agents trainer and connect Unity Play mode to it.

Steps:
1. Kill any existing mlagents-learn process on port 5004:
   Run: `powershell -Command "Get-NetTCPConnection -LocalPort 5004 -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id (Get-Process -Id (Get-NetTCPConnection -LocalPort 5004).OwningProcess).Id -Force -ErrorAction SilentlyContinue }"`

2. Start mlagents-learn in the background:
   Run: `cd "D:/Unity Projects/AdventureMultiplayer/AdventureMultiplayer" && "C:/Users/Senthil/AppData/Roaming/Python/Python39/Scripts/mlagents-learn.exe" ml-agents/config.yaml --run-id=aibot_v1 --resume`
   (If --resume fails because no prior run exists, retry without --resume)

3. Tell the user: "Trainer is starting. Watch for 'Listening on port 5004' in the terminal, then press Play in Unity."

4. Do NOT attempt to press Play automatically — the user must do this in the Unity Editor.
