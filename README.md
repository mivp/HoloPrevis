# Hololen app to view previs data

## Current functions (master branch)

- Load a previs mesh model or pointcloud by scanning a QR code. The model data is stored locally for demo (StreamingAssets)
- Provide a GUI menu to load/unload, scan QR code and modify the loaded object (move, rotate, scale)
- Support multi-hololens using UNET (only user who loads object can unload)
- Highligh object part when a user focus on it
- Sounds (environment + button click)


## Bugs or improvement features

- Add volume (slice) support
- Add network download

## Dev enviroment

- Windows 10
- Unity 2018.3
- HoloToolkit-Unity-2017.4.3.0
- Visual studio 2017 (community version)

## How to build and run

- Load project folder in Unity
- Open HoloPrevisUNET scene
- Build UWP project
- Build and deploy to hololens
